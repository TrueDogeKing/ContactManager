namespace ContactManager.Domain.Entities;

/// <summary>
/// Kategoria kontaktu (np. Służbowy, Prywatny, Inny). Dane słownikowe przechowywane w bazie.
/// </summary>
public class Category
{
    /// <summary>Unikalny identyfikator kategorii.</summary>
    public int Id { get; set; }

    /// <summary>Nazwa kategorii (unikalna).</summary>
    public required string Name { get; set; }

    /// <summary>Podkategorie należące do tej kategorii.</summary>
    public ICollection<Subcategory> Subcategories { get; set; } = new List<Subcategory>();

    /// <summary>Kontakty przypisane do tej kategorii.</summary>
    public ICollection<Contact> Contacts { get; set; } = new List<Contact>();
}
