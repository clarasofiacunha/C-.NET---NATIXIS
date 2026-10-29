using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PersonalFinance.Api.Data;
using PersonalFinance.Api.DTOs.Auth;

namespace PersonalFinance.Tests;

public class LoginTests : IClassFixture<LoginWebApplicationFactory>
{
    private readonly HttpClient client;

    public LoginTests(LoginWebApplicationFactory factory)
    {
        client = factory.CreateClient();
    }

    [Fact]
    public async Task Login_ComCredenciaisValidas_DevolveOkECookie()
    {
        var email = NewEmail();
        await RegisterAsync(email, "Password1!");

        var response = await client.PostAsJsonAsync("/auth/login", new
        {
            email,
            password = "Password1!"
        });
        var login = await response.Content.ReadFromJsonAsync<LoginResponseDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(login);
        Assert.Equal(email, login.Email);
        Assert.Contains(
            response.Headers.GetValues("Set-Cookie"),
            cookie => cookie.StartsWith("pf.auth=", StringComparison.Ordinal));

        var meResponse = await client.GetAsync("/auth/me");
        var me = await meResponse.Content.ReadFromJsonAsync<MeResponseDto>();

        Assert.Equal(HttpStatusCode.OK, meResponse.StatusCode);
        Assert.NotNull(me);
        Assert.Equal(email, me.Email);
    }

    [Fact]
    public async Task Login_ComPasswordInvalida_DevolveUnauthorized()
    {
        var email = NewEmail();
        await RegisterAsync(email, "Password1!");

        var response = await client.PostAsJsonAsync("/auth/login", new
        {
            email,
            password = "Password2!"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_SemLogin_DevolveUnauthorized()
    {
        var response = await client.GetAsync("/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Logout_ComLogin_InvalidaSessao()
    {
        var email = NewEmail();
        await RegisterAsync(email, "Password1!");

        var loginResponse = await client.PostAsJsonAsync("/auth/login", new
        {
            email,
            password = "Password1!"
        });
        loginResponse.EnsureSuccessStatusCode();

        var logoutResponse = await client.PostAsync("/auth/logout", content: null);
        var meResponse = await client.GetAsync("/auth/me");

        Assert.Equal(HttpStatusCode.NoContent, logoutResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, meResponse.StatusCode);
    }

    [Fact]
    public async Task Login_AposCincoPasswordsInvalidas_FicaBloqueado()
    {
        var email = NewEmail();
        await RegisterAsync(email, "Password1!");

        for (var attempt = 0; attempt < 5; attempt++)
        {
            var failedResponse = await client.PostAsJsonAsync("/auth/login", new
            {
                email,
                password = "Password2!"
            });

            Assert.Equal(HttpStatusCode.Unauthorized, failedResponse.StatusCode);
        }

        var blockedResponse = await client.PostAsJsonAsync("/auth/login", new
        {
            email,
            password = "Password1!"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, blockedResponse.StatusCode);
    }

    private async Task RegisterAsync(string email, string password)
    {
        var response = await client.PostAsJsonAsync("/auth/register", new
        {
            name = "Utilizador de Teste",
            email,
            password
        });

        response.EnsureSuccessStatusCode();
    }

    private static string NewEmail() => $"login.{Guid.NewGuid():N}@example.com";
}

public sealed class LoginWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string databaseName = $"login-tests-{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting(
            "ConnectionStrings:DefaultConnection",
            "Host=localhost;Port=5432;Database=login_tests;Username=test;Password=test");
        builder.ConfigureServices(services =>
        {
            var descriptor = services.Single(service =>
                service.ServiceType == typeof(DbContextOptions<AppDbContext>));
            services.Remove(descriptor);
            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(databaseName));
        });
    }
}