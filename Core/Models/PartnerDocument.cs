namespace Muzu.Api.Core.Models;

/// <summary>
/// Modelo que representa un documento (DUI, cédula, etc.) de un socio/usuario.
/// Máximo 2 documentos por usuario únicamente para fines de verificación de identidad.
/// </summary>
public class PartnerDocument
{
    /// <summary>
    /// ID único del documento
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// ID del usuario/socio propietario del documento
    /// </summary>
    public Guid UsuarioId { get; set; }

    /// <summary>
    /// ID del tenant para multi-tenancy
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// URL del documento en Cloudinary
    /// </summary>
    public string DocumentUrl { get; set; } = string.Empty;

    /// <summary>
    /// ID público de Cloudinary para facilitar eliminación
    /// </summary>
    public string CloudinaryPublicId { get; set; } = string.Empty;

    /// <summary>
    /// Tipo de documento (DUI, Pasaporte, Cédula, etc.)
    /// </summary>
    public string DocumentType { get; set; } = "DUI";

    /// <summary>
    /// Nombre original del archivo subido
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Tamaño en bytes del documento
    /// </summary>
    public long FileSizeBytes { get; set; }

    /// <summary>
    /// Orden de visualización (1 o 2)
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Marca si el documento está activo/válido
    /// </summary>
    public bool Activo { get; set; } = true;

    /// <summary>
    /// Fecha de creación del registro
    /// </summary>
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Última fecha de actualización
    /// </summary>
    public DateTime? FechaActualizacion { get; set; }

    /// <summary>
    /// Fecha de eliminación lógica
    /// </summary>
    public DateTime? FechaEliminacion { get; set; }

    /// <summary>
    /// Flag de eliminación lógica
    /// </summary>
    public bool Eliminado { get; set; }
}
