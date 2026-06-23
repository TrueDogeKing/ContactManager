namespace ContactManager.Application.DTOs.Contacts;

/// <summary>Reprezentacja kontaktu zwracana przez API. Nigdy nie zawiera hashu hasła.</summary>
/// <param name="Id">Unikalny identyfikator kontaktu.</param>
/// <param name="FirstName">Imię.</param>
/// <param name="LastName">Nazwisko.</param>
/// <param name="Email">Adres e-mail.</param>
/// <param name="Phone">Numer telefonu.</param>
/// <param name="BirthDate">Data urodzenia.</param>
/// <param name="CategoryId">Identyfikator kategorii.</param>
/// <param name="CategoryName">Nazwa kategorii.</param>
/// <param name="SubcategoryId">Identyfikator podkategorii (opcjonalny).</param>
/// <param name="SubcategoryName">Nazwa podkategorii (opcjonalna).</param>
/// <param name="CustomSubcategory">Dowolny tekst podkategorii.</param>
/// <param name="CreatedAt">Data utworzenia (UTC).</param>
/// <param name="UpdatedAt">Data ostatniej modyfikacji (UTC).</param>
/// <param name="RowVersion">Token współbieżności do przekazania przy aktualizacji.</param>
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
