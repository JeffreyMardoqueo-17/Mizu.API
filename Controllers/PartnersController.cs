using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Muzu.Api.Core.DTOs;
using Muzu.Api.Core.Interfaces.Service;

namespace Muzu.Api.Controllers;

/// <summary>
/// Controlador para gestión de documentos de partners/asociados.
/// Maneja carga, visualización, actualización y eliminación de documentos de identidad.
/// </summary>
[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class PartnersController : ControllerBase
{
    private readonly IPartnerDocumentService _documentService;

    public PartnersController(IPartnerDocumentService documentService)
    {
        _documentService = documentService ?? throw new ArgumentNullException(nameof(documentService));
    }

    /// <summary>
    /// Obtiene todos los documentos de un socio específico por su ID.
    /// Solo el propietario, administrador o autorizado puede obtener sus documentos.
    /// GET /api/partners/{partnerUserId}/documents
    /// </summary>
    [HttpGet("{partnerUserId:guid}/documents")]
    public async Task<IActionResult> ObtenerDocumentosAsync(
        [FromRoute] Guid partnerUserId,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetAuthContext(out var actorUserId, out var actorTenantId))
        {
            return Unauthorized();
        }

        try
        {
            // Validación: solo el usuario mismo o admin puede ver sus documentos
            if (actorUserId != partnerUserId && !User.IsInRole("Administrador"))
            {
                return Forbid();
            }

            var result = await _documentService.ObtenerDocumentosUsuarioAsync(
                partnerUserId,
                actorTenantId,
                cancellationToken);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Usuario no encontrado",
                Detail = ex.Message,
                Status = StatusCodes.Status404NotFound
            });
        }
    }

    /// <summary>
    /// Obtiene un documento específico por su ID.
    /// GET /api/partners/documents/{documentId}
    /// </summary>
    [HttpGet("documents/{documentId:guid}")]
    public async Task<IActionResult> ObtenerDocumentoPorIdAsync(
        [FromRoute] Guid documentId,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetAuthContext(out _, out var tenantId))
        {
            return Unauthorized();
        }

        try
        {
            var documento = await _documentService.ObtenerDocumentoPorIdAsync(documentId, tenantId, cancellationToken);

            if (documento == null)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Documento no encontrado",
                    Detail = $"El documento {documentId} no existe.",
                    Status = StatusCodes.Status404NotFound
                });
            }

            return Ok(documento);
        }
        catch (Exception ex)
        {
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new ProblemDetails
                {
                    Title = "Error interno",
                    Detail = ex.Message,
                    Status = StatusCodes.Status500InternalServerError
                });
        }
    }

    /// <summary>
    /// Carga un nuevo documento para un socio.
    /// Máximo 2 documentos por usuario.
    /// POST /api/partners/{partnerUserId}/documents
    /// </summary>
    [HttpPost("{partnerUserId:guid}/documents")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> CargarDocumentoAsync(
        [FromRoute] Guid partnerUserId,
        [FromForm] IFormFile archivo,
        [FromForm] string tipoDocumento,
        [FromForm] int orden,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetAuthContext(out var actorUserId, out var tenantId))
        {
            return Unauthorized();
        }

        // Validación: solo el usuario mismo o admin puede cargar documentos
        if (actorUserId != partnerUserId && !User.IsInRole("Administrador"))
        {
            return Forbid();
        }

        try
        {
            var resultado = await _documentService.CargarDocumentoAsync(
                partnerUserId,
                tenantId,
                archivo,
                tipoDocumento,
                orden,
                cancellationToken);

            return Created($"/api/partners/documents/{resultado.Document.Id}", resultado);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Error de validación",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
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

    /// <summary>
    /// Reemplaza la imagen de un documento existente manteniendo el ID.
    /// PUT /api/partners/documents/{documentId}/replace
    /// </summary>
    [HttpPut("documents/{documentId:guid}/replace")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> ReemplazarDocumentoAsync(
        [FromRoute] Guid documentId,
        [FromForm] IFormFile archivoNuevo,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetAuthContext(out _, out var tenantId))
        {
            return Unauthorized();
        }

        try
        {
            var resultado = await _documentService.ReemplazarDocumentoAsync(
                documentId,
                tenantId,
                archivoNuevo,
                cancellationToken);

            return Ok(resultado);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Error de validación",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Documento no encontrado",
                Detail = ex.Message,
                Status = StatusCodes.Status404NotFound
            });
        }
    }

    /// <summary>
    /// Actualiza información del documento (tipo, orden) pero no la imagen.
    /// PATCH /api/partners/documents/{documentId}
    /// </summary>
    [HttpPatch("documents/{documentId:guid}")]
    public async Task<IActionResult> ActualizarDocumentoAsync(
        [FromRoute] Guid documentId,
        [FromBody] UpdatePartnerDocumentRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetAuthContext(out _, out var tenantId))
        {
            return Unauthorized();
        }

        try
        {
            var documento = await _documentService.ActualizarDocumentoAsync(
                documentId,
                tenantId,
                dto,
                cancellationToken);

            return Ok(documento);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Error de validación",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Documento no encontrado",
                Detail = ex.Message,
                Status = StatusCodes.Status404NotFound
            });
        }
    }

    /// <summary>
    /// Elimina un documento específico.
    /// DELETE /api/partners/documents/{documentId}
    /// </summary>
    [HttpDelete("documents/{documentId:guid}")]
    public async Task<IActionResult> EliminarDocumentoAsync(
        [FromRoute] Guid documentId,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetAuthContext(out _, out var tenantId))
        {
            return Unauthorized();
        }

        try
        {
            var resultado = await _documentService.EliminarDocumentoAsync(
                documentId,
                tenantId,
                cancellationToken);

            return Ok(resultado);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Documento no encontrado",
                Detail = ex.Message,
                Status = StatusCodes.Status404NotFound
            });
        }
    }

    private bool TryGetAuthContext(out Guid usuarioId, out Guid tenantId)
    {
        usuarioId = Guid.Empty;
        tenantId = Guid.Empty;

        var userIdRaw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var tenantIdRaw = User.FindFirstValue("tenant_id");

        if (!Guid.TryParse(userIdRaw, out usuarioId) || !Guid.TryParse(tenantIdRaw, out tenantId))
        {
            return false;
        }

        return true;
    }
}
