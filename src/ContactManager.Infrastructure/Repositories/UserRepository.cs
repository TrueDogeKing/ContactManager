using ContactManager.Domain.Entities;
using ContactManager.Domain.Repositories;
using ContactManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ContactManager.Infrastructure.Repositories;

/// Implementacja <see cref="IUserRepository"/> oparta o <see cref="AppDbContext"/>.
public class UserRepository : IUserRepository
{
    private readonly AppDbContext _db;

    /// Creates repository with database context.
    public UserRepository(AppDbContext db) => _db = db;

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        _db.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
}
