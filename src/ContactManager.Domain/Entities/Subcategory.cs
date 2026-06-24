namespace ContactManager.Domain.Entities;

public class Subcategory
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public int CategoryId { get; set; }

    public Category Category { get; set; } = null!;

    public ICollection<Contact> Contacts { get; set; } = new List<Contact>();
}