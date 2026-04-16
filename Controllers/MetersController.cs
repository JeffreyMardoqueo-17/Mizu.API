using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Muzu.Api.Core.DTOs;
using Muzu.Api.Core.Interfaces.Service;

namespace Muzu.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class MetersController : ControllerBase
{
    private readonly IMedidorService _medidorService;

    public MetersController(IMedidorService medidorService)
    {
        _medidorService = medidorService;
    }

    [HttpGet]
    public async Task<IActionResult> ListByUsuario(
        [FromQuery] Guid usuarioId,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetAuthContext(out var actorUsuarioId, out var actorTenantId))
        {
            return Unauthorized();
        }

        try
        {
            var result = await _medidorService.ObtenerPorUsuarioAsync(actorUsuarioId, actorTenantId, usuarioId, cancellationToken);
            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new ProblemDetails
            {
                Title = "No encontrado",
                Detail = ex.Message,
                Status = StatusCodes.Status404NotFound
            });
        }
    }

    [HttpGet("next-number")]
    public async Task<IActionResult> GetNextNumber(
        [FromQuery] Guid usuarioId,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetAuthContext(out var actorUsuarioId, out var actorTenantId))
        {
            return Unauthorized();
        }

        try
        {
            var result = await _medidorService.ObtenerSiguienteNumeroAsync(actorUsuarioId, actorTenantId, usuarioId, cancellationToken);
            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new ProblemDetails
            {
                Title = "No encontrado",
                Detail = ex.Message,
                Status = StatusCodes.Status404NotFound
            });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Assign(
        [FromBody] MeterAssignRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetAuthContext(out var actorUsuarioId, out var actorTenantId))
        {
            return Unauthorized();
        }

        try
        {
            var result = await _medidorService.AsignarAsync(actorUsuarioId, actorTenantId, request, cancellationToken);
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

    [HttpPatch("{medidorId:guid}/status")]
    public async Task<IActionResult> UpdateStatus(
        [FromRoute] Guid medidorId,
        [FromBody] MeterStatusUpdateRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetAuthContext(out var actorUsuarioId, out var actorTenantId))
        {
            return Unauthorized();
        }

        try
        {
            var result = await _medidorService.ActualizarEstadoAsync(actorUsuarioId, actorTenantId, medidorId, request, cancellationToken);
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

    [HttpPost("{medidorId:guid}/transfer")]
    public async Task<IActionResult> Transfer(
        [FromRoute] Guid medidorId,
        [FromBody] MeterTransferRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetAuthContext(out var actorUsuarioId, out var actorTenantId))
        {
            return Unauthorized();
        }

        try
        {
            var result = await _medidorService.TransferirAsync(actorUsuarioId, actorTenantId, medidorId, request, cancellationToken);
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

    [HttpGet("{medidorId:guid}/transfers")]
    public async Task<IActionResult> GetTransfers(
        [FromRoute] Guid medidorId,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetAuthContext(out var actorUsuarioId, out var actorTenantId))
        {
            return Unauthorized();
        }

        try
        {
            var result = await _medidorService.ObtenerHistorialTransferenciasAsync(actorUsuarioId, actorTenantId, medidorId, cancellationToken);
            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new ProblemDetails
            {
                Title = "No encontrado",
                Detail = ex.Message,
                Status = StatusCodes.Status404NotFound
            });
        }
    }

    [HttpGet("active-conflicts")]
    public async Task<IActionResult> GetActiveConflicts(
        CancellationToken cancellationToken = default)
    {
        if (!TryGetAuthContext(out var actorUsuarioId, out var actorTenantId))
        {
            return Unauthorized();
        }

        try
        {
            var result = await _medidorService.ObtenerReporteConflictosActivosAsync(actorUsuarioId, actorTenantId, cancellationToken);
            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
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
