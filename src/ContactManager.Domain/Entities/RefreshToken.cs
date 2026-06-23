namespace ContactManager.Domain.Entities;

/// Refresh token bound to a user session. Stored in the database only as a hash;
/// the plaintext value is delivered once to the client (HttpOnly cookie).
public class RefreshToken
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public User? User { get; set; }

    /// SHA-256 hash of the plaintext token value. The plaintext is never stored.
    public required string TokenHash { get; set; }

    public DateTime ExpiresAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? RevokedAtUtc { get; set; }

    /// Hash of the token that replaced this one during rotation (if applicable).
    /// Enables tracing the rotation chain and detecting reuse.
    public string? ReplacedByTokenHash { get; set; }

    /// Token is active if not revoked and not expired.
    public bool IsActive => RevokedAtUtc is null && DateTime.UtcNow < ExpiresAtUtc;
}
