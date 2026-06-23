namespace ContactManager.Application.Models;

/// <param name="AccessToken">Access token (JWT).</param>
/// <param name="AccessTokenExpiresAtUtc">Access token expiration time (UTC).</param>
/// <param name="Email">Email of the authenticated user.</param>
/// <param name="RefreshToken">Plaintext refresh token value.</param>
/// <param name="RefreshTokenExpiresAtUtc">Refresh token expiration time (UTC).</param>
public record AuthResult(
    string AccessToken,
    DateTime AccessTokenExpiresAtUtc,
    string Email,
    string RefreshToken,
    DateTime RefreshTokenExpiresAtUtc);
