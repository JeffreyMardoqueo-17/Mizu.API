using System.Data;
using Muzu.Api.Core.Models;

namespace Muzu.Api.Core.Interfaces;

/// <summary>
/// Interfaz para operaciones de datos de documentos de partners.
/// Implementa patrón Repository para acceso a datos de documentos.
/// </summary>
public interface IPartnerDocumentRepository
{
    /// <summary>
    /// Obtiene todos los documentos activos de un usuario específico
    /// </summary>
    Task<IReadOnlyList<PartnerDocument>> ObtenerPorUsuarioIdAsync(
        Guid usuarioId,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene un documento específico por su ID
    /// </summary>
    Task<PartnerDocument?> ObtenerPorIdAsync(
        Guid id,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene la cantidad de documentos activos de un usuario
    /// </summary>
    Task<int> ContarDocumentosActivosAsync(
        Guid usuarioId,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Crea un nuevo documento de partner
    /// </summary>
    Task<PartnerDocument> CrearAsync(
        PartnerDocument documento,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Actualiza información de un documento existente
    /// </summary>
    Task<bool> ActualizarAsync(
        PartnerDocument documento,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Realiza eliminación lógica de un documento
    /// </summary>
    Task<bool> EliminarAsync(
        Guid id,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifica si un documento existe y pertenece al usuario especificado
    /// </summary>
    Task<bool> ExisteYPerteneceAlUsuarioAsync(
        Guid documentoId,
        Guid usuarioId,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default);
}
