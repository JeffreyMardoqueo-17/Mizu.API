using System.ComponentModel.DataAnnotations;

namespace Muzu.Api.Core.DTOs;

/// <summary>
/// DTO para crear un documento de partner
/// </summary>
public sealed record CreatePartnerDocumentRequestDto(
    [param: Required] Guid UsuarioId,
    [param: Required, StringLength(50)] string DocumentType,
    [param: Required, StringLength(500)] string FileName
);

/// <summary>
/// DTO para actualizar información de un documento (no la foto)
/// </summary>
public sealed record UpdatePartnerDocumentRequestDto(
    [param: Required, StringLength(50)] string DocumentType,
    [param: Required] int DisplayOrder
);

/// <summary>
/// DTO de respuesta para un documento individual
/// </summary>
public sealed record PartnerDocumentDto(
    Guid Id,
    Guid UsuarioId,
    string DocumentUrl,
    string DocumentType,
    string FileName,
    long FileSizeBytes,
    int DisplayOrder,
    bool Activo,
    DateTime FechaCreacion,
    DateTime? FechaActualizacion
);

/// <summary>
/// DTO para listar documentos de un usuario
/// </summary>
public sealed record PartnerDocumentsListDto(
    Guid UsuarioId,
    IReadOnlyList<PartnerDocumentDto> Documents,
    int TotalCount,
    int ActiveCount
);

/// <summary>
/// DTO para respuesta de carga exitosa
/// </summary>
public sealed record PartnerDocumentUploadResponseDto(
    PartnerDocumentDto Document,
    string Message
);

/// <summary>
/// DTO para respuesta de eliminación
/// </summary>
public sealed record PartnerDocumentDeleteResponseDto(
    Guid DocumentId,
    string Message
);
