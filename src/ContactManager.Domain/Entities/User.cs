namespace ContactManager.Domain.Entities;

public class User
{
    public Guid Id { get; set; }

    public required string Email { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public required string PasswordHash { get; set; }

    /// Create date (UTC).
    public DateTime CreatedAt { get; set; }

    public uint RowVersion { get; set; }
}
