using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Muzu.Api.Core.DTOs;
using Muzu.Api.Core.Interfaces.Service;
using Npgsql;

namespace Muzu.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class UsersController : ControllerBase
{
    private readonly IUsersService _usersService;

    public UsersController(IUsersService usersService)
    {
        _usersService = usersService;
    }

    [HttpGet]
    public async Task<IActionResult> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? dui = null,
        [FromQuery] string? nombre = null,
        [FromQuery] string? correo = null,
        [FromQuery] bool? estado = null,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetAuthContext(out var actorUsuarioId, out var actorTenantId))
        {
            return Unauthorized();
        }

        try
        {
            var response = await _usersService.GetUsersAsync(actorUsuarioId, actorTenantId, page, pageSize, dui, nombre, correo, estado, cancellationToken);
            return Ok(response);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequestDto request, CancellationToken cancellationToken)
    {
        if (!TryGetAuthContext(out var actorUsuarioId, out var actorTenantId))
        {
            return Unauthorized();
        }

        try
        {
            var created = await _usersService.CreateUserAsync(actorUsuarioId, actorTenantId, request, cancellationToken);
            return Ok(created);
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
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Conflicto de negocio",
                Detail = GetUniqueConstraintMessage(ex.ConstraintName),
                Status = StatusCodes.Status409Conflict
            });
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetAuthContext(out var actorUsuarioId, out var actorTenantId))
        {
            return Unauthorized();
        }

        try
        {
            var user = await _usersService.GetUserByIdAsync(actorUsuarioId, actorTenantId, id, cancellationToken);
            return user is null ? NotFound() : Ok(user);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateUserRequestDto request, CancellationToken cancellationToken)
    {
        if (!TryGetAuthContext(out var actorUsuarioId, out var actorTenantId))
        {
            return Unauthorized();
        }

        try
        {
            var user = await _usersService.UpdateUserAsync(actorUsuarioId, actorTenantId, id, request, cancellationToken);
            return user is null ? NotFound() : Ok(user);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Conflicto de negocio",
                Detail = GetUniqueConstraintMessage(ex.ConstraintName),
                Status = StatusCodes.Status409Conflict
            });
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> SoftDelete([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetAuthContext(out var actorUsuarioId, out var actorTenantId))
        {
            return Unauthorized();
        }

        try
        {
            var deleted = await _usersService.SoftDeleteUserAsync(actorUsuarioId, actorTenantId, id, cancellationToken);
            return deleted ? NoContent() : NotFound();
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

    private bool TryGetAuthContext(out Guid usuarioId, out Guid tenantId)
    {
        usuarioId = Guid.Empty;
        tenantId = Guid.Empty;

        var userIdRaw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var tenantIdRaw = User.FindFirstValue("tenant_id");

        return Guid.TryParse(userIdRaw, out usuarioId) && Guid.TryParse(tenantIdRaw, out tenantId);
    }

    private static string GetUniqueConstraintMessage(string? constraintName)
    {
        return constraintName switch
        {
            "usuarios_correo_key" => "Ya existe un usuario con ese correo.",
            "usuarios_dui_key" => "Ya existe un usuario con ese DUI.",
            _ => "Ya existe un registro con esos datos."
        };
    }
}
