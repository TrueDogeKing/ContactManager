using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ContactManager.Application.Interfaces;
using ContactManager.Application.Models;
using ContactManager.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ContactManager.Infrastructure.Auth;

/// <summary>
/// Implementacja <see cref="ITokenService"/> generująca podpisane tokeny JWT (HMAC-SHA256).
/// </summary>
public class JwtTokenService : ITokenService
{
    private readonly JwtSettings _settings;

    /// <summary>Tworzy serwis z ustawieniami JWT.</summary>
    public JwtTokenService(IOptions<JwtSettings> settings) => _settings = settings.Value;

    /// <inheritdoc />
    public AccessToken CreateToken(User user)
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
}
