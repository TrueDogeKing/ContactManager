using ContactManager.Application.DTOs.Auth;
using ContactManager.Application.Interfaces;
using ContactManager.Domain.Repositories;

namespace ContactManager.Application.Services;

/// <summary>
/// Implementacja <see cref="IAuthService"/>: weryfikuje dane logowania i wydaje token JWT.
/// </summary>
public class AuthService : IAuthService
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;

    /// <summary>Tworzy serwis z zależnościami.</summary>
    public AuthService(IUserRepository users, IPasswordHasher passwordHasher, ITokenService tokenService)
    {
        _users = users;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
    }

    /// <inheritdoc />
    public async Task<LoginResponseDto?> LoginAsync(
        LoginRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByEmailAsync(request.Email, cancellationToken);
        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            return null;
        }

        var token = _tokenService.CreateToken(user);
        return new LoginResponseDto(token.Token, token.ExpiresAtUtc, user.Email);
    }
}
