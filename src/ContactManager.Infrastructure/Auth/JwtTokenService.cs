using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ContactManager.Application.Interfaces;
using ContactManager.Application.Models;
using ContactManager.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ContactManager.Infrastructure.Auth;

/// Implementation of <see cref="ITokenService"/>: generates signed JWT tokens (HMAC-SHA256)
/// and cryptographically random refresh tokens.
public class JwtTokenService : ITokenService
{
    private const int RefreshTokenBytes = 32;

    private readonly JwtSettings _settings;
    private readonly RefreshTokenSettings _refreshSettings;

    ///Creates service with JWT and refresh token settings.
    public JwtTokenService(IOptions<JwtSettings> settings, IOptions<RefreshTokenSettings> refreshSettings)
    {
        _settings = settings.Value;
        _refreshSettings = refreshSettings.Value;
    }

    public AccessToken CreateAccessToken(User user)
    {
        var expiresAtUtc = DateTime.UtcNow.AddMinutes(_settings.ExpiryMinutes);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        var encoded = new JwtSecurityTokenHandler().WriteToken(token);
        return new AccessToken(encoded, expiresAtUtc);
    }

    public RefreshTokenInfo GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(RefreshTokenBytes);
        var rawToken = Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

        var expiresAtUtc = DateTime.UtcNow.AddDays(_refreshSettings.ExpiryDays);
        return new RefreshTokenInfo(rawToken, HashRefreshToken(rawToken), expiresAtUtc);
    }

    public string HashRefreshToken(string rawToken)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexString(hash);
    }
}
