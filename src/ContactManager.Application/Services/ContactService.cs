using ContactManager.Application.DTOs.Contacts;
using ContactManager.Application.Interfaces;
using ContactManager.Domain.Entities;
using ContactManager.Domain.Exceptions;
using ContactManager.Domain.Repositories;

namespace ContactManager.Application.Services;

/// Implementation of IContactService: orchestrates the contact repository, hashes passwords
/// and maps between entities and DTOs.
public class ContactService : IContactService
{
    private readonly IContactRepository _contacts;
    private readonly ICategoryRepository _categories;
    private readonly IPasswordHasher _passwordHasher;

    /// Creates service with dependencies.
    public ContactService(
        IContactRepository contacts,
        ICategoryRepository categories,
        IPasswordHasher passwordHasher)
    {
        _contacts = contacts;
        _categories = categories;
        _passwordHasher = passwordHasher;
    }

    public async Task<IReadOnlyList<ContactResponseDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var contacts = await _contacts.GetAllAsync(cancellationToken);
        return contacts.Select(ToResponse).ToList();
    }

    public async Task<ContactResponseDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var contact = await _contacts.GetByIdAsync(id, cancellationToken);
        return contact is null ? null : ToResponse(contact);
    }

    public async Task<ContactResponseDto> CreateAsync(
        CreateContactRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var existing = await _contacts.GetByEmailAsync(request.Email, cancellationToken);
        if (existing is not null)
        {
            throw new EmailConflictException($"A contact with email '{request.Email}' already exists.");
        }

        var customSubcategory = await ValidateCategorySelectionAsync(
            request.CategoryId, request.SubcategoryId, request.CustomSubcategory, cancellationToken);

        var contact = new Contact
        {
            Id = Guid.NewGuid(),
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            PasswordHash = _passwordHasher.Hash(request.Password),
            Phone = request.Phone,
            BirthDate = request.BirthDate,
            CategoryId = request.CategoryId,
            SubcategoryId = request.SubcategoryId,
            CustomSubcategory = customSubcategory,
            CreatedAt = DateTime.UtcNow
        };

        await _contacts.AddAsync(contact, cancellationToken);

        // Reload so navigation properties (Category/Subcategory names) are populated for the response.
        var created = await _contacts.GetByIdAsync(contact.Id, cancellationToken);
        return ToResponse(created!);
    }

    public async Task<ContactResponseDto?> UpdateAsync(
        Guid id,
        UpdateContactRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var contact = await _contacts.GetByIdAsync(id, cancellationToken);
        if (contact is null)
        {
            return null;
        }

        // Reject duplicate email when it now points to a different contact.
        if (!string.Equals(contact.Email, request.Email, StringComparison.OrdinalIgnoreCase))
        {
            var withEmail = await _contacts.GetByEmailAsync(request.Email, cancellationToken);
            if (withEmail is not null && withEmail.Id != id)
            {
                throw new EmailConflictException($"A contact with email '{request.Email}' already exists.");
            }
        }

        var customSubcategory = await ValidateCategorySelectionAsync(
            request.CategoryId, request.SubcategoryId, request.CustomSubcategory, cancellationToken);

        contact.FirstName = request.FirstName;
        contact.LastName = request.LastName;
        contact.Email = request.Email;
        contact.Phone = request.Phone;
        contact.BirthDate = request.BirthDate;
        contact.CategoryId = request.CategoryId;
        contact.SubcategoryId = request.SubcategoryId;
        contact.CustomSubcategory = customSubcategory;
        contact.UpdatedAt = DateTime.UtcNow;

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

        await _contacts.DeleteAsync(contact, cancellationToken);
        return true;
    }

    public async Task<bool> ChangePasswordAsync(
        Guid id,
        ChangeContactPasswordRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var contact = await _contacts.GetByIdAsync(id, cancellationToken);
        if (contact is null)
        {
            return false;
        }

        contact.PasswordHash = _passwordHasher.Hash(request.NewPassword);
        contact.UpdatedAt = DateTime.UtcNow;

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
        CancellationToken cancellationToken)
    {
        var category = await _categories.GetByIdWithSubcategoriesAsync(categoryId, cancellationToken);
        if (category is null)
        {
            throw new BusinessRuleViolationException($"Category {categoryId} does not exist.");
        }

        var normalizedCustom = string.IsNullOrWhiteSpace(customSubcategory) ? null : customSubcategory.Trim();
        var hasDictionarySubcategories = category.Subcategories.Count > 0;

        if (hasDictionarySubcategories)
        {
            if (subcategoryId is null)
            {
                throw new BusinessRuleViolationException(
                    $"Category '{category.Name}' requires a subcategory from the dictionary.");
            }

            if (category.Subcategories.All(s => s.Id != subcategoryId))
            {
                throw new BusinessRuleViolationException(
                    $"Subcategory {subcategoryId} does not belong to category '{category.Name}'.");
            }

            if (normalizedCustom is not null)
            {
                throw new BusinessRuleViolationException(
                    $"Category '{category.Name}' does not allow a custom subcategory.");
            }

            return null;
        }

        // Category without dictionary subcategories: a dictionary subcategory is never valid.
        if (subcategoryId is not null)
        {
            throw new BusinessRuleViolationException(
                $"Category '{category.Name}' does not have dictionary subcategories.");
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
                $"Category '{category.Name}' does not allow a subcategory.");
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
            contact.RowVersion);
}
