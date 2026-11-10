using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PersonalFinance.Api.Data;
using PersonalFinance.Api.Models;
using PersonalFinance.Api.Services;
using Microsoft.AspNetCore.Identity;

namespace PersonalFinance.Tests;

public class PasswordRecoveryTests : IClassFixture<PasswordRecoveryWebApplicationFactory>
{
    private readonly HttpClient client;
    private readonly PasswordRecoveryWebApplicationFactory factory;

    public PasswordRecoveryTests(PasswordRecoveryWebApplicationFactory factory)
    {
        this.factory = factory;
        factory.EmailSender.Clear();
        client = factory.CreateClient();
    }

    [Fact]
    public async Task ForgotPassword_ContaExistente_EnviaLinkESemTokenNaResposta()
    {
        var email = NewEmail();
        await RegisterAsync(email);

        var response = await client.PostAsJsonAsync("/auth/forgot-password", new { email });
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain("token", body, StringComparison.OrdinalIgnoreCase);
        var link = Assert.Single(factory.EmailSender.Links);
        Assert.Equal("https://app.example/", link.GetLeftPart(UriPartial.Path));
        Assert.StartsWith("#reset-password?", link.Fragment);
        Assert.Contains($"email={Uri.EscapeDataString(email)}", link.Fragment);
        Assert.Contains("token=", link.Fragment);
    }

    [Fact]
    public async Task ForgotPassword_ContaInexistente_DevolveMesmaRespostaSemEnviarEmail()
    {
        var response = await client.PostAsJsonAsync("/auth/forgot-password", new { email = NewEmail() });
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(
            "Se existir uma conta com este email, receberá instruções para redefinir a palavra-passe.",
            body.RootElement.GetProperty("message").GetString());
        Assert.Empty(factory.EmailSender.Links);
    }

    [Fact]
    public async Task ForgotPassword_EmailInvalido_DevolveBadRequest()
    {
        var response = await client.PostAsJsonAsync("/auth/forgot-password", new { email = "nao-e-email" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_TokenValido_AlteraPasswordSemCriarSessao()
    {
        var email = NewEmail();
        const string oldPassword = "Password1";
        const string newPassword = "NovaPassword2";
        await RegisterAsync(email);
        var token = await GenerateResetTokenAsync(email);

        var resetResponse = await client.PostAsJsonAsync("/auth/reset-password", new
        {
            email,
            token,
            newPassword
        });

        Assert.Equal(HttpStatusCode.NoContent, resetResponse.StatusCode);
        Assert.False(resetResponse.Headers.Contains("Set-Cookie"));
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/auth/me")).StatusCode);

        var oldPasswordLogin = await client.PostAsJsonAsync("/auth/login", new { email, password = oldPassword });
        var newPasswordLogin = await client.PostAsJsonAsync("/auth/login", new { email, password = newPassword });
        Assert.Equal(HttpStatusCode.Unauthorized, oldPasswordLogin.StatusCode);
        Assert.Equal(HttpStatusCode.OK, newPasswordLogin.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_TokenInvalidoOuReutilizado_DevolveErroSeguro()
    {
        var email = NewEmail();
        await RegisterAsync(email);
        var token = await GenerateResetTokenAsync(email);

        var invalidResponse = await client.PostAsJsonAsync("/auth/reset-password", new
        {
            email,
            token = "token-invalido",
            newPassword = "NovaPassword2"
        });
        var firstUseResponse = await client.PostAsJsonAsync("/auth/reset-password", new
        {
            email,
            token,
            newPassword = "NovaPassword2"
        });
        var reusedResponse = await client.PostAsJsonAsync("/auth/reset-password", new
        {
            email,
            token,
            newPassword = "OutraPassword3"
        });

        Assert.Equal(HttpStatusCode.BadRequest, invalidResponse.StatusCode);
        Assert.DoesNotContain(token, await invalidResponse.Content.ReadAsStringAsync(), StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.NoContent, firstUseResponse.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, reusedResponse.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_PasswordInvalida_DevolveErrosDoIdentity()
    {
        var email = NewEmail();
        await RegisterAsync(email);
        var token = await GenerateResetTokenAsync(email);

        var response = await client.PostAsJsonAsync("/auth/reset-password", new
        {
            email,
            token,
            newPassword = "curta"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(body.RootElement.TryGetProperty("errors", out _));
    }

    private async Task<string> GenerateResetTokenAsync(string email)
    {
        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var user = await userManager.FindByEmailAsync(email);
        Assert.NotNull(user);
        return await userManager.GeneratePasswordResetTokenAsync(user!);
    }

    private async Task RegisterAsync(string email)
    {
        var response = await client.PostAsJsonAsync("/auth/register", new
        {
            name = "Utilizador de Teste",
            email,
            password = "Password1"
        });
        response.EnsureSuccessStatusCode();
    }

    private static string NewEmail() => $"recover.{Guid.NewGuid():N}@example.com";
}

public sealed class PasswordRecoveryWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string databaseName = $"password-recovery-tests-{Guid.NewGuid():N}";
    public RecordingRecoveryEmailSender EmailSender { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:DefaultConnection", "Host=localhost;Database=test;Username=test;Password=test");
        builder.UseSetting("Brevo:FrontendBaseUrl", "https://app.example/");
        builder.ConfigureServices(services =>
        {
            services.AddDataProtection().UseEphemeralDataProtectionProvider();
            services.Remove(services.Single(service => service.ServiceType == typeof(DbContextOptions<AppDbContext>)));
            services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(databaseName));
            services.RemoveAll<IRecoveryEmailSender>();
            services.AddSingleton<IRecoveryEmailSender>(EmailSender);
        });
    }
}

public sealed class RecordingRecoveryEmailSender : IRecoveryEmailSender
{
    public List<Uri> Links { get; } = [];

    public Task SendPasswordResetAsync(string recipientEmail, Uri resetLink, CancellationToken cancellationToken = default)
    {
        Links.Add(resetLink);
        return Task.CompletedTask;
    }

    public void Clear() => Links.Clear();
}
