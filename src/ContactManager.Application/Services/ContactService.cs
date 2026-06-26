using ContactManager.Application.DTOs.Contacts;
using ContactManager.Application.Interfaces;
using ContactManager.Domain.Entities;
using ContactManager.Domain.Exceptions;
using ContactManager.Domain.Repositories;

namespace ContactManager.Application.Services;

/// Implementation of IContactService: orchestrates the contact repository, hashes passwords
/// and maps between entities and DTOs.
///
/// A contact and its login account (User) are kept in sync: creating a contact provisions a matching
/// login (same email + password), updating the email or password updates both, and deleting a contact
/// removes the login too.
public class ContactService : IContactService
{
    private readonly IContactRepository _contacts;
    private readonly ICategoryRepository _categories;
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _passwordHasher;

    /// Creates service with dependencies.
    public ContactService(
        IContactRepository contacts,
        ICategoryRepository categories,
        IUserRepository users,
        IPasswordHasher passwordHasher
    )
    {
        _contacts = contacts;
        _categories = categories;
        _users = users;
        _passwordHasher = passwordHasher;
    }

    public async Task<IReadOnlyList<ContactResponseDto>> GetAllAsync(
        CancellationToken cancellationToken = default
    )
    {
        var contacts = await _contacts.GetAllAsync(cancellationToken);
        return contacts.Select(ToResponse).ToList();
    }

    public async Task<ContactResponseDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default
    )
    {
        var contact = await _contacts.GetByIdAsync(id, cancellationToken);
        return contact is null ? null : ToResponse(contact);
    }

    public async Task<ContactResponseDto> CreateAsync(
        CreateContactRequestDto request,
        CancellationToken cancellationToken = default
    )
    {
        var existing = await _contacts.GetByEmailAsync(request.Email, cancellationToken);
        if (existing is not null)
        {
            throw new EmailConflictException(
                $"A contact with email '{request.Email}' already exists."
            );
        }

        // The email becomes a login too, so it must also be free in the Users table.
        var existingUser = await _users.GetByEmailAsync(request.Email, cancellationToken);
        if (existingUser is not null)
        {
            throw new EmailConflictException(
                $"A user with email '{request.Email}' already exists."
            );
        }

        var customSubcategory = await ValidateCategorySelectionAsync(
            request.CategoryId,
            request.SubcategoryId,
            request.CustomSubcategory,
            cancellationToken
        );

        var passwordHash = _passwordHasher.Hash(request.Password);
        var createdAt = DateTime.UtcNow;

        var contact = new Contact
        {
            Id = Guid.NewGuid(),
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            PasswordHash = passwordHash,
            Phone = request.Phone,
            BirthDate = request.BirthDate,
            CategoryId = request.CategoryId,
            SubcategoryId = request.SubcategoryId,
            CustomSubcategory = customSubcategory,
            CreatedAt = createdAt,
        };

        // Matching login account so the contact can sign in with its own email + password
        // (the password captured on the contact form).
        var loginUser = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            PasswordHash = passwordHash,
            CreatedAt = createdAt,
        };

        await _contacts.AddAsync(contact, loginUser, cancellationToken);

        // Reload so navigation properties (Category/Subcategory names) are populated for the response.
        var created = await _contacts.GetByIdAsync(contact.Id, cancellationToken);
        return ToResponse(created!);
    }

    public async Task<ContactResponseDto?> UpdateAsync(
        Guid id,
        UpdateContactRequestDto request,
        CancellationToken cancellationToken = default
    )
    {
        var contact = await _contacts.GetByIdAsync(id, cancellationToken);
        if (contact is null)
        {
            return null;
        }

        var oldEmail = contact.Email;
        var emailChanged = !string.Equals(
            oldEmail,
            request.Email,
            StringComparison.OrdinalIgnoreCase
        );

        if (emailChanged)
        {
            // The new email must be free among both contacts and login accounts.
            var contactWithEmail = await _contacts.GetByEmailAsync(
                request.Email,
                cancellationToken
            );
            if (contactWithEmail is not null && contactWithEmail.Id != id)
            {
                throw new EmailConflictException(
                    $"A contact with email '{request.Email}' already exists."
                );
            }

            var userWithEmail = await _users.GetByEmailAsync(request.Email, cancellationToken);
            if (userWithEmail is not null)
            {
                throw new EmailConflictException(
                    $"A user with email '{request.Email}' already exists."
                );
            }
        }

        var customSubcategory = await ValidateCategorySelectionAsync(
            request.CategoryId,
            request.SubcategoryId,
            request.CustomSubcategory,
            cancellationToken
        );

        contact.FirstName = request.FirstName;
        contact.LastName = request.LastName;
        contact.Email = request.Email;
        contact.Phone = request.Phone;
        contact.BirthDate = request.BirthDate;
        contact.CategoryId = request.CategoryId;
        contact.SubcategoryId = request.SubcategoryId;
        contact.CustomSubcategory = customSubcategory;
        contact.UpdatedAt = DateTime.UtcNow;

        // Keep the login account's email in sync with the contact's.
        if (emailChanged)
        {
            var loginUser = await _users.GetByEmailAsync(oldEmail, cancellationToken);
            if (loginUser is not null)
            {
                loginUser.Email = request.Email;
            }
        }

        await _contacts.UpdateAsync(contact, request.RowVersion, cancellationToken);

        // Reload to pick up the refreshed RowVersion and navigation names.
        var updated = await _contacts.GetByIdAsync(id, cancellationToken);
        return ToResponse(updated!);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var contact = await _contacts.GetByIdAsync(id, cancellationToken);
        if (contact is null)
        {
            return false;
        }

        // Remove the matching login account too (contact = account).
        var loginUser = await _users.GetByEmailAsync(contact.Email, cancellationToken);
        await _contacts.DeleteAsync(contact, loginUser, cancellationToken);
        return true;
    }

    public async Task<bool> ChangePasswordAsync(
        Guid id,
        ChangeContactPasswordRequestDto request,
        string callerEmail,
        CancellationToken cancellationToken = default
    )
    {
        var contact = await _contacts.GetByIdAsync(id, cancellationToken);
        if (contact is null)
        {
            return false;
        }

        // Only the signed-in owner (same email) may change a contact's password.
        if (!string.Equals(contact.Email, callerEmail, StringComparison.OrdinalIgnoreCase))
        {
            throw new ForbiddenActionException(
                "You can only change the password of your own account."
            );
        }

        var passwordHash = _passwordHasher.Hash(request.NewPassword);
        contact.PasswordHash = passwordHash;
        contact.UpdatedAt = DateTime.UtcNow;

        // Keep the login account's password in sync.
        var loginUser = await _users.GetByEmailAsync(contact.Email, cancellationToken);
        if (loginUser is not null)
        {
            loginUser.PasswordHash = passwordHash;
        }

        await _contacts.UpdateAsync(contact, request.RowVersion, cancellationToken);
        return true;
    }

    /// Validates the category/subcategory selection against the dictionary in the database and
    /// returns the normalized custom subcategory (trimmed, or null when empty). Rules:
    /// a category that has dictionary subcategories requires SubcategoryId from that same category
    /// and forbids custom text; a category that allows custom subcategory accepts free text only;
    /// any other category accepts neither. Throws BusinessRuleViolationException on a mismatch.
    private async Task<string?> ValidateCategorySelectionAsync(
        int categoryId,
        int? subcategoryId,
        string? customSubcategory,
        CancellationToken cancellationToken
    )
    {
        var category = await _categories.GetByIdWithSubcategoriesAsync(
            categoryId,
            cancellationToken
        );
        if (category is null)
        {
            throw new BusinessRuleViolationException($"Category {categoryId} does not exist.");
        }

        var normalizedCustom = string.IsNullOrWhiteSpace(customSubcategory)
            ? null
            : customSubcategory.Trim();
        var hasDictionarySubcategories = category.Subcategories.Count > 0;

        if (hasDictionarySubcategories)
        {
            if (subcategoryId is null)
            {
                throw new BusinessRuleViolationException(
                    $"Category '{category.Name}' requires a subcategory from the dictionary."
                );
            }

            if (category.Subcategories.All(s => s.Id != subcategoryId))
            {
                throw new BusinessRuleViolationException(
                    $"Subcategory {subcategoryId} does not belong to category '{category.Name}'."
                );
            }

            if (normalizedCustom is not null)
            {
                throw new BusinessRuleViolationException(
                    $"Category '{category.Name}' does not allow a custom subcategory."
                );
            }

            return null;
        }

        // Category without dictionary subcategories: a dictionary subcategory is never valid.
        if (subcategoryId is not null)
        {
            throw new BusinessRuleViolationException(
                $"Category '{category.Name}' does not have dictionary subcategories."
            );
        }

        if (category.AllowsCustomSubcategory)
        {
            // Custom text is optional for these categories (e.g. "Inny").
            return normalizedCustom;
        }

        // Any other category (e.g. "Prywatny") allows neither a dictionary nor a custom subcategory.
        if (normalizedCustom is not null)
        {
            throw new BusinessRuleViolationException(
                $"Category '{category.Name}' does not allow a subcategory."
            );
        }

        return null;
    }

    /// Maps a contact entity to its API representation. Never exposes the password hash.
    private static ContactResponseDto ToResponse(Contact contact) =>
        new(
            contact.Id,
            contact.FirstName,
            contact.LastName,
            contact.Email,
            contact.Phone,
            contact.BirthDate,
            contact.CategoryId,
            contact.Category?.Name ?? string.Empty,
            contact.SubcategoryId,
            contact.Subcategory?.Name,
            contact.CustomSubcategory,
            contact.CreatedAt,
            contact.UpdatedAt,
            contact.RowVersion
        );
}
