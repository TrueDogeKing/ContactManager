using ContactManager.Application.DTOs.Auth;
using ContactManager.Application.Models;

namespace ContactManager.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResult?> LoginAsync(
        LoginRequestDto request,
        CancellationToken cancellationToken = default
    );

    /// Exchanges a refresh token for a new token pair (rotation). Returns null if the token
    /// is unknown, expired, or revoked. Reuse of a rotated token is treated as theft and
    /// revokes all user sessions.
    /// <param name="rawRefreshToken">Plaintext refresh token value.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<AuthResult?> RefreshAsync(
        string? rawRefreshToken,
        CancellationToken cancellationToken = default
    );

    Task LogoutAsync(string? rawRefreshToken, CancellationToken cancellationToken = default);
}
