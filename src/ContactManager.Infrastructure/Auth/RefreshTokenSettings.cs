namespace ContactManager.Infrastructure.Auth;

public class RefreshTokenSettings
{
    public const string SectionName = "RefreshToken";

    public int ExpiryDays { get; set; } = 7;

    public string CookieName { get; set; } = "refreshToken";

    /// <summary>Whether the cookie has the Secure flag (requires HTTPS). Always true in production.</summary>
    public bool CookieSecure { get; set; } = true;

    /// <summary>Cookie SameSite mode ("Strict", "Lax", or "None").</summary>
    public string CookieSameSite { get; set; } = "Strict";

    public string CookiePath { get; set; } = "/api/auth";
}
