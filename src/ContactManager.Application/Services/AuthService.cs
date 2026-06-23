using ContactManager.Application.DTOs.Auth;
using ContactManager.Application.Interfaces;
using ContactManager.Application.Models;
using ContactManager.Domain.Entities;
using ContactManager.Domain.Repositories;

namespace ContactManager.Application.Services;

/// Implementation of <see cref="IAuthService"/>: verifies credentials, issues access and refresh tokens,
/// and handles rotation and revocation.
public class AuthService : IAuthService
{
    private readonly IUserRepository _users;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;

    /// Creates service with dependencies.
    public AuthService(
        IUserRepository users,
        IRefreshTokenRepository refreshTokens,
        IPasswordHasher passwordHasher,
        ITokenService tokenService)
    {
        _users = users;
        _refreshTokens = refreshTokens;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
    }

    public async Task<AuthResult?> LoginAsync(
        LoginRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByEmailAsync(request.Email, cancellationToken);
        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            return null;
        }

        return await IssueTokensAsync(user, cancellationToken);
    }

    public async Task<AuthResult?> RefreshAsync(
        string? rawRefreshToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rawRefreshToken))
        {
            return null;
        }

        var tokenHash = _tokenService.HashRefreshToken(rawRefreshToken);
        var stored = await _refreshTokens.GetByTokenHashAsync(tokenHash, cancellationToken);
        if (stored is null)
        {
            return null;
        }

        var now = DateTime.UtcNow;

        // Reuse of a revoked (rotated) token = possible theft.
        // Action: revoke all active sessions for this user.
        if (stored.RevokedAtUtc is not null)
        {
            await _refreshTokens.RevokeAllActiveForUserAsync(stored.UserId, now, cancellationToken);
            await _refreshTokens.SaveChangesAsync(cancellationToken);
            return null;
        }

        if (stored.ExpiresAtUtc <= now || stored.User is null)
        {
            return null;
        }

        // Rotation: current token is revoked and replaced with a new one.
        var refresh = _tokenService.GenerateRefreshToken();
        await _refreshTokens.AddAsync(
            CreateTokenEntity(stored.UserId, refresh, now),
            cancellationToken);

        stored.RevokedAtUtc = now;
        stored.ReplacedByTokenHash = refresh.TokenHash;
        await _refreshTokens.SaveChangesAsync(cancellationToken);

        var access = _tokenService.CreateAccessToken(stored.User);
        return new AuthResult(
            access.Token,
            access.ExpiresAtUtc,
            stored.User.Email,
            refresh.RawToken,
            refresh.ExpiresAtUtc);
    }

    public async Task LogoutAsync(
        string? rawRefreshToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rawRefreshToken))
        {
            return;
        }

        var tokenHash = _tokenService.HashRefreshToken(rawRefreshToken);
        var stored = await _refreshTokens.GetByTokenHashAsync(tokenHash, cancellationToken);
        if (stored is null || stored.RevokedAtUtc is not null)
        {
            return;
        }

        stored.RevokedAtUtc = DateTime.UtcNow;
        await _refreshTokens.SaveChangesAsync(cancellationToken);
    }

    private async Task<AuthResult> IssueTokensAsync(User user, CancellationToken cancellationToken)
    {
        var access = _tokenService.CreateAccessToken(user);
        var refresh = _tokenService.GenerateRefreshToken();

        await _refreshTokens.AddAsync(
            CreateTokenEntity(user.Id, refresh, DateTime.UtcNow),
            cancellationToken);
        await _refreshTokens.SaveChangesAsync(cancellationToken);

        return new AuthResult(
            access.Token,
            access.ExpiresAtUtc,
            user.Email,
            refresh.RawToken,
            refresh.ExpiresAtUtc);
    }

    private static RefreshToken CreateTokenEntity(Guid userId, RefreshTokenInfo info, DateTime createdAtUtc) =>
        new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = info.TokenHash,
            ExpiresAtUtc = info.ExpiresAtUtc,
            CreatedAtUtc = createdAtUtc
        };
}
