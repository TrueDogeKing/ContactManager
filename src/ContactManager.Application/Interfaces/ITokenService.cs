using ContactManager.Application.Models;
using ContactManager.Domain.Entities;

namespace ContactManager.Application.Interfaces;

public interface ITokenService
{
    AccessToken CreateAccessToken(User user);

    RefreshTokenInfo GenerateRefreshToken();

    /// Returns the hash (SHA-256) of the raw refresh token value.
    /// <param name="rawToken">The raw refresh token value.</param>
    string HashRefreshToken(string rawToken);
}
