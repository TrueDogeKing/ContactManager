namespace ContactManager.Domain.Entities;

/// <summary>
/// Kontakt w książce telefonicznej.
/// </summary>
public class Contact
{
    /// <summary>Unikalny identyfikator kontaktu.</summary>
    public Guid Id { get; set; }

    /// <summary>Imię.</summary>
    public required string FirstName { get; set; }

    /// <summary>Nazwisko.</summary>
    public required string LastName { get; set; }

    /// <summary>Adres e-mail (unikalny).</summary>
    public required string Email { get; set; }

    /// <summary>Hash hasła kontaktu. Hasło nigdy nie jest przechowywane jawnie.</summary>
    public required string PasswordHash { get; set; }

    /// <summary>Numer telefonu.</summary>
    public required string Phone { get; set; }

    /// <summary>Data urodzenia.</summary>
    public DateOnly BirthDate { get; set; }

    /// <summary>Identyfikator kategorii.</summary>
    public int CategoryId { get; set; }

    /// <summary>Kategoria kontaktu.</summary>
    public Category Category { get; set; } = null!;

    /// <summary>Identyfikator podkategorii (opcjonalny – tylko dla kategorii z predefiniowanymi podkategoriami).</summary>
    public int? SubcategoryId { get; set; }

    /// <summary>Podkategoria kontaktu (opcjonalna).</summary>
    public Subcategory? Subcategory { get; set; }

    /// <summary>Dowolny tekst podkategorii używany, gdy kategoria to „Inny”.</summary>
    public string? CustomSubcategory { get; set; }

    /// <summary>Data utworzenia kontaktu (UTC).</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Data ostatniej modyfikacji (UTC).</summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Token współbieżności mapowany na systemową kolumnę PostgreSQL <c>xmin</c>
    /// (optimistic concurrency – ochrona przed race conditions przy edycji).
    /// </summary>
    public uint RowVersion { get; set; }
}
