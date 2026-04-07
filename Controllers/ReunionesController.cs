using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Muzu.Api.Core.DTOs;
using Muzu.Api.Core.Interfaces.Service;

namespace Muzu.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class ReunionesController : ControllerBase
{
    private readonly IReunionService _reunionService;

    public ReunionesController(IReunionService reunionService)
    {
        _reunionService = reunionService;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken cancellationToken)
    {
        if (!TryGetAuthContext(out var actorUsuarioId, out var actorTenantId))
        {
            return Unauthorized();
        }

        try
        {
            var result = await _reunionService.ListarAsync(actorUsuarioId, actorTenantId, cancellationToken);
            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> ObtenerPorId([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetAuthContext(out var actorUsuarioId, out var actorTenantId))
        {
            return Unauthorized();
        }

        try
        {
            var result = await _reunionService.ObtenerPorIdAsync(actorUsuarioId, actorTenantId, id, cancellationToken);
            return result is null ? NotFound() : Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CreateReunionRequestDto request, CancellationToken cancellationToken)
    {
        if (!TryGetAuthContext(out var actorUsuarioId, out var actorTenantId))
        {
            return Unauthorized();
        }

        try
        {
            var result = await _reunionService.CrearAsync(actorUsuarioId, actorTenantId, request, cancellationToken);
            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Error de negocio",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Actualizar([FromRoute] Guid id, [FromBody] UpdateReunionRequestDto request, CancellationToken cancellationToken)
    {
        if (!TryGetAuthContext(out var actorUsuarioId, out var actorTenantId))
        {
            return Unauthorized();
        }

        try
        {
            var result = await _reunionService.ActualizarAsync(actorUsuarioId, actorTenantId, id, request, cancellationToken);
            return result is null ? NotFound() : Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Error de negocio",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }

    [HttpPost("{id:guid}/start")]
    public async Task<IActionResult> Iniciar([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetAuthContext(out var actorUsuarioId, out var actorTenantId))
        {
            return Unauthorized();
        }

        try
        {
            var result = await _reunionService.IniciarAsync(actorUsuarioId, actorTenantId, id, cancellationToken);
            return result is null ? NotFound() : Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Error de negocio",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }

    [HttpPut("{id:guid}/attendance")]
    public async Task<IActionResult> ActualizarAsistencia([FromRoute] Guid id, [FromBody] UpdateReunionAsistenciaRequestDto request, CancellationToken cancellationToken)
    {
        if (!TryGetAuthContext(out var actorUsuarioId, out var actorTenantId))
        {
            return Unauthorized();
        }

        try
        {
            var result = await _reunionService.ActualizarAsistenciaAsync(actorUsuarioId, actorTenantId, id, request, cancellationToken);
            return result is null ? NotFound() : Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Error de negocio",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }

    [HttpPut("{id:guid}/acuerdos")]
    public async Task<IActionResult> ActualizarAcuerdos([FromRoute] Guid id, [FromBody] UpdateReunionAcuerdosRequestDto request, CancellationToken cancellationToken)
    {
        if (!TryGetAuthContext(out var actorUsuarioId, out var actorTenantId))
        {
            return Unauthorized();
        }

        try
        {
            var result = await _reunionService.ActualizarAcuerdosAsync(actorUsuarioId, actorTenantId, id, request, cancellationToken);
            return result is null ? NotFound() : Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Error de negocio",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }

    [HttpPost("{id:guid}/finalize")]
    public async Task<IActionResult> Finalizar([FromRoute] Guid id, [FromBody] FinalizeReunionRequestDto request, CancellationToken cancellationToken)
    {
        if (!TryGetAuthContext(out var actorUsuarioId, out var actorTenantId))
        {
            return Unauthorized();
        }

        try
        {
            var result = await _reunionService.FinalizarAsync(actorUsuarioId, actorTenantId, id, request, cancellationToken);
            return result is null ? NotFound() : Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Error de negocio",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
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
}