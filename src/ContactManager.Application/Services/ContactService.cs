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
    private readonly IPasswordHasher _passwordHasher;

    /// Creates service with dependencies.
    public ContactService(IContactRepository contacts, IPasswordHasher passwordHasher)
    {
        _contacts = contacts;
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
            CustomSubcategory = request.CustomSubcategory,
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

        contact.FirstName = request.FirstName;
        contact.LastName = request.LastName;
        contact.Email = request.Email;
        contact.Phone = request.Phone;
        contact.BirthDate = request.BirthDate;
        contact.CategoryId = request.CategoryId;
        contact.SubcategoryId = request.SubcategoryId;
        contact.CustomSubcategory = request.CustomSubcategory;
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
