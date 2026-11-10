using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;

namespace PersonalFinance.Api.Services;

public sealed class BrevoRecoveryEmailSender(
    HttpClient httpClient,
    IOptions<BrevoOptions> options,
    ILogger<BrevoRecoveryEmailSender> logger) : IRecoveryEmailSender
{
    public const string ApiBaseAddress = "https://api.brevo.com/";
    private const string SendEmailPath = "v3/smtp/email";

    public async Task SendPasswordResetAsync(
        string recipientEmail,
        Uri resetLink,
        CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        ValidateSettings(settings);

        if (string.IsNullOrWhiteSpace(recipientEmail))
            throw new RecoveryEmailDeliveryException("O destinatário do email de recuperação é inválido.");

        if (!resetLink.IsAbsoluteUri)
            throw new RecoveryEmailDeliveryException("O link de recuperação é inválido.");

        var sender = new { email = settings.SenderEmail!, name = settings.SenderName };
        var payload = new
        {
            sender,
            to = new[] { new { email = recipientEmail } },
            subject = "Recuperação da palavra-passe",
            htmlContent = CreateHtmlContent(resetLink)
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, SendEmailPath)
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.Add("api-key", settings.ApiKey);

        try
        {
            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
                return;

            // Do not log the request URI, body, recipient, reset token or API key.
            logger.LogWarning("Brevo rejected a password recovery email with status code {StatusCode}.",
                (int)response.StatusCode);
            throw new RecoveryEmailDeliveryException("Não foi possível enviar o email de recuperação.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (RecoveryEmailDeliveryException)
        {
            throw;
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(exception, "Brevo could not be reached for a password recovery email.");
            throw new RecoveryEmailDeliveryException("Não foi possível enviar o email de recuperação.", exception);
        }
    }

    private static void ValidateSettings(BrevoOptions settings)
    {
        if (string.IsNullOrWhiteSpace(settings.ApiKey) ||
            string.IsNullOrWhiteSpace(settings.SenderEmail))
        {
            throw new RecoveryEmailDeliveryException(
                "A configuração do email de recuperação está incompleta.");
        }
    }

    private static string CreateHtmlContent(Uri resetLink)
    {
        var encodedLink = WebUtility.HtmlEncode(resetLink.AbsoluteUri);
        return $"<p>Recebemos um pedido para redefinir a sua palavra-passe.</p>" +
               $"<p><a href=\"{encodedLink}\">Redefinir palavra-passe</a></p>" +
               "<p>Se não fez este pedido, pode ignorar este email.</p>";
    }
}
