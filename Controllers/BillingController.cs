using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Muzu.Api.Core.DTOs;
using Muzu.Api.Core.Interfaces.Service;
using Muzu.Api.Core.Rules;

namespace Muzu.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class BillingController : ControllerBase
{
    private readonly IBillingService _billingService;

    public BillingController(IBillingService billingService)
    {
        _billingService = billingService;
    }

    [HttpPost("cycles/generate")]
    public async Task<IActionResult> GenerarCiclo([FromBody] CreateBillingCycleRequestDto request, CancellationToken cancellationToken = default)
    {
        if (!TryGetAuthContext(out var actorUsuarioId, out var actorTenantId))
        {
            return Unauthorized();
        }

        if (!TieneAccesoFinanciero())
        {
            return Forbid();
        }

        try
        {
            var result = await _billingService.GenerarCicloAsync(actorUsuarioId, actorTenantId, request, cancellationToken);
            return Ok(result);
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
        catch (ArgumentException exception)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Error de validación",
                Detail = exception.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }

    [HttpPost("readings")]
    public async Task<IActionResult> RegistrarLectura([FromBody] MeterReadingCreateRequestDto request, CancellationToken cancellationToken = default)
    {
        if (!TryGetAuthContext(out var actorUsuarioId, out var actorTenantId))
        {
            return Unauthorized();
        }

        if (!TieneAccesoFinanciero())
        {
            return Forbid();
        }

        try
        {
            var result = await _billingService.RegistrarLecturaAsync(actorUsuarioId, actorTenantId, request, cancellationToken);
            return Ok(result);
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
        catch (ArgumentException exception)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Error de validación",
                Detail = exception.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }

    [HttpGet("invoices/{invoiceId:guid}")]
    public async Task<IActionResult> ObtenerFactura([FromRoute] Guid invoiceId, CancellationToken cancellationToken = default)
    {
        if (!TryGetAuthContext(out var actorUsuarioId, out var actorTenantId))
        {
            return Unauthorized();
        }

        if (!TieneAccesoFinanciero())
        {
            return Forbid();
        }

        try
        {
            var result = await _billingService.ObtenerFacturaAsync(actorUsuarioId, actorTenantId, invoiceId, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException exception)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Factura no encontrada",
                Detail = exception.Message,
                Status = StatusCodes.Status404NotFound
            });
        }
    }

    [HttpGet("meters/{meterId:guid}/history")]
    public async Task<IActionResult> ObtenerHistorialMedidor([FromRoute] Guid meterId, CancellationToken cancellationToken = default)
    {
        if (!TryGetAuthContext(out var actorUsuarioId, out var actorTenantId))
        {
            return Unauthorized();
        }

        if (!TieneAccesoFinanciero())
        {
            return Forbid();
        }

        try
        {
            var result = await _billingService.ObtenerHistorialMedidorAsync(actorUsuarioId, actorTenantId, meterId, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException exception)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Medidor no encontrado",
                Detail = exception.Message,
                Status = StatusCodes.Status404NotFound
            });
        }
    }

    [HttpGet("users/{usuarioId:guid}/debt")]
    public async Task<IActionResult> ObtenerResumenDeudaUsuario([FromRoute] Guid usuarioId, CancellationToken cancellationToken = default)
    {
        if (!TryGetAuthContext(out var actorUsuarioId, out var actorTenantId))
        {
            return Unauthorized();
        }

        if (!TieneAccesoFinanciero() && actorUsuarioId != usuarioId)
        {
            return Forbid();
        }

        var result = await _billingService.ObtenerResumenDeudaUsuarioAsync(actorUsuarioId, actorTenantId, usuarioId, cancellationToken);
        return Ok(result);
    }

    [HttpPost("invoices/{invoiceId:guid}/payments")]
    public async Task<IActionResult> RegistrarPago([FromRoute] Guid invoiceId, [FromBody] InvoicePayRequestDto request, CancellationToken cancellationToken = default)
    {
        if (!TryGetAuthContext(out var actorUsuarioId, out var actorTenantId))
        {
            return Unauthorized();
        }

        if (!TieneAccesoFinanciero())
        {
            return Forbid();
        }

        try
        {
            var result = await _billingService.RegistrarPagoAsync(actorUsuarioId, actorTenantId, invoiceId, request, cancellationToken);
            return Ok(result);
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
        catch (ArgumentException exception)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Error de validación",
                Detail = exception.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }

    [HttpPost("invoices/{invoiceId:guid}/reliquidations")]
    public async Task<IActionResult> ReliquidarFactura([FromRoute] Guid invoiceId, [FromBody] InvoiceReliquidateRequestDto request, CancellationToken cancellationToken = default)
    {
        if (!TryGetAuthContext(out var actorUsuarioId, out var actorTenantId))
        {
            return Unauthorized();
        }

        if (!TieneAccesoFinanciero())
        {
            return Forbid();
        }

        try
        {
            var result = await _billingService.ReliquidarFacturaAsync(actorUsuarioId, actorTenantId, invoiceId, request, cancellationToken);
            return Ok(result);
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
        catch (ArgumentException exception)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Error de validación",
                Detail = exception.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }

    [HttpPost("penalties")]
    public async Task<IActionResult> RegistrarMultaOperativa([FromBody] AssignOperationalPenaltyRequestDto request, CancellationToken cancellationToken = default)
    {
        if (!TryGetAuthContext(out var actorUsuarioId, out var actorTenantId))
        {
            return Unauthorized();
        }

        if (!TieneAccesoFinanciero())
        {
            return Forbid();
        }

        try
        {
            var result = await _billingService.RegistrarMultaOperativaAsync(actorUsuarioId, actorTenantId, request, cancellationToken);
            return Ok(result);
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
        catch (ArgumentException exception)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Error de validación",
                Detail = exception.Message,
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

    private bool TieneAccesoFinanciero()
    {
        return User.IsInRole(SystemRoles.Administrador)
            || User.IsInRole(SystemRoles.Presidente)
            || User.IsInRole(SystemRoles.Tesorero)
            || User.IsInRole(SystemRoles.Contador);
    }
}
