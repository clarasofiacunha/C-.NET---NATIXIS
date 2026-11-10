using System.Net.Mail;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PersonalFinance.Api.Services;

namespace PersonalFinance.Tests;

public class BrevoRecoveryEmailSenderTests
{
    [Fact]
    public async Task SendPasswordResetAsync_EnviaMensagemSmtpComParametrosEsperados()
    {
        var smtpClient = new RecordingSmtpEmailClient();
        var sender = CreateSender(smtpClient);
        var link = new Uri("https://app.example/#reset-password?email=user%40example.com&token=token-secreto");

        await sender.SendPasswordResetAsync("user@example.com", link);

        Assert.Equal("sender@example.com", smtpClient.FromEmail);
        Assert.Equal("Personal Finance", smtpClient.FromName);
        Assert.Equal("user@example.com", smtpClient.RecipientEmail);
        Assert.Equal("Recuperação da palavra-passe", smtpClient.Subject);
        Assert.True(smtpClient.IsBodyHtml);
        Assert.Contains(
            "https://app.example/#reset-password?email=user%40example.com&amp;token=token-secreto",
            smtpClient.Body);
    }

    [Fact]
    public async Task SendPasswordResetAsync_FalhaSmtp_DevolveErroControlado()
    {
        var sender = CreateSender(new RecordingSmtpEmailClient
        {
            ExceptionToThrow = new SmtpException("SMTP rejected the message.")
        });

        var exception = await Assert.ThrowsAsync<RecoveryEmailDeliveryException>(() =>
            sender.SendPasswordResetAsync("user@example.com", new Uri("https://app.example/#reset-password?token=secret")));

        Assert.DoesNotContain("secret", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SendPasswordResetAsync_ConfiguracaoEmFalta_DevolveErroControlado()
    {
        var sender = new BrevoRecoveryEmailSender(
            Options.Create(new BrevoOptions()),
            new RecordingSmtpEmailClientFactory(new RecordingSmtpEmailClient()),
            NullLogger<BrevoRecoveryEmailSender>.Instance);

        await Assert.ThrowsAsync<RecoveryEmailDeliveryException>(() =>
            sender.SendPasswordResetAsync("user@example.com", new Uri("https://app.example/reset")));
    }

    private static BrevoRecoveryEmailSender CreateSender(RecordingSmtpEmailClient smtpClient) => new(
        Options.Create(new BrevoOptions
        {
            SmtpHost = "smtp-relay.brevo.com",
            SmtpPort = 587,
            SmtpUsername = "login@smtp-brevo.com",
            SmtpPassword = "smtp-test-key",
            SenderEmail = "sender@example.com",
            SenderName = "Personal Finance",
            FrontendBaseUrl = "https://app.example"
        }),
        new RecordingSmtpEmailClientFactory(smtpClient),
        NullLogger<BrevoRecoveryEmailSender>.Instance);

    private sealed class RecordingSmtpEmailClientFactory(RecordingSmtpEmailClient client) : ISmtpEmailClientFactory
    {
        public ISmtpEmailClient Create(BrevoOptions settings) => client;
    }

    private sealed class RecordingSmtpEmailClient : ISmtpEmailClient
    {
        public Exception? ExceptionToThrow { get; init; }
        public string? FromEmail { get; private set; }
        public string? FromName { get; private set; }
        public string? RecipientEmail { get; private set; }
        public string? Subject { get; private set; }
        public string? Body { get; private set; }
        public bool IsBodyHtml { get; private set; }

        public Task SendAsync(MailMessage message, CancellationToken cancellationToken)
        {
            FromEmail = message.From?.Address;
            FromName = message.From?.DisplayName;
            RecipientEmail = message.To.Single().Address;
            Subject = message.Subject;
            Body = message.Body;
            IsBodyHtml = message.IsBodyHtml;

            return ExceptionToThrow is null
                ? Task.CompletedTask
                : Task.FromException(ExceptionToThrow);
        }

        public void Dispose()
        {
        }
    }
}
