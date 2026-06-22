namespace ContactManager.Application.DTOs.Auth;

/// <summary>Dane logowania.</summary>
/// <param name="Email">Adres e-mail użytkownika.</param>
/// <param name="Password">Hasło w postaci jawnej.</param>
public record LoginRequestDto(string Email, string Password);
