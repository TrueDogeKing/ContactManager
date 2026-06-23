using ContactManager.Domain.Entities;
using ContactManager.Domain.Repositories;
using ContactManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ContactManager.Infrastructure.Repositories;

/// Implementation of <see cref="IRefreshTokenRepository"/> using <see cref="AppDbContext"/>.
public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly AppDbContext _db;

    /// Creates repository with database context.
    public RefreshTokenRepository(AppDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task AddAsync(RefreshToken token, CancellationToken cancellationToken = default) =>
        await _db.RefreshTokens.AddAsync(token, cancellationToken);

    /// <inheritdoc />
    public Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default) =>
        _db.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

    /// <inheritdoc />
    public async Task RevokeAllActiveForUserAsync(
        Guid userId,
        DateTime revokedAtUtc,
        CancellationToken cancellationToken = default)
    {
        var activeTokens = await _db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);

        foreach (var token in activeTokens)
        {
            token.RevokedAtUtc = revokedAtUtc;
        }
    }

    /// <inheritdoc />
    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _db.SaveChangesAsync(cancellationToken);
}
