using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PersonalFinance.Api.Services;

namespace PersonalFinance.Tests;

public class BrevoRecoveryEmailSenderTests
{
    [Fact]
    public async Task SendPasswordResetAsync_EnviaPedidoBrevoComHeaderEPayloadEsperados()
    {
        var handler = new RecordingHandler(HttpStatusCode.Accepted);
        using var client = new HttpClient(handler) { BaseAddress = new Uri(BrevoRecoveryEmailSender.ApiBaseAddress) };
        var sender = CreateSender(client);
        var link = new Uri("https://app.example/#reset-password?email=user%40example.com&token=token-secreto");

        await sender.SendPasswordResetAsync("user@example.com", link);

        Assert.NotNull(handler.Request);
        Assert.Equal(HttpMethod.Post, handler.Request!.Method);
        Assert.Equal("https://api.brevo.com/v3/smtp/email", handler.Request.RequestUri!.AbsoluteUri);
        Assert.Equal("brevo-test-key", handler.Request.Headers.GetValues("api-key").Single());

        using var document = JsonDocument.Parse(handler.Body!);
        Assert.Equal("sender@example.com", document.RootElement.GetProperty("sender").GetProperty("email").GetString());
        Assert.Equal("user@example.com", document.RootElement.GetProperty("to")[0].GetProperty("email").GetString());
        Assert.Contains(
            "https://app.example/#reset-password?email=user%40example.com&amp;token=token-secreto",
            document.RootElement.GetProperty("htmlContent").GetString());
    }

    [Fact]
    public async Task SendPasswordResetAsync_RespostaFalhada_DevolveErroControlado()
    {
        using var client = new HttpClient(new RecordingHandler(HttpStatusCode.BadRequest))
        {
            BaseAddress = new Uri(BrevoRecoveryEmailSender.ApiBaseAddress)
        };
        var sender = CreateSender(client);

        var exception = await Assert.ThrowsAsync<RecoveryEmailDeliveryException>(() =>
            sender.SendPasswordResetAsync("user@example.com", new Uri("https://app.example/#reset-password?token=secret")));

        Assert.DoesNotContain("secret", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SendPasswordResetAsync_ConfiguracaoEmFalta_DevolveErroControlado()
    {
        using var client = new HttpClient(new RecordingHandler(HttpStatusCode.Accepted));
        var sender = new BrevoRecoveryEmailSender(
            client,
            Options.Create(new BrevoOptions()),
            NullLogger<BrevoRecoveryEmailSender>.Instance);

        await Assert.ThrowsAsync<RecoveryEmailDeliveryException>(() =>
            sender.SendPasswordResetAsync("user@example.com", new Uri("https://app.example/reset")));
    }

    private static BrevoRecoveryEmailSender CreateSender(HttpClient client) => new(
        client,
        Options.Create(new BrevoOptions
        {
            ApiKey = "brevo-test-key",
            SenderEmail = "sender@example.com",
            SenderName = "Personal Finance",
            FrontendBaseUrl = "https://app.example"
        }),
        NullLogger<BrevoRecoveryEmailSender>.Instance);

    private sealed class RecordingHandler(HttpStatusCode statusCode) : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }
        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Request = request;
            Body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(statusCode);
        }
    }
}
