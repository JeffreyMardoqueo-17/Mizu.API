using Muzu.Api.Core.DTOs;

namespace Muzu.Api.Core.Interfaces.Service;

public interface ITenantConfigService
{
    Task<TenantConfigResponseDto?> ObtenerPorTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<TenantConfigResponseDto?> ActualizarAsync(Guid tenantId, UpdateTenantConfigDto request, CancellationToken cancellationToken = default);
}
