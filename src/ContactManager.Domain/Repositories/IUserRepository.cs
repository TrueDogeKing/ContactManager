using ContactManager.Domain.Entities;

namespace ContactManager.Domain.Repositories;

/// <summary>
/// Repozytorium użytkowników (kont logowania).
/// </summary>
public interface IUserRepository
{
    /// <summary>Zwraca użytkownika o podanym adresie e-mail lub <c>null</c>.</summary>
    /// <param name="email">Adres e-mail.</param>
    /// <param name="cancellationToken">Token anulowania.</param>
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
}
