using System.Net;
using System.Net.Http.Json;
using ContactManager.Application.DTOs.Contacts;

namespace ContactManager.IntegrationTests;

public class ContactsApiTests : IntegrationTestBase
{
    public ContactsApiTests(ContactManagerApiFactory factory) : base(factory)
    {
    }

    private static string UniqueEmail() => $"it-{Guid.NewGuid():N}@example.com";

    private static CreateContactRequestDto NewContactRequest(
        int categoryId = 3,
        int? subcategoryId = null,
        string? customSubcategory = null,
        string? email = null) =>
        new("Test", "User", email ?? UniqueEmail(), "Password123!", "+48123456789",
            new DateOnly(1990, 1, 1), categoryId, subcategoryId, customSubcategory);

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
        var fetched = await client.GetFromJsonAsync<ContactResponseDto>($"/api/contacts/{created.Id}");
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

        var createResponse = await client.PostAsJsonAsync("/api/contacts", NewContactRequest(categoryId: 3));
        var created = await createResponse.Content.ReadFromJsonAsync<ContactResponseDto>();

        var firstUpdate = new UpdateContactRequestDto(
            "Updated", "User", created!.Email, "+48123456789",
            new DateOnly(1990, 1, 1), 3, null, null, created.RowVersion);

        var firstResponse = await client.PutAsJsonAsync($"/api/contacts/{created.Id}", firstUpdate);
        Assert.Equal(HttpStatusCode.NoContent, firstResponse.StatusCode);

        // Re-using the original (now stale) RowVersion must be rejected.
        var staleUpdate = firstUpdate with { FirstName = "Stale" };
        var staleResponse = await client.PutAsJsonAsync($"/api/contacts/{created.Id}", staleUpdate);

        Assert.Equal(HttpStatusCode.Conflict, staleResponse.StatusCode);
    }

    [Fact]
    public async Task Update_UnknownId_Returns404()
    {
        var client = await CreateAuthenticatedClientAsync();
        var request = new UpdateContactRequestDto(
            "Ghost", "User", UniqueEmail(), "+48123456789",
            new DateOnly(1990, 1, 1), 3, null, null, RowVersion: 1);

        var response = await client.PutAsJsonAsync($"/api/contacts/{Guid.NewGuid()}", request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ----- delete -----

    [Fact]
    public async Task Delete_RemovesContact_Returns204ThenGet404()
    {
        var client = await CreateAuthenticatedClientAsync();
        var createResponse = await client.PostAsJsonAsync("/api/contacts", NewContactRequest(categoryId: 3));
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
}
