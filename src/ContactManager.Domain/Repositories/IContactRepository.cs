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

    /// Adds a new contact together with its login account (User) in a single transaction and
    /// saves changes. The login lets the contact sign in afterwards with its own email + password.
    Task AddAsync(Contact contact, User loginUser, CancellationToken cancellationToken = default);

    /// Saves changes to a tracked contact with concurrency control. expectedRowVersion is the token
    /// the client fetched on read. Throws ConcurrencyConflictException when the contact was modified meanwhile.
    Task UpdateAsync(
        Contact contact,
        uint expectedRowVersion,
        CancellationToken cancellationToken = default
    );

    /// Removes a contact together with its login account (when present), in a single transaction,
    /// and saves changes.
    Task DeleteAsync(
        Contact contact,
        User? loginUser,
        CancellationToken cancellationToken = default
    );
}
