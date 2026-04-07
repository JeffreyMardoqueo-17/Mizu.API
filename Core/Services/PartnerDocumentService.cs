using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Muzu.Api.Core.DTOs;
using Muzu.Api.Core.Interfaces;
using Muzu.Api.Core.Interfaces.Service;
using Muzu.Api.Core.Models;

namespace Muzu.Api.Core.Services;

/// <summary>
/// Servicio de dominio para gestión de documentos de partners.
/// Coordina la lógica de negocio con repositorios e integraciones externas (Cloudinary).
/// </summary>
public sealed class PartnerDocumentService : IPartnerDocumentService
{
    private readonly Cloudinary _cloudinary;
    private readonly IPartnerDocumentRepository _documentRepository;
    private readonly IUsuarioRepository _usuarioRepository;
    private const int MaxDocumentosPermitidos = 2;
    private const int MaxFileSizeBytes = 10 * 1024 * 1024; // 10MB
    private static readonly string[] TiposDocumentoPermitidos = { "DUI", "Pasaporte", "Cédula", "Licencia" };
    private static readonly string[] ExtensionesPermitidas = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

    public PartnerDocumentService(
        Cloudinary cloudinary,
        IPartnerDocumentRepository documentRepository,
        IUsuarioRepository usuarioRepository)
    {
        _cloudinary = cloudinary ?? throw new ArgumentNullException(nameof(cloudinary));
        _documentRepository = documentRepository ?? throw new ArgumentNullException(nameof(documentRepository));
        _usuarioRepository = usuarioRepository ?? throw new ArgumentNullException(nameof(usuarioRepository));
    }

    public async Task<PartnerDocumentsListDto> ObtenerDocumentosUsuarioAsync(
        Guid usuarioId,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        // Validar que el usuario existe
        var usuario = await _usuarioRepository.ObtenerPorIdAsync(usuarioId, cancellationToken: cancellationToken);
        if (usuario == null)
        {
            throw new InvalidOperationException($"El usuario {usuarioId} no existe.");
        }

        // Obtener documentos del usuario
        var documentos = await _documentRepository.ObtenerPorUsuarioIdAsync(
            usuarioId,
            cancellationToken: cancellationToken);

        var documentosDtos = documentos
            .Select(MapearADocumentoDto)
            .ToList()
            .AsReadOnly();

        return new PartnerDocumentsListDto(
            UsuarioId: usuarioId,
            Documents: documentosDtos,
            TotalCount: documentosDtos.Count,
            ActiveCount: documentosDtos.Count(d => d.Activo));
    }

    public async Task<PartnerDocumentDto?> ObtenerDocumentoPorIdAsync(
        Guid documentoId,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var documento = await _documentRepository.ObtenerPorIdAsync(documentoId, cancellationToken: cancellationToken);
        
        if (documento == null || documento.TenantId != tenantId)
        {
            return null;
        }

        return MapearADocumentoDto(documento);
    }

    public async Task<PartnerDocumentUploadResponseDto> CargarDocumentoAsync(
        Guid usuarioId,
        Guid tenantId,
        IFormFile archivo,
        string tipoDocumento,
        int orden,
        CancellationToken cancellationToken = default)
    {
        // Validaciones previas
        ValidarArchivo(archivo);
        ValidarTipoDocumento(tipoDocumento);
        ValidarOrdenDocumento(orden);

        // Validar que usuario existe
        var usuario = await _usuarioRepository.ObtenerPorIdAsync(usuarioId, cancellationToken: cancellationToken);
        if (usuario == null)
        {
            throw new InvalidOperationException($"El usuario {usuarioId} no existe.");
        }

        // Validar límite de documentos
        var countDocumentos = await _documentRepository.ContarDocumentosActivosAsync(
            usuarioId,
            cancellationToken: cancellationToken);

        if (countDocumentos >= MaxDocumentosPermitidos)
        {
            throw new InvalidOperationException(
                $"El usuario ya tiene el máximo de {MaxDocumentosPermitidos} documentos permitidos.");
        }

        // Validar que no existe documento en el mismo orden
        var documentosExistentes = await _documentRepository.ObtenerPorUsuarioIdAsync(
            usuarioId,
            cancellationToken: cancellationToken);

        if (documentosExistentes.Any(d => d.DisplayOrder == orden && d.Activo))
        {
            throw new InvalidOperationException(
                $"Ya existe un documento en la posición {orden}. Reemplázalo si deseas cambiar.");
        }

        // Cargar a Cloudinary
        var uploadResult = await CargarACloudinaryAsync(archivo, usuarioId, tipoDocumento);

        // Crear documento
        var nuevoDocumento = new PartnerDocument
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuarioId,
            TenantId = tenantId,
            DocumentUrl = uploadResult.SecureUrl.ToString(),
            CloudinaryPublicId = uploadResult.PublicId,
            DocumentType = tipoDocumento,
            FileName = archivo.FileName,
            FileSizeBytes = archivo.Length,
            DisplayOrder = orden,
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };

        // Guardar en base de datos
        var documentoGuardado = await _documentRepository.CrearAsync(
            nuevoDocumento,
            cancellationToken: cancellationToken);

        return new PartnerDocumentUploadResponseDto(
            MapearADocumentoDto(documentoGuardado),
            "Documento cargado exitosamente.");
    }

    public async Task<PartnerDocumentDto> ActualizarDocumentoAsync(
        Guid documentoId,
        Guid tenantId,
        UpdatePartnerDocumentRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        ValidarTipoDocumento(dto.DocumentType);
        ValidarOrdenDocumento(dto.DisplayOrder);

        // Obtener documento
        var documento = await _documentRepository.ObtenerPorIdAsync(documentoId, cancellationToken: cancellationToken);
        if (documento == null || documento.TenantId != tenantId)
        {
            throw new InvalidOperationException($"Documento {documentoId} no encontrado.");
        }

        // Validar que no existe otro documento en el mismo orden
        var documentosExistentes = await _documentRepository.ObtenerPorUsuarioIdAsync(
            documento.UsuarioId,
            cancellationToken: cancellationToken);

        if (documentosExistentes.Any(d => 
            d.DisplayOrder == dto.DisplayOrder && 
            d.Id != documentoId && 
            d.Activo))
        {
            throw new InvalidOperationException(
                $"Ya existe un documento en la posición {dto.DisplayOrder}.");
        }

        // Actualizar
        documento.DocumentType = dto.DocumentType;
        documento.DisplayOrder = dto.DisplayOrder;
        documento.FechaActualizacion = DateTime.UtcNow;

        var actualizado = await _documentRepository.ActualizarAsync(documento, cancellationToken: cancellationToken);
        if (!actualizado)
        {
            throw new InvalidOperationException("Error al actualizar el documento.");
        }

        var documentoActualizado = await _documentRepository.ObtenerPorIdAsync(
            documentoId,
            cancellationToken: cancellationToken);

        return MapearADocumentoDto(documentoActualizado!);
    }

    public async Task<PartnerDocumentDeleteResponseDto> EliminarDocumentoAsync(
        Guid documentoId,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        // Obtener documento
        var documento = await _documentRepository.ObtenerPorIdAsync(documentoId, cancellationToken: cancellationToken);
        if (documento == null || documento.TenantId != tenantId)
        {
            throw new InvalidOperationException($"Documento {documentoId} no encontrado.");
        }

        // Eliminar de Cloudinary
        try
        {
            var deleteParams = new DeletionParams(documento.CloudinaryPublicId);
            await _cloudinary.DestroyAsync(deleteParams);
        }
        catch (Exception ex)
        {
            // Log pero no fallar si Cloudinary no puede eliminar
            Console.WriteLine($"Error eliminando documento de Cloudinary: {ex.Message}");
        }

        // Eliminar de base de datos (lógica)
        var eliminado = await _documentRepository.EliminarAsync(documentoId, cancellationToken: cancellationToken);
        if (!eliminado)
        {
            throw new InvalidOperationException("Error al eliminar el documento.");
        }

        return new PartnerDocumentDeleteResponseDto(
            documentoId,
            "Documento eliminado exitosamente.");
    }

    public async Task<PartnerDocumentUploadResponseDto> ReemplazarDocumentoAsync(
        Guid documentoId,
        Guid tenantId,
        IFormFile archivoNuevo,
        CancellationToken cancellationToken = default)
    {
        ValidarArchivo(archivoNuevo);

        // Obtener documento existente
        var documento = await _documentRepository.ObtenerPorIdAsync(documentoId, cancellationToken: cancellationToken);
        if (documento == null || documento.TenantId != tenantId)
        {
            throw new InvalidOperationException($"Documento {documentoId} no encontrado.");
        }

        // Eliminar imagen anterior de Cloudinary
        try
        {
            var deleteParams = new DeletionParams(documento.CloudinaryPublicId);
            await _cloudinary.DestroyAsync(deleteParams);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error eliminando documento anterior de Cloudinary: {ex.Message}");
        }

        // Cargar nueva imagen
        var uploadResult = await CargarACloudinaryAsync(archivoNuevo, documento.UsuarioId, documento.DocumentType);

        // Actualizar documento
        documento.DocumentUrl = uploadResult.SecureUrl.ToString();
        documento.CloudinaryPublicId = uploadResult.PublicId;
        documento.FileName = archivoNuevo.FileName;
        documento.FileSizeBytes = archivoNuevo.Length;
        documento.FechaActualizacion = DateTime.UtcNow;

        var actualizado = await _documentRepository.ActualizarAsync(documento, cancellationToken: cancellationToken);
        if (!actualizado)
        {
            throw new InvalidOperationException("Error al persistir el documento reemplazado.");
        }

        var documentoActualizado = await _documentRepository.ObtenerPorIdAsync(
            documentoId,
            cancellationToken: cancellationToken);

        return new PartnerDocumentUploadResponseDto(
            MapearADocumentoDto(documentoActualizado!),
            "Documento reemplazado exitosamente.");
    }

    // Métodos privados auxiliares

    private async Task<ImageUploadResult> CargarACloudinaryAsync(
        IFormFile archivo,
        Guid usuarioId,
        string tipoDocumento)
    {
        using var stream = archivo.OpenReadStream();

        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(archivo.FileName, stream),
            Folder = $"partners/{usuarioId}/documents",
            PublicId = $"{tipoDocumento}_{Guid.NewGuid():N}",
            UseFilename = false,
            UniqueFilename = false,
            Overwrite = false
        };

        var result = await _cloudinary.UploadAsync(uploadParams);

        if (result.Error != null)
        {
            throw new InvalidOperationException($"Error al cargar documento a Cloudinary: {result.Error.Message}");
        }

        return result;
    }

    private void ValidarArchivo(IFormFile archivo)
    {
        if (archivo == null || archivo.Length == 0)
        {
            throw new ArgumentException("El archivo no puede estar vacío.");
        }

        if (archivo.Length > MaxFileSizeBytes)
        {
            throw new ArgumentException($"El archivo no puede exceder {MaxFileSizeBytes / (1024 * 1024)}MB.");
        }

        var extension = Path.GetExtension(archivo.FileName).ToLower();
        if (!ExtensionesPermitidas.Contains(extension))
        {
            throw new ArgumentException(
                $"Tipo de archivo no permitido. Extensiones válidas: {string.Join(", ", ExtensionesPermitidas)}");
        }
    }

    private void ValidarTipoDocumento(string tipoDocumento)
    {
        if (!TiposDocumentoPermitidos.Contains(tipoDocumento, StringComparer.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                $"Tipo de documento no válido. Tipos permitidos: {string.Join(", ", TiposDocumentoPermitidos)}");
        }
    }

    private void ValidarOrdenDocumento(int orden)
    {
        if (orden < 1 || orden > MaxDocumentosPermitidos)
        {
            throw new ArgumentException(
                $"El orden debe estar entre 1 y {MaxDocumentosPermitidos}.");
        }
    }

    private static PartnerDocumentDto MapearADocumentoDto(PartnerDocument documento)
    {
        return new PartnerDocumentDto(
            Id: documento.Id,
            UsuarioId: documento.UsuarioId,
            DocumentUrl: documento.DocumentUrl,
            DocumentType: documento.DocumentType,
            FileName: documento.FileName,
            FileSizeBytes: documento.FileSizeBytes,
            DisplayOrder: documento.DisplayOrder,
            Activo: documento.Activo,
            FechaCreacion: documento.FechaCreacion,
            FechaActualizacion: documento.FechaActualizacion);
    }
}
