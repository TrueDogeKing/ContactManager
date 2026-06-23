using ContactManager.Domain.Entities;
using ContactManager.Domain.Exceptions;
using ContactManager.Domain.Repositories;
using ContactManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ContactManager.Infrastructure.Repositories;

/// Implementation of IContactRepository backed by AppDbContext.
public class ContactRepository : IContactRepository
{
    private readonly AppDbContext _db;

    /// Creates repository with database context.
    public ContactRepository(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<Contact>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _db.Contacts
            .AsNoTracking()
            .Include(c => c.Category)
            .Include(c => c.Subcategory)
            .ToListAsync(cancellationToken);

    public Task<Contact?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.Contacts
            .Include(c => c.Category)
            .Include(c => c.Subcategory)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<Contact?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        _db.Contacts.FirstOrDefaultAsync(c => c.Email == email, cancellationToken);

    public async Task AddAsync(Contact contact, CancellationToken cancellationToken = default)
    {
        await _db.Contacts.AddAsync(contact, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(
        Contact contact,
        uint expectedRowVersion,
        CancellationToken cancellationToken = default)
    {
        // Optimistic concurrency: compare the client's read token against the current state in the database.
        _db.Entry(contact).Property(c => c.RowVersion).OriginalValue = expectedRowVersion;

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConcurrencyConflictException(
                "The contact was modified by another process.", ex);
        }
    }

    public async Task DeleteAsync(Contact contact, CancellationToken cancellationToken = default)
    {
        _db.Contacts.Remove(contact);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
