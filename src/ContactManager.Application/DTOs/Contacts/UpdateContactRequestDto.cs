namespace ContactManager.Application.DTOs.Contacts;

/// <summary>Dane wejściowe do aktualizacji istniejącego kontaktu. Hasła nie da się zmienić tą drogą.</summary>
/// <param name="FirstName">Imię.</param>
/// <param name="LastName">Nazwisko.</param>
/// <param name="Email">Adres e-mail (musi być unikalny).</param>
/// <param name="Phone">Numer telefonu.</param>
/// <param name="BirthDate">Data urodzenia.</param>
/// <param name="CategoryId">Identyfikator kategorii.</param>
/// <param name="SubcategoryId">Identyfikator podkategorii (opcjonalny).</param>
/// <param name="CustomSubcategory">Dowolny tekst podkategorii (dla kategorii „Inny”).</param>
/// <param name="RowVersion">Token współbieżności pobrany przy odczycie (optimistic concurrency → 409 przy konflikcie).</param>
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
