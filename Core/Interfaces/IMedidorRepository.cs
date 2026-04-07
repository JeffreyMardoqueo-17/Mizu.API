using System.Data;
using Muzu.Api.Core.Models;

namespace Muzu.Api.Core.Interfaces;

public interface IMedidorRepository
{
    Task<IReadOnlyList<Medidor>> ObtenerPorUsuarioAsync(
        Guid tenantId,
        Guid usuarioId,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default);

    Task<Medidor?> ObtenerPorIdAsync(
        Guid tenantId,
        Guid medidorId,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default);

    Task<int> ContarPorUsuarioAsync(
        Guid tenantId,
        Guid usuarioId,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default);

    Task<int> ContarActivosPorUsuarioAsync(
        Guid tenantId,
        Guid usuarioId,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default);

    Task<long?> ObtenerNiuUsuarioAsync(
        Guid tenantId,
        Guid usuarioId,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default);

    Task<long> AsignarNiuCorrelativoSiFaltaAsync(
        Guid tenantId,
        Guid usuarioId,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default);

    Task<long> ObtenerSiguienteNumeroMedidorAsync(
        Guid tenantId,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default);

    Task<Medidor> CrearAsync(
        Medidor medidor,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default);

    Task<Medidor?> ActualizarEstadoAsync(
        Guid tenantId,
        Guid medidorId,
        bool activo,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default);

    Task<int> DesactivarTodosPorUsuarioAsync(
        Guid tenantId,
        Guid usuarioId,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default);

    Task<int> SincronizarActivosPorUsuarioAsync(
        Guid tenantId,
        Guid usuarioId,
        int maximoActivos,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MeterRuleConflictRow>> ObtenerUsuariosConExcesoActivosAsync(
        Guid tenantId,
        int maximoActivosPermitidos,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default);
}
