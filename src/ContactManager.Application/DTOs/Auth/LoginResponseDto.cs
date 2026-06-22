namespace ContactManager.Application.DTOs.Auth;

/// <summary>Odpowiedź po poprawnym zalogowaniu.</summary>
/// <param name="Token">Token JWT.</param>
/// <param name="ExpiresAtUtc">Czas wygaśnięcia tokenu (UTC).</param>
/// <param name="Email">Adres e-mail zalogowanego użytkownika.</param>
public record LoginResponseDto(string Token, DateTime ExpiresAtUtc, string Email);
