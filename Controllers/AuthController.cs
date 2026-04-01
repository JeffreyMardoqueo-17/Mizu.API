using Microsoft.AspNetCore.Mvc;
using Muzu.Api.Core.DTOs;
using Muzu.Api.Core.Interfaces.Service;

namespace Muzu.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register-tenant")]
    public async Task<IActionResult> RegisterTenant([FromBody] TenantUsuarioRegistroDto request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _authService.RegistrarPrimerTenantYUsuarioAsync(request, cancellationToken);
            AppendAuthCookies(result.AccessToken, result.RefreshToken);
            return Ok(result.Response);
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Conflicto de negocio",
                Detail = exception.Message,
                Status = StatusCodes.Status409Conflict
            });
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto request, CancellationToken cancellationToken)
    {
        var result = await _authService.LoginAsync(request, cancellationToken);
        if (result is null)
        {
            return Unauthorized();
        }

        AppendAuthCookies(result.AccessToken, result.Response.RefreshToken);
        return Ok(result.Response);
    }

    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _authService.RefreshTokenAsync(request.RefreshToken, cancellationToken);
        if (result is null)
        {
            return Unauthorized();
        }

        AppendAuthCookies(result.AccessToken, result.Response.RefreshToken);
        return Ok(result.Response);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] LogoutRequestDto request, CancellationToken cancellationToken)
    {
        await _authService.LogoutAsync(request.RefreshToken, cancellationToken);
        Response.Cookies.Delete("muzu_token", BuildCookieOptions());
        Response.Cookies.Delete("muzu_refresh_token", BuildCookieOptions());
        return NoContent();
    }

    private void AppendAuthCookies(string accessToken, string refreshToken)
    {
        Response.Cookies.Append("muzu_token", accessToken, BuildCookieOptions());
        Response.Cookies.Append("muzu_refresh_token", refreshToken, BuildCookieOptions());
    }

    private CookieOptions BuildCookieOptions()
    {
        return new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Strict,
            IsEssential = true
        };
    }
}
