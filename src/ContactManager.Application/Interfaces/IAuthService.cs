using ContactManager.Application.DTOs.Auth;

namespace ContactManager.Application.Interfaces;

/// <summary>
/// Logika uwierzytelniania użytkowników.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Próbuje zalogować użytkownika. Zwraca token przy poprawnych danych,
    /// w przeciwnym razie <c>null</c>.
    /// </summary>
    /// <param name="request">Dane logowania.</param>
    /// <param name="cancellationToken">Token anulowania.</param>
    Task<LoginResponseDto?> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default);
}
