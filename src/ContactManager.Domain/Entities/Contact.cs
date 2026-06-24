namespace ContactManager.Domain.Entities;

public class Contact
{
    public Guid Id { get; set; }

    public required string FirstName { get; set; }

    public required string LastName { get; set; }

    public required string Email { get; set; }

    public required string PasswordHash { get; set; }

    public required string Phone { get; set; }

    public DateOnly BirthDate { get; set; }

    public int CategoryId { get; set; }

    public Category Category { get; set; } = null!;

    public int? SubcategoryId { get; set; }

    public Subcategory? Subcategory { get; set; }

    public string? CustomSubcategory { get; set; }

    /// <summary>Creation date of the contact (UTC).</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Last modification date of the contact (UTC).</summary>
    public DateTime? UpdatedAt { get; set; }

    /// (optimistic concurrency – race condition protection).
    public uint RowVersion { get; set; }
}
