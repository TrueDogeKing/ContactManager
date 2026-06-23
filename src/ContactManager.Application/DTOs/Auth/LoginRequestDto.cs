namespace ContactManager.Application.DTOs.Auth;

/// <param name="Password">Password in plain text.</param>
public record LoginRequestDto(string Email, string Password);
