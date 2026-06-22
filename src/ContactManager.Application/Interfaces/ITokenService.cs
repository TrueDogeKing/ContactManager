using ContactManager.Application.Models;
using ContactManager.Domain.Entities;

namespace ContactManager.Application.Interfaces;

/// <summary>
/// Abstrakcja generowania tokenów dostępu (JWT) dla użytkowników.
/// </summary>
public interface ITokenService
{
    /// <summary>Tworzy token dostępu dla wskazanego użytkownika.</summary>
    /// <param name="user">Użytkownik, dla którego generowany jest token.</param>
    AccessToken CreateToken(User user);
}
