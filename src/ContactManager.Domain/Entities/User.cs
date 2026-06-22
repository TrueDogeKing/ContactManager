namespace ContactManager.Domain.Entities;

/// <summary>
/// Konto użytkownika (operatora) uprawnione do logowania i zarządzania kontaktami.
/// </summary>
public class User
{
    /// <summary>Unikalny identyfikator użytkownika.</summary>
    public Guid Id { get; set; }

    /// <summary>Adres e-mail (unikalny) – służy jako login.</summary>
    public required string Email { get; set; }

    /// <summary>Hash hasła. Hasło nigdy nie jest przechowywane jawnie.</summary>
    public required string PasswordHash { get; set; }

    /// <summary>Data utworzenia konta (UTC).</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Token współbieżności mapowany na systemową kolumnę PostgreSQL <c>xmin</c>
    /// (optimistic concurrency).
    /// </summary>
    public uint RowVersion { get; set; }
}
