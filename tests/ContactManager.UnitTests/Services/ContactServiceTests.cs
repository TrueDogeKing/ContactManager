using ContactManager.Application.DTOs.Contacts;
using ContactManager.Application.Interfaces;
using ContactManager.Application.Services;
using ContactManager.Domain.Entities;
using ContactManager.Domain.Exceptions;
using ContactManager.Domain.Repositories;
using NSubstitute;

namespace ContactManager.UnitTests.Services;

/// Unit tests for ContactService. All dependencies are substituted, so these tests exercise the
/// orchestration and the category/subcategory business rules in isolation (no database).
public class ContactServiceTests
{
    private readonly IContactRepository _contacts = Substitute.For<IContactRepository>();
    private readonly ICategoryRepository _categories = Substitute.For<ICategoryRepository>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly ContactService _sut;

    public ContactServiceTests()
    {
        _sut = new ContactService(_contacts, _categories, _users, _passwordHasher);
        // Deterministic, inspectable hash so tests can assert the password was hashed.
        _passwordHasher.Hash(Arg.Any<string>()).Returns(ci => "HASHED:" + ci.Arg<string>());
    }

    // ----- helpers -----

    private static Category Category(int id, string name, bool allowsCustom, params (int Id, string Name)[] subs)
    {
        var category = new Category { Id = id, Name = name, AllowsCustomSubcategory = allowsCustom };
        foreach (var (subId, subName) in subs)
        {
            category.Subcategories.Add(new Subcategory { Id = subId, Name = subName, CategoryId = id });
        }
        return category;
    }

    private void StubCategory(Category category) =>
        _categories.GetByIdWithSubcategoriesAsync(category.Id, Arg.Any<CancellationToken>())
            .Returns(category);

    /// Captures the contact passed to AddAsync and makes the post-add reload return it,
    /// so success-path Create tests can assert on the persisted entity.
    private Func<Contact> CaptureAddedContact()
    {
        Contact? added = null;
        _contacts.When(c => c.AddAsync(Arg.Any<Contact>(), Arg.Any<User>(), Arg.Any<CancellationToken>()))
            .Do(ci => added = ci.Arg<Contact>());
        _contacts.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(_ => added);
        return () => added!;
    }

    private static CreateContactRequestDto CreateRequest(
        int categoryId = 1,
        int? subcategoryId = null,
        string? customSubcategory = null,
        string email = "jan@example.com",
        string password = "password123") =>
        new("Jan", "Kowalski", email, password, "+48123456789",
            new DateOnly(1990, 1, 1), categoryId, subcategoryId, customSubcategory);

    private static UpdateContactRequestDto UpdateRequest(
        int categoryId = 1,
        int? subcategoryId = null,
        string? customSubcategory = null,
        string email = "jan@example.com",
        uint rowVersion = 1) =>
        new("Jan", "Kowalski", email, "+48123456789",
            new DateOnly(1990, 1, 1), categoryId, subcategoryId, customSubcategory, rowVersion);

    private static Contact ContactEntity(Guid id, string email = "jan@example.com") =>
        new()
        {
            Id = id,
            FirstName = "Jan",
            LastName = "Kowalski",
            Email = email,
            PasswordHash = "HASHED:old",
            Phone = "+48123456789",
            BirthDate = new DateOnly(1990, 1, 1),
            CategoryId = 1
        };

    // ----- category/subcategory rules (via CreateAsync) -----

    [Fact]
    public async Task CreateAsync_UnknownCategory_ThrowsBusinessRule()
    {
        _categories.GetByIdWithSubcategoriesAsync(99, Arg.Any<CancellationToken>())
            .Returns((Category?)null);

        await Assert.ThrowsAsync<BusinessRuleViolationException>(
            () => _sut.CreateAsync(CreateRequest(categoryId: 99)));
    }

    [Fact]
    public async Task CreateAsync_DictionaryCategory_MissingSubcategory_ThrowsBusinessRule()
    {
        StubCategory(Category(1, "Służbowy", allowsCustom: false, (10, "Klient"), (11, "Dostawca")));

        await Assert.ThrowsAsync<BusinessRuleViolationException>(
            () => _sut.CreateAsync(CreateRequest(categoryId: 1, subcategoryId: null)));
    }

    [Fact]
    public async Task CreateAsync_DictionaryCategory_ForeignSubcategory_ThrowsBusinessRule()
    {
        StubCategory(Category(1, "Służbowy", allowsCustom: false, (10, "Klient")));

        await Assert.ThrowsAsync<BusinessRuleViolationException>(
            () => _sut.CreateAsync(CreateRequest(categoryId: 1, subcategoryId: 999)));
    }

    [Fact]
    public async Task CreateAsync_DictionaryCategory_WithCustomText_ThrowsBusinessRule()
    {
        StubCategory(Category(1, "Służbowy", allowsCustom: false, (10, "Klient")));

        await Assert.ThrowsAsync<BusinessRuleViolationException>(
            () => _sut.CreateAsync(CreateRequest(categoryId: 1, subcategoryId: 10, customSubcategory: "vip")));
    }

    [Fact]
    public async Task CreateAsync_DictionaryCategory_ValidSubcategory_Persists()
    {
        StubCategory(Category(1, "Służbowy", allowsCustom: false, (10, "Klient")));
        var added = CaptureAddedContact();

        await _sut.CreateAsync(CreateRequest(categoryId: 1, subcategoryId: 10));

        Assert.Equal(10, added().SubcategoryId);
        Assert.Null(added().CustomSubcategory);
    }

    [Fact]
    public async Task CreateAsync_NonDictionaryCategory_WithSubcategoryId_ThrowsBusinessRule()
    {
        StubCategory(Category(2, "Prywatny", allowsCustom: false));

        await Assert.ThrowsAsync<BusinessRuleViolationException>(
            () => _sut.CreateAsync(CreateRequest(categoryId: 2, subcategoryId: 10)));
    }

    [Fact]
    public async Task CreateAsync_AllowsCustom_TrimsAndStoresCustomText()
    {
        StubCategory(Category(3, "Inny", allowsCustom: true));
        var added = CaptureAddedContact();

        await _sut.CreateAsync(CreateRequest(categoryId: 3, customSubcategory: "  Sąsiad  "));

        Assert.Equal("Sąsiad", added().CustomSubcategory);
        Assert.Null(added().SubcategoryId);
    }

    [Fact]
    public async Task CreateAsync_AllowsCustom_NullCustom_IsAccepted()
    {
        StubCategory(Category(3, "Inny", allowsCustom: true));
        var added = CaptureAddedContact();

        await _sut.CreateAsync(CreateRequest(categoryId: 3, customSubcategory: null));

        Assert.Null(added().CustomSubcategory);
    }

    [Fact]
    public async Task CreateAsync_AllowsCustom_WhitespaceCustom_NormalizedToNull()
    {
        StubCategory(Category(3, "Inny", allowsCustom: true));
        var added = CaptureAddedContact();

        await _sut.CreateAsync(CreateRequest(categoryId: 3, customSubcategory: "   "));

        Assert.Null(added().CustomSubcategory);
    }

    [Fact]
    public async Task CreateAsync_NonDictionary_NoCustomAllowed_WithCustomText_ThrowsBusinessRule()
    {
        StubCategory(Category(2, "Prywatny", allowsCustom: false));

        await Assert.ThrowsAsync<BusinessRuleViolationException>(
            () => _sut.CreateAsync(CreateRequest(categoryId: 2, customSubcategory: "x")));
    }

    [Fact]
    public async Task CreateAsync_PrivateCategory_NoSubcategoryNoCustom_Persists()
    {
        StubCategory(Category(2, "Prywatny", allowsCustom: false));
        var added = CaptureAddedContact();

        await _sut.CreateAsync(CreateRequest(categoryId: 2));

        Assert.Null(added().SubcategoryId);
        Assert.Null(added().CustomSubcategory);
    }

    // ----- CreateAsync: email + hashing -----

    [Fact]
    public async Task CreateAsync_EmailAlreadyExists_ThrowsAndDoesNotPersist()
    {
        _contacts.GetByEmailAsync("jan@example.com", Arg.Any<CancellationToken>())
            .Returns(ContactEntity(Guid.NewGuid()));

        await Assert.ThrowsAsync<EmailConflictException>(() => _sut.CreateAsync(CreateRequest()));

        await _contacts.DidNotReceive().AddAsync(Arg.Any<Contact>(), Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_EmailUsedByExistingUser_ThrowsAndDoesNotPersist()
    {
        _users.GetByEmailAsync("jan@example.com", Arg.Any<CancellationToken>())
            .Returns(new User { Id = Guid.NewGuid(), Email = "jan@example.com", PasswordHash = "x" });

        await Assert.ThrowsAsync<EmailConflictException>(() => _sut.CreateAsync(CreateRequest()));

        await _contacts.DidNotReceive().AddAsync(Arg.Any<Contact>(), Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_ProvisionsLoginUserWithSameEmailAndHash()
    {
        StubCategory(Category(3, "Inny", allowsCustom: true));
        var added = CaptureAddedContact();
        User? loginUser = null;
        _contacts.When(c => c.AddAsync(Arg.Any<Contact>(), Arg.Any<User>(), Arg.Any<CancellationToken>()))
            .Do(ci => loginUser = ci.Arg<User>());

        await _sut.CreateAsync(CreateRequest(categoryId: 3, email: "new@example.com", password: "Password123!"));

        Assert.NotNull(loginUser);
        Assert.Equal("new@example.com", loginUser!.Email);
        Assert.Equal("HASHED:Password123!", loginUser.PasswordHash);
        // The login shares the contact's password hash exactly.
        Assert.Equal(added().PasswordHash, loginUser.PasswordHash);
    }

    [Fact]
    public async Task CreateAsync_HashesPassword()
    {
        StubCategory(Category(3, "Inny", allowsCustom: true));
        var added = CaptureAddedContact();

        await _sut.CreateAsync(CreateRequest(categoryId: 3, password: "password123"));

        Assert.Equal("HASHED:password123", added().PasswordHash);
    }

    // ----- UpdateAsync -----

    [Fact]
    public async Task UpdateAsync_ContactNotFound_ReturnsNull()
    {
        var result = await _sut.UpdateAsync(Guid.NewGuid(), UpdateRequest());

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_EmailTakenByAnotherContact_Throws()
    {
        var id = Guid.NewGuid();
        _contacts.GetByIdAsync(id, Arg.Any<CancellationToken>())
            .Returns(ContactEntity(id, email: "old@example.com"));
        _contacts.GetByEmailAsync("new@example.com", Arg.Any<CancellationToken>())
            .Returns(ContactEntity(Guid.NewGuid(), email: "new@example.com"));

        await Assert.ThrowsAsync<EmailConflictException>(
            () => _sut.UpdateAsync(id, UpdateRequest(email: "new@example.com")));
    }

    [Fact]
    public async Task UpdateAsync_SameEmailIgnoringCase_SkipsUniquenessCheckAndUpdates()
    {
        var id = Guid.NewGuid();
        var existing = ContactEntity(id, email: "Jan@Example.com");
        _contacts.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(existing);
        StubCategory(Category(3, "Inny", allowsCustom: true));

        var result = await _sut.UpdateAsync(
            id, UpdateRequest(categoryId: 3, email: "jan@example.com", rowVersion: 42));

        Assert.NotNull(result);
        await _contacts.DidNotReceive().GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _contacts.Received(1).UpdateAsync(existing, 42u, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_AppliesFieldsAndForwardsRowVersion()
    {
        var id = Guid.NewGuid();
        var existing = ContactEntity(id, email: "jan@example.com");
        _contacts.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(existing);
        StubCategory(Category(3, "Inny", allowsCustom: true));

        await _sut.UpdateAsync(
            id, UpdateRequest(categoryId: 3, customSubcategory: "  Kolega  ", rowVersion: 7));

        Assert.Equal(3, existing.CategoryId);
        Assert.Equal("Kolega", existing.CustomSubcategory);
        Assert.NotNull(existing.UpdatedAt);
        await _contacts.Received(1).UpdateAsync(existing, 7u, Arg.Any<CancellationToken>());
    }

    // ----- DeleteAsync -----

    [Fact]
    public async Task DeleteAsync_NotFound_ReturnsFalse()
    {
        var result = await _sut.DeleteAsync(Guid.NewGuid());

        Assert.False(result);
        await _contacts.DidNotReceive().DeleteAsync(Arg.Any<Contact>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_Found_DeletesAndReturnsTrue()
    {
        var id = Guid.NewGuid();
        var contact = ContactEntity(id);
        _contacts.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(contact);

        var result = await _sut.DeleteAsync(id);

        Assert.True(result);
        await _contacts.Received(1).DeleteAsync(contact, Arg.Any<CancellationToken>());
    }

    // ----- ChangePasswordAsync -----

    [Fact]
    public async Task ChangePasswordAsync_NotFound_ReturnsFalse()
    {
        var result = await _sut.ChangePasswordAsync(
            Guid.NewGuid(), new ChangeContactPasswordRequestDto("newpass12", 1));

        Assert.False(result);
        await _contacts.DidNotReceive().UpdateAsync(Arg.Any<Contact>(), Arg.Any<uint>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ChangePasswordAsync_Found_RehashesAndForwardsRowVersion()
    {
        var id = Guid.NewGuid();
        var contact = ContactEntity(id);
        _contacts.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(contact);

        var result = await _sut.ChangePasswordAsync(
            id, new ChangeContactPasswordRequestDto("newpass12", 9));

        Assert.True(result);
        Assert.Equal("HASHED:newpass12", contact.PasswordHash);
        await _contacts.Received(1).UpdateAsync(contact, 9u, Arg.Any<CancellationToken>());
    }

    // ----- read + mapping -----

    [Fact]
    public async Task GetByIdAsync_NotFound_ReturnsNull()
    {
        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_MapsDictionarySubcategoryName()
    {
        var id = Guid.NewGuid();
        var contact = ContactEntity(id);
        contact.Category = new Category { Id = 1, Name = "Służbowy" };
        contact.SubcategoryId = 10;
        contact.Subcategory = new Subcategory { Id = 10, Name = "Klient", CategoryId = 1 };
        _contacts.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(contact);

        var result = await _sut.GetByIdAsync(id);

        Assert.NotNull(result);
        Assert.Equal("Służbowy", result!.CategoryName);
        Assert.Equal("Klient", result.SubcategoryName);
        Assert.Null(result.CustomSubcategory);
    }

    [Fact]
    public async Task GetByIdAsync_MapsCustomSubcategory()
    {
        var id = Guid.NewGuid();
        var contact = ContactEntity(id);
        contact.Category = new Category { Id = 3, Name = "Inny" };
        contact.CustomSubcategory = "Sąsiad";
        _contacts.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(contact);

        var result = await _sut.GetByIdAsync(id);

        Assert.NotNull(result);
        Assert.Equal("Inny", result!.CategoryName);
        Assert.Null(result.SubcategoryName);
        Assert.Equal("Sąsiad", result.CustomSubcategory);
    }

    [Fact]
    public async Task GetAllAsync_MapsAllContacts()
    {
        _contacts.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Contact>
        {
            ContactEntity(Guid.NewGuid(), "a@example.com"),
            ContactEntity(Guid.NewGuid(), "b@example.com")
        });

        var result = await _sut.GetAllAsync();

        Assert.Equal(2, result.Count);
    }
}
