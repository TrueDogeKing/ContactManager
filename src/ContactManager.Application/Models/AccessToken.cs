namespace ContactManager.Application.Models;

public record AccessToken(string Token, DateTime ExpiresAtUtc);
