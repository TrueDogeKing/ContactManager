namespace ContactManager.Domain.Entities;

/// <summary>
/// Podkategoria kontaktu powiązana z kategorią
/// (np. Szef, Klient, Pracownik, Kontrahent dla kategorii „Służbowy”).
/// </summary>
public class Subcategory
{
    /// <summary>Unikalny identyfikator podkategorii.</summary>
    public int Id { get; set; }

    /// <summary>Nazwa podkategorii (unikalna w obrębie kategorii).</summary>
    public required string Name { get; set; }

    /// <summary>Identyfikator kategorii nadrzędnej.</summary>
    public int CategoryId { get; set; }

    /// <summary>Kategoria nadrzędna.</summary>
    public Category Category { get; set; } = null!;

    /// <summary>Kontakty przypisane do tej podkategorii.</summary>
    public ICollection<Contact> Contacts { get; set; } = new List<Contact>();
}