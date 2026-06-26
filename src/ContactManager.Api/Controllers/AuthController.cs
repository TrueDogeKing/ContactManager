using ContactManager.Api.RateLimiting;
using ContactManager.Application.DTOs.Auth;
using ContactManager.Application.Interfaces;
using ContactManager.Application.Models;
using ContactManager.Infrastructure.Auth;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace ContactManager.Api.Controllers;

/// Authentication endpoints. Access token (JWT) is returned in the response body,
/// and refresh token is set in an HttpOnly cookie.
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IValidator<LoginRequestDto> _loginValidator;
    private readonly RefreshTokenSettings _refreshSettings;

    /// Creates controller with dependencies.
    public AuthController(
        IAuthService authService,
        IValidator<LoginRequestDto> loginValidator,
        IOptions<RefreshTokenSettings> refreshSettings)
    {
        _authService = authService;
        _loginValidator = loginValidator;
        _refreshSettings = refreshSettings.Value;
    }

    /// Logs in a user, returns an access token and sets a refresh token in an HttpOnly cookie.
    /// <param name="request">Login data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("login")]
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequestDto request,
        CancellationToken cancellationToken)
    {
        var validation = await _loginValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            foreach (var error in validation.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }

            return ValidationProblem(ModelState);
        }

        var result = await _authService.LoginAsync(request, cancellationToken);
        if (result is null)
        {
            return Unauthorized();
        }

        return IssueTokens(result);
    }

    /// Exchanges a refresh token (from cookie) for a new pair of tokens (rotation).
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        var refreshToken = Request.Cookies[_refreshSettings.CookieName];
        var result = await _authService.RefreshAsync(refreshToken, cancellationToken);
        if (result is null)
        {
            DeleteRefreshTokenCookie();
            return Unauthorized();
        }

        return IssueTokens(result);
    }

    /// Invalidates the refresh token (logout) and deletes the cookie. The operation is idempotent.
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var refreshToken = Request.Cookies[_refreshSettings.CookieName];
        await _authService.LogoutAsync(refreshToken, cancellationToken);
        DeleteRefreshTokenCookie();
        return NoContent();
    }

    private IActionResult IssueTokens(AuthResult result)
    {
        SetRefreshTokenCookie(result.RefreshToken, result.RefreshTokenExpiresAtUtc);
        return Ok(new LoginResponseDto(result.AccessToken, result.AccessTokenExpiresAtUtc, result.Email));
    }

    private void SetRefreshTokenCookie(string token, DateTime expiresAtUtc)
    {
        var options = BuildCookieOptions();
        options.Expires = expiresAtUtc;
        Response.Cookies.Append(_refreshSettings.CookieName, token, options);
    }

    private void DeleteRefreshTokenCookie() =>
        Response.Cookies.Delete(_refreshSettings.CookieName, BuildCookieOptions());

    private CookieOptions BuildCookieOptions() => new()
    {
        HttpOnly = true,
        Secure = _refreshSettings.CookieSecure,
        SameSite = Enum.Parse<SameSiteMode>(_refreshSettings.CookieSameSite, ignoreCase: true),
        Path = _refreshSettings.CookiePath,
        IsEssential = true
    };
}
