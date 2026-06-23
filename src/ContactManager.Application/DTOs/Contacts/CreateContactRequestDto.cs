namespace ContactManager.Application.DTOs.Contacts;

/// <summary>Dane wejściowe do utworzenia nowego kontaktu.</summary>
/// <param name="FirstName">Imię.</param>
/// <param name="LastName">Nazwisko.</param>
/// <param name="Email">Adres e-mail (musi być unikalny).</param>
/// <param name="Password">Hasło kontaktu w postaci jawnej (zostanie zahashowane).</param>
/// <param name="Phone">Numer telefonu.</param>
/// <param name="BirthDate">Data urodzenia.</param>
/// <param name="CategoryId">Identyfikator kategorii.</param>
/// <param name="SubcategoryId">Identyfikator podkategorii (opcjonalny).</param>
/// <param name="CustomSubcategory">Dowolny tekst podkategorii (dla kategorii „Inny”).</param>
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
