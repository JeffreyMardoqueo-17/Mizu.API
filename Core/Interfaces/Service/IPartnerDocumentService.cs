using Muzu.Api.Core.DTOs;

namespace Muzu.Api.Core.Interfaces.Service;

/// <summary>
/// Servicio de dominio para gestión de documentos de partners.
/// Implementa lógica de negocio: validaciones, límites, orquestación de operaciones.
/// </summary>
public interface IPartnerDocumentService
{
    /// <summary>
    /// Obtiene todos los documentos activos de un usuario con paginación.
    /// Solo retorna documentos que no han sido eliminados lógicamente.
    /// </summary>
    Task<PartnerDocumentsListDto> ObtenerDocumentosUsuarioAsync(
        Guid usuarioId,
        Guid tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene un documento específico por ID después de validar propiedad.
    /// </summary>
    Task<PartnerDocumentDto?> ObtenerDocumentoPorIdAsync(
        Guid documentoId,
        Guid tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cra un nuevo documento después de:
    /// - Validar que el usuario existe
    /// - Validar que no existe otro documento en el mismo orden
    /// - Validar que no excede el máximo de 2 documentos
    /// - Cargar archivo a Cloudinary
    /// </summary>
    Task<PartnerDocumentUploadResponseDto> CargarDocumentoAsync(
        Guid usuarioId,
        Guid tenantId,
        IFormFile archivo,
        string tipoDocumento,
        int orden,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Actualiza la información de un documento existente sin cambiar la imagen.
    /// Valida propiedad del documento antes de actualizar.
    /// </summary>
    Task<PartnerDocumentDto> ActualizarDocumentoAsync(
        Guid documentoId,
        Guid tenantId,
        UpdatePartnerDocumentRequestDto dto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Realiza eliminación lógica de un documento y lo elimina de Cloudinary.
    /// Valida propiedad del documento antes de eliminar.
    /// </summary>
    Task<PartnerDocumentDeleteResponseDto> EliminarDocumentoAsync(
        Guid documentoId,
        Guid tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reemplaza la imagen de un documento existente.
    /// Elimina la imagen anterior de Cloudinary.
    /// </summary>
    Task<PartnerDocumentUploadResponseDto> ReemplazarDocumentoAsync(
        Guid documentoId,
        Guid tenantId,
        IFormFile archivoNuevo,
        CancellationToken cancellationToken = default);
}
