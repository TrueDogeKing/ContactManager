using ContactManager.Domain.Entities;

namespace ContactManager.Domain.Repositories;

public interface IContactRepository
{
    /// Return all contacts
    Task<IReadOnlyList<Contact>> GetAllAsync(CancellationToken cancellationToken = default);

    /// Return contact by ID or <c>null</c> 
    Task<Contact?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// Return contact by email or <c>null</c> (for uniqueness checks).
    Task<Contact?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// Add a new contact and save changes.
    Task AddAsync(Contact contact, CancellationToken cancellationToken = default);

    /// <exception cref="Exceptions.ConcurrencyConflictException">
    /// Gdy kontakt został w międzyczasie zmodyfikowany przez kogoś innego.
    /// </exception>
    Task UpdateAsync(Contact contact, uint expectedRowVersion, CancellationToken cancellationToken = default);

    Task DeleteAsync(Contact contact, CancellationToken cancellationToken = default);
}
