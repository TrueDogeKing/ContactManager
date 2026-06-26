using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ContactManager.Application.DTOs.Auth;
using ContactManager.Application.DTOs.Contacts;

namespace ContactManager.IntegrationTests;

public class ContactsApiTests : IntegrationTestBase
{
    public ContactsApiTests(ContactManagerApiFactory factory)
        : base(factory) { }

    private static string UniqueEmail() => $"it-{Guid.NewGuid():N}@example.com";

    private static CreateContactRequestDto NewContactRequest(
        int categoryId = 3,
        int? subcategoryId = null,
        string? customSubcategory = null,
        string? email = null
    ) =>
        new(
            "Test",
            "User",
            email ?? UniqueEmail(),
            "Password123!",
            "+48123456789",
            new DateOnly(1990, 1, 1),
            categoryId,
            subcategoryId,
            customSubcategory
        );

    // ----- reads (public) -----

    [Fact]
    public async Task GetAll_ReturnsSeededContacts()
    {
        var client = CreateClient();

        var contacts = await client.GetFromJsonAsync<List<ContactResponseDto>>("/api/contacts");

        Assert.NotNull(contacts);
        Assert.True(contacts!.Count >= 3);
        Assert.Contains(contacts, c => c.Email == "anna.kowalska@example.com");
    }

    [Fact]
    public async Task GetById_UnknownId_Returns404()
    {
        var client = CreateClient();

        var response = await client.GetAsync($"/api/contacts/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ----- auth gate -----

    [Fact]
    public async Task Create_WithoutAuthentication_Returns401()
    {
        var client = CreateClient();

        var response = await client.PostAsJsonAsync("/api/contacts", NewContactRequest());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ----- create -----

    [Fact]
    public async Task Create_DictionaryCategoryWithSubcategory_Returns201AndIsRetrievable()
    {
        var client = await CreateAuthenticatedClientAsync();
        var request = NewContactRequest(categoryId: 1, subcategoryId: 2); // Służbowy / Klient

        var response = await client.PostAsJsonAsync("/api/contacts", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<ContactResponseDto>();
        Assert.NotNull(created);
        Assert.Equal("Służbowy", created!.CategoryName);
        Assert.Equal("Klient", created.SubcategoryName);

        // Follow the Location header / id to confirm it was persisted.
        var fetched = await client.GetFromJsonAsync<ContactResponseDto>(
            $"/api/contacts/{created.Id}"
        );
        Assert.NotNull(fetched);
        Assert.Equal(request.Email, fetched!.Email);
    }

    [Fact]
    public async Task Create_CustomSubcategoryCategory_StoresTrimmedCustomText()
    {
        var client = await CreateAuthenticatedClientAsync();
        var request = NewContactRequest(categoryId: 3, customSubcategory: "  VIP  "); // Inny

        var response = await client.PostAsJsonAsync("/api/contacts", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<ContactResponseDto>();
        Assert.Equal("Inny", created!.CategoryName);
        Assert.Null(created.SubcategoryName);
        Assert.Equal("VIP", created.CustomSubcategory);
    }

    [Fact]
    public async Task Create_DictionaryCategoryWithoutSubcategory_Returns400()
    {
        var client = await CreateAuthenticatedClientAsync();
        var request = NewContactRequest(categoryId: 1, subcategoryId: null); // Służbowy requires a subcategory

        var response = await client.PostAsJsonAsync("/api/contacts", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_ProvisionsLogin_NewContactCanLogInWithItsOwnCredentials()
    {
        var client = await CreateAuthenticatedClientAsync();
        var email = UniqueEmail();
        var request = NewContactRequest(categoryId: 3, email: email); // password "Password123!"

        var createResponse = await client.PostAsJsonAsync("/api/contacts", request);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        // The contact's email + password should now work as login credentials.
        var anon = CreateClient();
        var loginResponse = await anon.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequestDto(email, "Password123!")
        );

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
    }

    [Fact]
    public async Task Create_DuplicateEmail_Returns409()
    {
        var client = await CreateAuthenticatedClientAsync();
        var request = NewContactRequest(categoryId: 3, email: "anna.kowalska@example.com"); // already seeded

        var response = await client.PostAsJsonAsync("/api/contacts", request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Create_InvalidPayload_Returns400()
    {
        var client = await CreateAuthenticatedClientAsync();
        var request = NewContactRequest(categoryId: 3) with { FirstName = "" };

        var response = await client.PostAsJsonAsync("/api/contacts", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ----- update + optimistic concurrency (xmin) -----

    [Fact]
    public async Task Update_WithStaleRowVersion_Returns409()
    {
        var client = await CreateAuthenticatedClientAsync();

        var createResponse = await client.PostAsJsonAsync(
            "/api/contacts",
            NewContactRequest(categoryId: 3)
        );
        var created = await createResponse.Content.ReadFromJsonAsync<ContactResponseDto>();

        var firstUpdate = new UpdateContactRequestDto(
            "Updated",
            "User",
            created!.Email,
            "+48123456789",
            new DateOnly(1990, 1, 1),
            3,
            null,
            null,
            created.RowVersion
        );

        var firstResponse = await client.PutAsJsonAsync($"/api/contacts/{created.Id}", firstUpdate);
        Assert.Equal(HttpStatusCode.NoContent, firstResponse.StatusCode);

        // Re-using the original (now stale) RowVersion must be rejected.
        var staleUpdate = firstUpdate with
        {
            FirstName = "Stale",
        };
        var staleResponse = await client.PutAsJsonAsync($"/api/contacts/{created.Id}", staleUpdate);

        Assert.Equal(HttpStatusCode.Conflict, staleResponse.StatusCode);
    }

    [Fact]
    public async Task Update_UnknownId_Returns404()
    {
        var client = await CreateAuthenticatedClientAsync();
        var request = new UpdateContactRequestDto(
            "Ghost",
            "User",
            UniqueEmail(),
            "+48123456789",
            new DateOnly(1990, 1, 1),
            3,
            null,
            null,
            RowVersion: 1
        );

        var response = await client.PutAsJsonAsync($"/api/contacts/{Guid.NewGuid()}", request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Update_ChangingEmail_MovesTheLoginAccount()
    {
        var client = await CreateAuthenticatedClientAsync();
        var oldEmail = UniqueEmail();
        var createResponse = await client.PostAsJsonAsync(
            "/api/contacts",
            NewContactRequest(categoryId: 3, email: oldEmail)
        );
        var created = await createResponse.Content.ReadFromJsonAsync<ContactResponseDto>();

        var newEmail = UniqueEmail();
        var update = new UpdateContactRequestDto(
            "Test",
            "User",
            newEmail,
            "+48123456789",
            new DateOnly(1990, 1, 1),
            3,
            null,
            null,
            created!.RowVersion
        );
        var updateResponse = await client.PutAsJsonAsync($"/api/contacts/{created.Id}", update);
        Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);

        var anon = CreateClient();
        // The new email logs in; the old one no longer works.
        var withNew = await anon.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequestDto(newEmail, "Password123!")
        );
        Assert.Equal(HttpStatusCode.OK, withNew.StatusCode);

        var withOld = await anon.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequestDto(oldEmail, "Password123!")
        );
        Assert.Equal(HttpStatusCode.Unauthorized, withOld.StatusCode);
    }

    // ----- delete -----

    [Fact]
    public async Task Delete_RemovesContact_Returns204ThenGet404()
    {
        var client = await CreateAuthenticatedClientAsync();
        var createResponse = await client.PostAsJsonAsync(
            "/api/contacts",
            NewContactRequest(categoryId: 3)
        );
        var created = await createResponse.Content.ReadFromJsonAsync<ContactResponseDto>();

        var deleteResponse = await client.DeleteAsync($"/api/contacts/{created!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await client.GetAsync($"/api/contacts/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_WithoutAuthentication_Returns401()
    {
        var client = CreateClient();

        var response = await client.DeleteAsync($"/api/contacts/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Delete_AlsoRemovesTheLoginAccount()
    {
        var client = await CreateAuthenticatedClientAsync();
        var email = UniqueEmail();
        var createResponse = await client.PostAsJsonAsync(
            "/api/contacts",
            NewContactRequest(categoryId: 3, email: email)
        );
        var created = await createResponse.Content.ReadFromJsonAsync<ContactResponseDto>();

        var anon = CreateClient();
        var loginBefore = await anon.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequestDto(email, "Password123!")
        );
        Assert.Equal(HttpStatusCode.OK, loginBefore.StatusCode);

        var deleteResponse = await client.DeleteAsync($"/api/contacts/{created!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // The login account is gone too.
        var loginAfter = await anon.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequestDto(email, "Password123!")
        );
        Assert.Equal(HttpStatusCode.Unauthorized, loginAfter.StatusCode);
    }

    // ----- change password (owner only) -----

    [Fact]
    public async Task ChangePassword_ByOwner_Succeeds_AndUpdatesLogin()
    {
        var admin = await CreateAuthenticatedClientAsync();
        var email = UniqueEmail();
        var createResponse = await admin.PostAsJsonAsync(
            "/api/contacts",
            NewContactRequest(categoryId: 3, email: email)
        );
        var created = await createResponse.Content.ReadFromJsonAsync<ContactResponseDto>();

        // Sign in as the contact itself and change its own password.
        var owner = await LoginAsAsync(email, "Password123!");
        var change = await owner.PutAsJsonAsync(
            $"/api/contacts/{created!.Id}/password",
            new ChangeContactPasswordRequestDto("NewPassword123!", created.RowVersion)
        );
        Assert.Equal(HttpStatusCode.NoContent, change.StatusCode);

        var anon = CreateClient();
        var withNew = await anon.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequestDto(email, "NewPassword123!")
        );
        Assert.Equal(HttpStatusCode.OK, withNew.StatusCode);

        var withOld = await anon.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequestDto(email, "Password123!")
        );
        Assert.Equal(HttpStatusCode.Unauthorized, withOld.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_ByDifferentUser_Returns403()
    {
        // Authenticated as admin (different email than the contact).
        var admin = await CreateAuthenticatedClientAsync();
        var email = UniqueEmail();
        var createResponse = await admin.PostAsJsonAsync(
            "/api/contacts",
            NewContactRequest(categoryId: 3, email: email)
        );
        var created = await createResponse.Content.ReadFromJsonAsync<ContactResponseDto>();

        var change = await admin.PutAsJsonAsync(
            $"/api/contacts/{created!.Id}/password",
            new ChangeContactPasswordRequestDto("NewPassword123!", created.RowVersion)
        );

        Assert.Equal(HttpStatusCode.Forbidden, change.StatusCode);
    }

    /// Logs in as the given user and returns a client carrying its bearer token.
    private async Task<HttpClient> LoginAsAsync(string email, string password)
    {
        var client = CreateClient();
        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequestDto(email, password)
        );
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            body!.Token
        );
        return client;
    }
}
