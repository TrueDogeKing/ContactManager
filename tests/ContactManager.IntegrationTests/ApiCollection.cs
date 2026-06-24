using System.Net.Http.Headers;
using System.Net.Http.Json;
using ContactManager.Application.DTOs.Auth;

namespace ContactManager.IntegrationTests;

/// All integration tests share a single API host + PostgreSQL container via this collection.
[CollectionDefinition(Name)]
public class ApiCollection : ICollectionFixture<ContactManagerApiFactory>
{
    public const string Name = "api";
}

/// Base class with the shared factory and HTTP helpers.
[Collection(ApiCollection.Name)]
public abstract class IntegrationTestBase
{
    // Seeded by DataSeeder (admin from appsettings "Admin" section).
    protected const string AdminEmail = "admin@contactmanager.local";
    protected const string AdminPassword = "Admin123!";

    protected ContactManagerApiFactory Factory { get; }

    protected IntegrationTestBase(ContactManagerApiFactory factory) => Factory = factory;

    /// An unauthenticated client.
    protected HttpClient CreateClient() => Factory.CreateClient();

    /// A client whose Authorization header carries a fresh admin access token.
    protected async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        var client = Factory.CreateClient();
        var response = await client.PostAsJsonAsync(
            "/api/auth/login", new LoginRequestDto(AdminEmail, AdminPassword));
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.Token);
        return client;
    }
}
