using ContactManager.Application.DTOs.Contacts;

namespace ContactManager.Application.Interfaces;

/// Business logic for contacts: retrieval, creation, updates and deletion.
public interface IContactService
{
    /// Returns all contacts.
    Task<IReadOnlyList<ContactResponseDto>> GetAllAsync(
        CancellationToken cancellationToken = default
    );

    /// Returns the contact with the given id or null when it does not exist.
    Task<ContactResponseDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// Creates a new contact (hashes the password). Throws EmailConflictException when the email is taken.
    Task<ContactResponseDto> CreateAsync(
        CreateContactRequestDto request,
        CancellationToken cancellationToken = default
    );

    /// Updates an existing contact. Returns null when it does not exist. Throws EmailConflictException
    /// when the new email is taken by another contact and ConcurrencyConflictException on a RowVersion mismatch.
    Task<ContactResponseDto?> UpdateAsync(
        Guid id,
        UpdateContactRequestDto request,
        CancellationToken cancellationToken = default
    );

    /// Deletes a contact together with its login account. Returns false when it does not exist.
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// Changes a contact's password (rehashes it) and the matching login account. Returns false when
    /// the contact does not exist. Throws ForbiddenActionException when <paramref name="callerEmail"/>
    /// differs from the contact's email, and ConcurrencyConflictException on a RowVersion mismatch.
    Task<bool> ChangePasswordAsync(
        Guid id,
        ChangeContactPasswordRequestDto request,
        string callerEmail,
        CancellationToken cancellationToken = default
    );
}
