namespace ContactManager.Application.Models;

/// <summary>
/// Wygenerowany token dostępu (JWT) wraz z czasem wygaśnięcia.
/// </summary>
/// <param name="Token">Zakodowany token JWT.</param>
/// <param name="ExpiresAtUtc">Czas wygaśnięcia tokenu (UTC).</param>
public record AccessToken(string Token, DateTime ExpiresAtUtc);
