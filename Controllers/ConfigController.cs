using Microsoft.AspNetCore.Mvc;
using Muzu.Api.Core.DTOs;
using Muzu.Api.Core.Interfaces.Service;

namespace Muzu.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ConfigController : ControllerBase
{
    private readonly ITenantConfigService _tenantConfigService;

    public ConfigController(ITenantConfigService tenantConfigService)
    {
        _tenantConfigService = tenantConfigService;
    }

    [HttpGet("{tenantId:guid}")]
    public async Task<IActionResult> GetConfig([FromRoute] Guid tenantId, CancellationToken cancellationToken)
    {
        var config = await _tenantConfigService.ObtenerPorTenantIdAsync(tenantId, cancellationToken);
        if (config is null)
        {
            return NotFound();
        }

        return Ok(config);
    }

    [HttpPut("{tenantId:guid}")]
    public async Task<IActionResult> UpdateConfig([FromRoute] Guid tenantId, [FromBody] UpdateTenantConfigDto request, CancellationToken cancellationToken)
    {
        var config = await _tenantConfigService.ActualizarAsync(tenantId, request, cancellationToken);
        if (config is null)
        {
            return NotFound();
        }

        return Ok(config);
    }
}
