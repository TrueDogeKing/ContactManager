namespace ContactManager.Domain.Entities;

/// Contact category (e.g. Służbowy, Prywatny, Inny). Dictionary data stored in the database.
public class Category
{
    /// Unique category identifier.
    public int Id { get; set; }

    /// Category name (unique).
    public required string Name { get; set; }

    /// Whether the category allows entering a free-text subcategory (e.g. "Inny")
    /// instead of selecting one from the dictionary.
    public bool AllowsCustomSubcategory { get; set; }

    /// Subcategories belonging to this category.
    public ICollection<Subcategory> Subcategories { get; set; } = new List<Subcategory>();

    /// Contacts assigned to this category.
    public ICollection<Contact> Contacts { get; set; } = new List<Contact>();
}
