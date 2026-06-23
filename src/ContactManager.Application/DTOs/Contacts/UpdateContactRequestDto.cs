namespace ContactManager.Application.DTOs.Contacts;

/// Input data for updating a contact. The password cannot be changed this way.
/// RowVersion is the concurrency token fetched during read (optimistic concurrency, conflict returns 409).
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
