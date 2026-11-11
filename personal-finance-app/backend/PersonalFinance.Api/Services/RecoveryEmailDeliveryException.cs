namespace PersonalFinance.Api.Services;

/// <summary>Raised when a password recovery email cannot be delivered.</summary>
public sealed class RecoveryEmailDeliveryException : Exception
{
    public RecoveryEmailDeliveryException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
