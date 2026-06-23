namespace ContactManager.Application.DTOs.Contacts;

/// <param name="CreatedAt">Creation date (UTC).</param>
/// <param name="UpdatedAt">Date of last modification (UTC).</param>
/// <param name="RowVersion">Concurrency token for update operations.</param>
public record ContactResponseDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string Phone,
    DateOnly BirthDate,
    int CategoryId,
    string CategoryName,
    int? SubcategoryId,
    string? SubcategoryName,
    string? CustomSubcategory,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    uint RowVersion);
