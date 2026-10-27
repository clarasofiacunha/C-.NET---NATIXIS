using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PersonalFinance.Api.Data;

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

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(
            response.Headers.GetValues("Set-Cookie"),
            cookie => cookie.StartsWith("pf.auth=", StringComparison.Ordinal));
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