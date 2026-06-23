namespace ContactManager.Application.DTOs.Contacts;

/// <param name="RowVersion"> Concurrency token fetched during read (optimistic concurrency → 409 when conflict occurs).</param>
public record UpdateContactRequestDto(
    string FirstName,
    string LastName,
    string Email,
    string Phone,
    DateOnly BirthDate,
    int CategoryId,
    int? SubcategoryId,
    string? CustomSubcategory,
    uint RowVersion);
