using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PersonalFinance.Api.Data;

namespace PersonalFinance.Tests;

public class RegisterTests : IClassFixture<RegisterWebApplicationFactory>
{
    private readonly HttpClient client;

    public RegisterTests(RegisterWebApplicationFactory factory)
    {
        client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_ComDadosValidos_DevolveCreated()
    {
        var response = await RegisterAsync();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<RegisterResponse>();
        Assert.NotNull(result);
        Assert.Equal("Utilizador de Teste", result.Name);
        Assert.NotEqual(Guid.Empty, result.Id);
    }

    [Fact]
    public async Task Register_ComPasswordCurta_DevolveBadRequest()
    {
        var response = await client.PostAsJsonAsync("/auth/register", new
        {
            name = "Utilizador de Teste",
            email = NewEmail(),
            password = "Pass1"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_ComEmailDuplicado_DevolveBadRequest()
    {
        var email = NewEmail();
        var request = new
        {
            name = "Utilizador de Teste",
            email,
            password = "Password1"
        };

        var firstResponse = await client.PostAsJsonAsync("/auth/register", request);
        var secondResponse = await client.PostAsJsonAsync("/auth/register", request);

        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, secondResponse.StatusCode);
    }

    private async Task<HttpResponseMessage> RegisterAsync()
    {
        return await client.PostAsJsonAsync("/auth/register", new
        {
            name = "Utilizador de Teste",
            email = NewEmail(),
            password = "Password1"
        });
    }

    private static string NewEmail() => $"teste.{Guid.NewGuid():N}@example.com";

    private sealed record RegisterResponse(Guid Id, string Name, string Email);
}

public sealed class RegisterWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string databaseName = $"register-tests-{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting(
            "ConnectionStrings:DefaultConnection",
            "Host=localhost;Port=5432;Database=register_tests;Username=test;Password=test");
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