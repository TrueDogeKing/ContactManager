using ContactManager.Domain.Entities;

namespace ContactManager.Domain.Repositories;

/// Data access for contacts.
public interface IContactRepository
{
    /// Returns all contacts (with dictionaries included, read-only).
    Task<IReadOnlyList<Contact>> GetAllAsync(CancellationToken cancellationToken = default);

    /// Returns the contact with the given id or null (tracked, with dictionaries included).
    Task<Contact?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// Returns the contact with the given email or null (for uniqueness checks).
    Task<Contact?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// Adds a new contact and saves changes.
    Task AddAsync(Contact contact, CancellationToken cancellationToken = default);

    /// Saves changes to a tracked contact with concurrency control. expectedRowVersion is the token
    /// the client fetched on read. Throws ConcurrencyConflictException when the contact was modified meanwhile.
    Task UpdateAsync(Contact contact, uint expectedRowVersion, CancellationToken cancellationToken = default);

    /// Removes a contact and saves changes.
    Task DeleteAsync(Contact contact, CancellationToken cancellationToken = default);
}
