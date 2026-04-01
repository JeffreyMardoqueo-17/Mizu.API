using System.Data;
using Muzu.Api.Core.Models;

namespace Muzu.Api.Core.Interfaces;

public interface IMultaRepository
{
    Task<Multa> CrearMultaAsync(Multa multa, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<Multa>> ObtenerPorTenantIdAsync(Guid tenantId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<Multa?> ObtenerPorIdAsync(Guid id, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<bool> ActualizarMultaAsync(Multa multa, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<bool> EliminarMultaAsync(Guid id, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
}
