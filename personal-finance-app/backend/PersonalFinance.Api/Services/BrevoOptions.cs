namespace PersonalFinance.Api.Services;

/// <summary>
/// Server-side settings for password recovery emails.
/// Configure secrets with environment variables or user-secrets, for example
/// Brevo__ApiKey. Do not place the API key in appsettings files or the frontend.
/// </summary>
public sealed class BrevoOptions
{
    public const string SectionName = "Brevo";

    public string? ApiKey { get; init; }
    public string? SenderEmail { get; init; }
    public string? SenderName { get; init; }

    // Used by the password recovery endpoint when it creates the reset link.
    public string? FrontendBaseUrl { get; init; }
}
