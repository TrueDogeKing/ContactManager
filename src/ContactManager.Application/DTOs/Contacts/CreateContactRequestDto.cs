namespace ContactManager.Application.DTOs.Contacts;

/// Input data for creating a new contact. Password is provided in plain text and gets hashed.
public record CreateContactRequestDto(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    string Phone,
    DateOnly BirthDate,
    int CategoryId,
    int? SubcategoryId,
    string? CustomSubcategory
);
