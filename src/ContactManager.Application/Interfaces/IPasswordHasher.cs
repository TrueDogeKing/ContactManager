namespace ContactManager.Application.Interfaces;

/// <summary>
/// Abstrakcja haszowania i weryfikacji haseł.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>Zwraca hash podanego hasła.</summary>
    /// <param name="password">Hasło w postaci jawnej.</param>
    string Hash(string password);

    /// <summary>Weryfikuje, czy hasło odpowiada zapisanemu hashowi.</summary>
    /// <param name="password">Hasło w postaci jawnej.</param>
    /// <param name="passwordHash">Zapisany hash hasła.</param>
    bool Verify(string password, string passwordHash);
}
