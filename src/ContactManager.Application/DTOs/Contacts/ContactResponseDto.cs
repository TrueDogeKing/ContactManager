namespace ContactManager.Application.DTOs.Contacts;

/// Contact representation returned by the API. Never contains the password hash.
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
    uint RowVersion
);
