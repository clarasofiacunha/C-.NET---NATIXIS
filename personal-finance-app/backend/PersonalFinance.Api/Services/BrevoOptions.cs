namespace PersonalFinance.Api.Services;

/// <summary>
/// Server-side settings for password recovery emails sent through Brevo SMTP.
/// Configure credentials with environment variables or user-secrets, for example
/// Brevo__SmtpPassword. Do not place SMTP credentials in appsettings files or the frontend.
/// </summary>
public sealed class BrevoOptions
{
    public const string SectionName = "Brevo";

    public string? SmtpHost { get; init; }
    public int SmtpPort { get; init; } = 587;
    public string? SmtpUsername { get; init; }
    public string? SmtpPassword { get; init; }
    public string? SenderEmail { get; init; }
    public string? SenderName { get; init; }

    // Used by the password recovery endpoint when it creates the reset link.
    public string? FrontendBaseUrl { get; init; }
}
