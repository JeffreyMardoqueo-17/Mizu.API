using System.Data;
using Muzu.Api.Core.Models;

namespace Muzu.Api.Core.Interfaces;

public interface IReunionRepository
{
    Task<IReadOnlyList<Reunion>> ListarPorTenantAsync(Guid tenantId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);

    Task<Reunion?> ObtenerPorIdAsync(Guid tenantId, Guid reunionId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);

    Task<Reunion> CrearAsync(Reunion reunion, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);

    Task<bool> ActualizarAsync(Reunion reunion, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);

    Task<bool> CambiarEstadoAsync(Guid reunionId, Guid tenantId, string estado, DateTime? fechaInicio, DateTime? fechaFin, TimeOnly? horaFin = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);

    Task<bool> ActualizarAcuerdosAsync(Guid reunionId, Guid tenantId, string acuerdosJson, string? notasSecretaria, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReunionAsistencia>> ObtenerAsistenciasAsync(Guid reunionId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);

    Task<ReunionAsistencia> CrearAsistenciaAsync(ReunionAsistencia asistencia, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);

    Task<bool> ActualizarAsistenciaAsync(Guid reunionId, Guid usuarioId, bool asistio, string? observacion, Guid? marcadoPorUsuarioId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReunionHistorial>> ObtenerHistorialAsync(Guid reunionId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);

    Task<ReunionHistorial> AgregarHistorialAsync(ReunionHistorial historial, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
}