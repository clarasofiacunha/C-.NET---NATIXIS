using System.Net;
using System.Net.Mail;

namespace PersonalFinance.Api.Services;

public interface ISmtpEmailClient : IDisposable
{
    Task SendAsync(MailMessage message, CancellationToken cancellationToken);
}

public interface ISmtpEmailClientFactory
{
    ISmtpEmailClient Create(BrevoOptions settings);
}

public sealed class SmtpEmailClientFactory : ISmtpEmailClientFactory
{
    public ISmtpEmailClient Create(BrevoOptions settings)
    {
        var client = new SmtpClient(settings.SmtpHost!, settings.SmtpPort)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(settings.SmtpUsername, settings.SmtpPassword),
            DeliveryMethod = SmtpDeliveryMethod.Network
        };

        return new SmtpEmailClient(client);
    }
}

public sealed class SmtpEmailClient(SmtpClient client) : ISmtpEmailClient
{
    public Task SendAsync(MailMessage message, CancellationToken cancellationToken) =>
        client.SendMailAsync(message).WaitAsync(cancellationToken);

    public void Dispose() => client.Dispose();
}
