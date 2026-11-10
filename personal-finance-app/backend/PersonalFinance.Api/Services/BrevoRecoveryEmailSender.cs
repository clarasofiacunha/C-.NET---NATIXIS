using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace PersonalFinance.Api.Services;

public sealed class BrevoRecoveryEmailSender(
    IOptions<BrevoOptions> options,
    ISmtpEmailClientFactory smtpClientFactory,
    ILogger<BrevoRecoveryEmailSender> logger) : IRecoveryEmailSender
{
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

        using var message = new MailMessage
        {
            From = new MailAddress(settings.SenderEmail!, settings.SenderName),
            Subject = "Recuperação da palavra-passe",
            Body = CreateHtmlContent(resetLink),
            IsBodyHtml = true
        };
        message.To.Add(recipientEmail);

        try
        {
            using var smtpClient = smtpClientFactory.Create(settings);
            await smtpClient.SendAsync(message, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (RecoveryEmailDeliveryException)
        {
            throw;
        }
        catch (SmtpException exception)
        {
            // Do not log the recipient, reset link, SMTP password, or SMTP username.
            logger.LogWarning(exception, "Brevo SMTP could not deliver a password recovery email.");
            throw new RecoveryEmailDeliveryException("Não foi possível enviar o email de recuperação.", exception);
        }
    }

    private static void ValidateSettings(BrevoOptions settings)
    {
        if (string.IsNullOrWhiteSpace(settings.SmtpHost) ||
            settings.SmtpPort is < 1 or > 65535 ||
            string.IsNullOrWhiteSpace(settings.SmtpUsername) ||
            string.IsNullOrWhiteSpace(settings.SmtpPassword) ||
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
