using ContactManager.Domain.Entities;

namespace ContactManager.Domain.Repositories;

public interface IRefreshTokenRepository
{
    /// Adds a new token to the context. Changes are persisted only by <see cref="SaveChangesAsync"/>.
    /// <param name="token">Token to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AddAsync(RefreshToken token, CancellationToken cancellationToken = default);

    /// Returns the token with the given hash (including the related user) or null.
    /// <param name="tokenHash">Token hash.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    Task RevokeAllActiveForUserAsync(Guid userId, DateTime revokedAtUtc, CancellationToken cancellationToken = default);

    /// Persists changes to the database.
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
