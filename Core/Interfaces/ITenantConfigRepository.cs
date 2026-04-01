using System.Data;
using Muzu.Api.Core.Models;

namespace Muzu.Api.Core.Interfaces;

public interface ITenantConfigRepository
{
    Task<TenantConfig> CrearConfigAsync(TenantConfig config, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<TenantConfig?> ObtenerPorTenantIdAsync(Guid tenantId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<bool> ActualizarAsync(TenantConfig config, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
}
