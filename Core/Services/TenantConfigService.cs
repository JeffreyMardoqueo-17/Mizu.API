using Muzu.Api.Core.DTOs;
using Muzu.Api.Core.Interfaces;
using Muzu.Api.Core.Interfaces.Service;
using Muzu.Api.Core.Mappers;
using Muzu.Api.Core.Models;

namespace Muzu.Api.Core.Services;

public sealed class TenantConfigService : ITenantConfigService
{
    private readonly ITenantConfigRepository _tenantConfigRepository;

    public TenantConfigService(ITenantConfigRepository tenantConfigRepository)
    {
        _tenantConfigRepository = tenantConfigRepository;
    }

    public async Task<TenantConfigResponseDto?> ObtenerPorTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var config = await _tenantConfigRepository.ObtenerPorTenantIdAsync(tenantId, cancellationToken: cancellationToken);

        if (config is null)
        {
            config = new TenantConfig
            {
                TenantId = tenantId
            };

            await _tenantConfigRepository.CrearConfigAsync(config, cancellationToken: cancellationToken);
        }

        return config.ToResponseDto();
    }

    public async Task<TenantConfigResponseDto?> ActualizarAsync(Guid tenantId, UpdateTenantConfigDto request, CancellationToken cancellationToken = default)
    {
        var config = await _tenantConfigRepository.ObtenerPorTenantIdAsync(tenantId, cancellationToken: cancellationToken);
        if (config is null)
        {
            return null;
        }

        request.Apply(config);
        await _tenantConfigRepository.ActualizarAsync(config, cancellationToken: cancellationToken);

        return config.ToResponseDto();
    }
}
