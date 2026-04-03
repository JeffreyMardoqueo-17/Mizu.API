using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Muzu.Api.Core.DTOs;
using Muzu.Api.Core.Interfaces.Service;

namespace Muzu.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RolesController(
    IRoleManagementService roleManagementService) : ControllerBase
{
    // GET: api/roles
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RoleDto>>> ObtenerTodosLosRoles(CancellationToken cancellationToken)
    {
        var usuarioId = Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());
        var tenantId = Guid.Parse(User.FindFirst("tenant_id")?.Value ?? throw new UnauthorizedAccessException());

        var roles = await roleManagementService.ObtenerTodosLosRolesAsync(usuarioId, tenantId, cancellationToken);
        return Ok(roles);
    }

    // GET: api/roles/{id}/permissions
    [HttpGet("{id:guid}/permissions")]
    public async Task<ActionResult<RolePermissionResponseDto>> ObtenerPermisosDelRol(Guid id, CancellationToken cancellationToken)
    {
        var usuarioId = Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());
        var tenantId = Guid.Parse(User.FindFirst("tenant_id")?.Value ?? throw new UnauthorizedAccessException());

        var result = await roleManagementService.ObtenerPermisosDelRolAsync(usuarioId, tenantId, id, cancellationToken);
        if (result is null)
        {
            return NotFound();
        }

        return Ok(result);
    }

    // GET: api/roles/permissions/all
    [HttpGet("permissions/all")]
    public async Task<ActionResult<IReadOnlyList<PermisoDto>>> ObtenerTodosLosPermisos(CancellationToken cancellationToken)
    {
        var usuarioId = Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());
        var tenantId = Guid.Parse(User.FindFirst("tenant_id")?.Value ?? throw new UnauthorizedAccessException());

        var permisos = await roleManagementService.ObtenerTodosLosPermisosAsync(usuarioId, tenantId, cancellationToken);
        return Ok(permisos);
    }

    // PUT: api/roles/{id}/permissions
    [HttpPut("{id:guid}/permissions")]
    public async Task<IActionResult> ActualizarPermisosDelRol(Guid id, [FromBody] ActualizarPermisosDelRolRequest request, CancellationToken cancellationToken)
    {
        var usuarioId = Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());
        var tenantId = Guid.Parse(User.FindFirst("tenant_id")?.Value ?? throw new UnauthorizedAccessException());

        await roleManagementService.ActualizarPermisosDelRolAsync(usuarioId, tenantId, id, request.PermisoCodigos, cancellationToken);
        return NoContent();
    }
}

public record ActualizarPermisosDelRolRequest(List<string> PermisoCodigos);
