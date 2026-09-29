using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Portal.Api.Common;
using Portal.Application.Auth;

namespace Portal.Api.Controllers;

public sealed class AuthController(
    IAuthService authService,
    IValidator<LoginRequest> loginValidator,
    IWebHostEnvironment hostEnvironment) : ApiControllerBase
{
    private const string RefreshTokenCookieName = "refreshToken";

    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var validation = await loginValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return BadRequest(ApiResponse<object>.Fail(
                new ApiError
                {
                    Code = "VALIDATION_FAILED",
                    Message = "One or more fields are invalid.",
                    Details = validation.Errors.Select(e => new { field = e.PropertyName, message = e.ErrorMessage })
                },
                CorrelationId));
        }

        var result = await authService.LoginAsync(request, ct);
        if (!result.IsSuccess)
        {
            return Unauthorized(ApiResponse<object>.Fail(
                new ApiError { Code = result.ErrorCode!, Message = result.ErrorMessage! },
                CorrelationId));
        }

        var tokens = result.Value!;
        var currentUser = await authService.GetCurrentUserAsync(GetUserIdFromAccessToken(tokens.AccessToken), ct);

        SetRefreshTokenCookie(tokens);
        return Ok(ApiResponse<object>.Ok(
            new { accessToken = tokens.AccessToken, accessTokenExpiresAtUtc = tokens.AccessTokenExpiresAtUtc, user = currentUser },
            CorrelationId));
    }

    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(CancellationToken ct)
    {
        if (!Request.Cookies.TryGetValue(RefreshTokenCookieName, out var refreshToken) || string.IsNullOrEmpty(refreshToken))
        {
            return Unauthorized(ApiResponse<object>.Fail(
                new ApiError { Code = AuthErrorCodes.RefreshTokenInvalid, Message = "No refresh token was supplied." },
                CorrelationId));
        }

        var result = await authService.RefreshAsync(refreshToken, ct);
        if (!result.IsSuccess)
        {
            Response.Cookies.Delete(RefreshTokenCookieName, new CookieOptions { Path = "/api/v1/auth" });
            return Unauthorized(ApiResponse<object>.Fail(
                new ApiError { Code = result.ErrorCode!, Message = result.ErrorMessage! },
                CorrelationId));
        }

        var tokens = result.Value!;
        SetRefreshTokenCookie(tokens);
        return Ok(ApiResponse<object>.Ok(
            new { accessToken = tokens.AccessToken, accessTokenExpiresAtUtc = tokens.AccessTokenExpiresAtUtc },
            CorrelationId));
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        if (Request.Cookies.TryGetValue(RefreshTokenCookieName, out var refreshToken) && !string.IsNullOrEmpty(refreshToken))
        {
            await authService.LogoutAsync(refreshToken, ct);
        }

        Response.Cookies.Delete(RefreshTokenCookieName, new CookieOptions { Path = "/api/v1/auth" });
        return Ok(ApiResponse<object>.Ok(new { loggedOut = true }, CorrelationId));
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        var currentUser = await authService.GetCurrentUserAsync(GetUserId(), ct);
        if (currentUser is null)
        {
            return NotFound(ApiResponse<object>.Fail(
                new ApiError { Code = "USER_NOT_FOUND", Message = "Current user could not be resolved." },
                CorrelationId));
        }

        return Ok(ApiResponse<object>.Ok(currentUser, CorrelationId));
    }

    private void SetRefreshTokenCookie(AuthTokens tokens)
    {
        Response.Cookies.Append(RefreshTokenCookieName, tokens.RefreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = !hostEnvironment.IsDevelopment(),
            SameSite = SameSiteMode.Lax,
            Path = "/api/v1/auth",
            Expires = tokens.RefreshTokenExpiresAtUtc,
        });
    }

    // JwtBearerOptions.MapInboundClaims is disabled (Program.cs), so claim types
    // on User.Claims match exactly what AuthService put in the token — no remapping.
    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);

    private static Guid GetUserIdFromAccessToken(string accessToken)
    {
        var token = new JwtSecurityTokenHandler().ReadJwtToken(accessToken);
        var sub = token.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value;
        return Guid.Parse(sub);
    }
}
