namespace ContactManager.Infrastructure.Auth;

/// <summary>
/// Ustawienia tokenów JWT (wiązane z sekcją konfiguracji "Jwt").
/// </summary>
public class JwtSettings
{
    /// <summary>Nazwa sekcji konfiguracji.</summary>
    public const string SectionName = "Jwt";

    /// <summary>Wystawca tokenu.</summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>Odbiorca tokenu.</summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>Klucz podpisujący (symetryczny, min. 32 znaki dla HMAC-SHA256).</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Czas ważności tokenu w minutach.</summary>
    public int ExpiryMinutes { get; set; } = 60;
}
