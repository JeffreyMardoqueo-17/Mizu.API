using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Muzu.Api.Core.DTOs;
using Muzu.Api.Core.Interfaces.Service;

namespace Muzu.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class UsuariosController : ControllerBase
{
    private readonly IUsuarioAdministracionService _usuarioAdministracionService;

    public UsuariosController(IUsuarioAdministracionService usuarioAdministracionService)
    {
        _usuarioAdministracionService = usuarioAdministracionService;
    }

    [HttpGet]
    public async Task<IActionResult> ListarUsuarios(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetAuthContext(out var actorUsuarioId, out var actorTenantId))
        {
            return Unauthorized();
        }

        try
        {
            var result = await _usuarioAdministracionService.ListarUsuariosAsync(
                actorUsuarioId,
                actorTenantId,
                search,
                page,
                pageSize,
                cancellationToken);

            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpPost]
    public async Task<IActionResult> CrearUsuario([FromBody] CrearUsuarioDto request, CancellationToken cancellationToken)
    {
        if (!TryGetAuthContext(out var actorUsuarioId, out var actorTenantId))
        {
            return Unauthorized();
        }

        try
        {
            var result = await _usuarioAdministracionService.CrearUsuarioAsync(request, actorUsuarioId, actorTenantId, cancellationToken);
            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
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

    [HttpPatch("{usuarioId:guid}/rol")]
    public async Task<IActionResult> ActualizarRol([FromRoute] Guid usuarioId, [FromBody] ActualizarRolUsuarioDto request, CancellationToken cancellationToken)
    {
        return Conflict(new ProblemDetails
        {
            Title = "Operacion no permitida",
            Detail = "El rol ya no se edita desde modulo usuarios. Usa el flujo de directivas en /api/boards/{id}/members.",
            Status = StatusCodes.Status409Conflict
        });
    }

    private bool TryGetAuthContext(out Guid usuarioId, out Guid tenantId)
    {
        usuarioId = Guid.Empty;
        tenantId = Guid.Empty;

        var userIdRaw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var tenantIdRaw = User.FindFirstValue("tenant_id");

        if (!Guid.TryParse(userIdRaw, out usuarioId))
        {
            return false;
        }

        if (!Guid.TryParse(tenantIdRaw, out tenantId))
        {
            return false;
        }

        return true;
    }
}
