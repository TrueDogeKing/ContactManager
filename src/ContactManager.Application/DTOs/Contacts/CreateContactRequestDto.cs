namespace ContactManager.Application.DTOs.Contacts;

public record CreateContactRequestDto(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    string Phone,
    DateOnly BirthDate,
    int CategoryId,
    int? SubcategoryId,
    string? CustomSubcategory);
