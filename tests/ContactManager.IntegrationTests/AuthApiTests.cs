using System.Net;
using System.Net.Http.Json;
using ContactManager.Application.DTOs.Auth;

namespace ContactManager.IntegrationTests;

public class AuthApiTests : IntegrationTestBase
{
    public AuthApiTests(ContactManagerApiFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Login_WithValidAdminCredentials_ReturnsTokenAndSetsRefreshCookie()
    {
        var client = CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/login", new LoginRequestDto(AdminEmail, AdminPassword));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
        Assert.NotNull(body);
        Assert.False(string.IsNullOrWhiteSpace(body!.Token));
        Assert.Equal(3, body.Token.Split('.').Length); // header.payload.signature
        Assert.Equal(AdminEmail, body.Email);
        Assert.Contains(response.Headers, h => h.Key == "Set-Cookie");
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        var client = CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/login", new LoginRequestDto(AdminEmail, "WrongPassword1!"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithUnknownEmail_ReturnsUnauthorized()
    {
        var client = CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/login", new LoginRequestDto("nobody@example.com", "Whatever1!"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithInvalidPayload_ReturnsBadRequest()
    {
        var client = CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/login", new LoginRequestDto("", ""));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SeededContact_CanLogIn()
    {
        var client = CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/login", new LoginRequestDto("anna.kowalska@example.com", "Password123!"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
