using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace PersonalFinance.Tests;

// Testes HTTP com servidor ASP.NET Core em memória; não exigem API nem Docker a correr.
public sealed class SwaggerEndpointsTests
{
    private sealed class SwaggerTestFactory : WebApplicationFactory<Program>
{
    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureHostConfiguration(configuration =>
        {
            configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] =
                        "Host=127.0.0.1;Port=5432;Database=tests_dummy;Username=test;Password=test"
                });
        });

        return base.CreateHost(builder);
    }
}

private static WebApplicationFactory<Program> CreateFactory(
    string environment = "Development") =>
    new SwaggerTestFactory().WithWebHostBuilder(builder =>
    {
        builder.UseEnvironment(environment);
    });

    [Fact]
    public async Task SwaggerUi_InDevelopment_Returns200()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/swagger/index.html");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("swagger", html.ToLowerInvariant());
    }

    [Fact]
    public async Task SwaggerJson_DocumentsAllFiveAccountsOperations()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/swagger/v1/swagger.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var paths = document.RootElement.GetProperty("paths");
        var collection = paths.GetProperty("/api/accounts");
        var item = paths.GetProperty("/api/accounts/{id}");

        Assert.True(collection.TryGetProperty("get", out _));
        Assert.True(collection.TryGetProperty("post", out _));
        Assert.True(item.TryGetProperty("get", out _));
        Assert.True(item.TryGetProperty("put", out _));
        Assert.True(item.TryGetProperty("delete", out _));
    }

    [Fact]
    public async Task GetAccounts_WithoutAuthentication_Returns401()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        using var response = await client.GetAsync("/api/accounts");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SwaggerUi_InProduction_Returns404()
    {
        using var factory = CreateFactory("Production");
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/swagger/index.html");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
