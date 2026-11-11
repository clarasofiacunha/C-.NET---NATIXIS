namespace PersonalFinance.Api.Services;

public interface IRecoveryEmailSender
{
    Task SendPasswordResetAsync(
        string recipientEmail,
        Uri resetLink,
        CancellationToken cancellationToken = default);
}
