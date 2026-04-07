using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Muzu.Api.Core.DTOs;
using Muzu.Api.Core.Interfaces.Service;
using Muzu.Api.Core.Rules;

namespace Muzu.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IAuthSecurityService _authSecurityService;

    public AuthController(IAuthService authService, IAuthSecurityService authSecurityService)
    {
        _authService = authService;
        _authSecurityService = authSecurityService;
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
        try
        {
            var result = await _authService.LoginAsync(request, cancellationToken);
            if (result is null)
            {
                return Unauthorized();
            }

            AppendAuthCookies(result.AccessToken, result.Response.RefreshToken);
            return Ok(result.Response);
        }
        catch (DirectivaAccessBlockedException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                code = "DIRECTIVA_NOT_ACTIVE",
                message = ex.Message,
                statusCode = StatusCodes.Status403Forbidden
            });
        }
    }

    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _authService.RefreshTokenAsync(request.RefreshToken, cancellationToken);
            if (result is null)
            {
                return Unauthorized();
            }

            AppendAuthCookies(result.AccessToken, result.Response.RefreshToken);
            return Ok(result.Response);
        }
        catch (DirectivaAccessBlockedException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                code = "DIRECTIVA_NOT_ACTIVE",
                message = ex.Message,
                statusCode = StatusCodes.Status403Forbidden
            });
        }
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] LogoutRequestDto request, CancellationToken cancellationToken)
    {
        await _authService.LogoutAsync(request.RefreshToken, cancellationToken);
        Response.Cookies.Delete("muzu_token", BuildCookieOptions());
        Response.Cookies.Delete("muzu_refresh_token", BuildCookieOptions());
        return NoContent();
    }

    [Authorize]
    [HttpPost("change-temporary-password")]
    public async Task<IActionResult> ChangeTemporaryPassword([FromBody] ChangeTemporaryPasswordRequestDto request, CancellationToken cancellationToken)
    {
        if (!TryGetAuthContext(out var actorUsuarioId, out var actorTenantId))
        {
            return Unauthorized();
        }

        var changed = await _authSecurityService.ChangeTemporaryPasswordAsync(actorUsuarioId, actorTenantId, request.NewPassword, cancellationToken);
        return changed ? NoContent() : NotFound();
    }

    [Authorize]
    [HttpPost("regenerate-temp-password")]
    public async Task<IActionResult> RegenerateTemporaryPassword([FromBody] RegenerateTemporaryPasswordRequestDto request, CancellationToken cancellationToken)
    {
        if (!TryGetAuthContext(out var actorUsuarioId, out var actorTenantId))
        {
            return Unauthorized();
        }

        try
        {
            var response = await _authSecurityService.RegenerateTemporaryPasswordAsync(actorUsuarioId, actorTenantId, request.UsuarioId, cancellationToken);
            return Ok(response);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Conflicto de negocio",
                Detail = ex.Message,
                Status = StatusCodes.Status409Conflict
            });
        }
    }

    [Authorize]
    [HttpPost("invalidate-board-sessions")]
    public async Task<IActionResult> InvalidateBoardSessions([FromBody] InvalidateBoardSessionsRequestDto request, CancellationToken cancellationToken)
    {
        if (!TryGetAuthContext(out var actorUsuarioId, out var actorTenantId))
        {
            return Unauthorized();
        }

        try
        {
            var total = await _authSecurityService.InvalidateBoardSessionsAsync(actorUsuarioId, actorTenantId, request.BoardId, cancellationToken);
            return Ok(new { invalidatedUsers = total });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Conflicto de negocio",
                Detail = ex.Message,
                Status = StatusCodes.Status409Conflict
            });
        }
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

    private bool TryGetAuthContext(out Guid usuarioId, out Guid tenantId)
    {
        usuarioId = Guid.Empty;
        tenantId = Guid.Empty;

        var userIdRaw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var tenantIdRaw = User.FindFirstValue("tenant_id");

        return Guid.TryParse(userIdRaw, out usuarioId) && Guid.TryParse(tenantIdRaw, out tenantId);
    }
}
