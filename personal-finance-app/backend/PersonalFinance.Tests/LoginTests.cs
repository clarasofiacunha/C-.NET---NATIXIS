using System.Net;
using System.Net.Http.Json;

namespace PersonalFinance.Tests;

public class LoginTests : IClassFixture<RegisterWebApplicationFactory>
{
    private readonly HttpClient client;

    public LoginTests(RegisterWebApplicationFactory factory)
    {
        client = factory.CreateClient();
    }

    [Fact]
    public async Task Login_ComCredenciaisValidas_DevolveOk_EPermiteAcessoProtegido()
    {
        var email = NewEmail();
        var password = "Password1";
        await RegisterAsync(email, password);

        var loginResponse = await client.PostAsJsonAsync("/auth/login", new
        {
            email,
            password
        });

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var protectedResponse = await client.GetAsync("/api/accounts");
        Assert.Equal(HttpStatusCode.OK, protectedResponse.StatusCode);
    }

    [Fact]
    public async Task Login_ComPasswordInvalida_DevolveUnauthorized()
    {
        var email = NewEmail();
        await RegisterAsync(email, "Password1");

        var response = await client.PostAsJsonAsync("/auth/login", new
        {
            email,
            password = "Password2"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Logout_AposLogin_DevolveNoContent_ERevogaSessao()
    {
        var email = NewEmail();
        var password = "Password1";
        await RegisterAsync(email, password);
        await client.PostAsJsonAsync("/auth/login", new { email, password });

        var logoutResponse = await client.PostAsync("/auth/logout", null);
        Assert.Equal(HttpStatusCode.NoContent, logoutResponse.StatusCode);

        var protectedResponse = await client.GetAsync("/api/accounts");
        Assert.Equal(HttpStatusCode.Unauthorized, protectedResponse.StatusCode);
    }

    private async Task RegisterAsync(string email, string password)
    {
        var response = await client.PostAsJsonAsync("/auth/register", new
        {
            name = "Utilizador de Teste",
            email,
            password
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private static string NewEmail() => $"login.{Guid.NewGuid():N}@example.com";
}
