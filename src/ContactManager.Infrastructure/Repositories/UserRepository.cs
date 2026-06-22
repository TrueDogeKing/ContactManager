using ContactManager.Domain.Entities;
using ContactManager.Domain.Repositories;
using ContactManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ContactManager.Infrastructure.Repositories;

/// <summary>
/// Implementacja <see cref="IUserRepository"/> oparta o <see cref="AppDbContext"/>.
/// </summary>
public class UserRepository : IUserRepository
{
    private readonly AppDbContext _db;

    /// <summary>Tworzy repozytorium z kontekstem bazy danych.</summary>
    public UserRepository(AppDbContext db) => _db = db;

    /// <inheritdoc />
    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        _db.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
}
