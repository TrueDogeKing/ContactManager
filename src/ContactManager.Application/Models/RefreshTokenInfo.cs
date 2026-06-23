namespace ContactManager.Application.Models;

/// Generated refresh token: plaintext value (for client), its hash (for database),
/// and expiration time.
/// <param name="RawToken">Plaintext token value – delivered to the client once.</param>
/// <param name="TokenHash">SHA-256 hash of the plaintext – stored in the database.</param>
/// <param name="ExpiresAtUtc">Token expiration time (UTC).</param>
public record RefreshTokenInfo(string RawToken, string TokenHash, DateTime ExpiresAtUtc);
