using ContactManager.Application.Interfaces;

namespace ContactManager.Infrastructure.Auth;

/// <summary>
/// Implementacja <see cref="IPasswordHasher"/> oparta o algorytm BCrypt.
/// </summary>
public class BcryptPasswordHasher : IPasswordHasher
{
    /// <inheritdoc />
    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password);

    /// <inheritdoc />
    public bool Verify(string password, string passwordHash) =>
        BCrypt.Net.BCrypt.Verify(password, passwordHash);
}
