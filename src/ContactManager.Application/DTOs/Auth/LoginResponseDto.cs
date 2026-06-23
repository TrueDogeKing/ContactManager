namespace ContactManager.Application.DTOs.Auth;

/// <param name="Token">JWT token.</param>
public record LoginResponseDto(string Token, DateTime ExpiresAtUtc, string Email);
