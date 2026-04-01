using Microsoft.AspNetCore.Mvc;
using Muzu.Api.Core.DTOs;
using Muzu.Api.Core.Interfaces.Service;
using System.Globalization;

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
        ValidateTramosConsumo(request);

        if (request.LimiteConsumoExtra1 <= request.LimiteConsumoFijo)
        {
            ModelState.AddModelError(nameof(request.LimiteConsumoExtra1), "El tope del extra 1 debe ser mayor que el limite del tramo fijo.");
        }

        if (request.LimiteConsumoExtra2 <= request.LimiteConsumoExtra1)
        {
            ModelState.AddModelError(nameof(request.LimiteConsumoExtra2), "El tope del extra 2 debe ser mayor que el tope del extra 1.");
        }

        if (request.LimiteConsumoExtra3 <= request.LimiteConsumoExtra2)
        {
            ModelState.AddModelError(nameof(request.LimiteConsumoExtra3), "El tope del extra 3 debe ser mayor que el tope del extra 2.");
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var config = await _tenantConfigService.ActualizarAsync(tenantId, request, cancellationToken);
        if (config is null)
        {
            return NotFound();
        }

        return Ok(config);
    }

    private void ValidateTramosConsumo(UpdateTenantConfigDto request)
    {
        if (request.TramosConsumo is null || request.TramosConsumo.Count == 0)
        {
            ModelState.AddModelError(nameof(request.TramosConsumo), "Debes definir al menos un tramo de consumo.");
            return;
        }

        var tramos = request.TramosConsumo
            .OrderBy(t => t.DesdeM3)
            .ToList();

        var tienePorM3 = false;

        for (var i = 0; i < tramos.Count; i++)
        {
            var tramo = tramos[i];
            var path = $"{nameof(request.TramosConsumo)}[{i}]";
            var modo = tramo.ModoCobro?.Trim().ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(modo) || (modo != "fijo_por_rango" && modo != "por_m3"))
            {
                ModelState.AddModelError($"{path}.{nameof(tramo.ModoCobro)}", "ModoCobro debe ser 'fijo_por_rango' o 'por_m3'.");
                continue;
            }

            if (i == 0 && tramo.DesdeM3 < request.LimiteConsumoFijo)
            {
                ModelState.AddModelError($"{path}.{nameof(tramo.DesdeM3)}", "El primer tramo configurable no puede iniciar antes del limite fijo.");
            }

            if (i > 0)
            {
                var previo = tramos[i - 1];
                var esperado = previo.HastaM3 ?? previo.DesdeM3;
                if (tramo.DesdeM3 != esperado)
                {
                    ModelState.AddModelError($"{path}.{nameof(tramo.DesdeM3)}", $"El tramo debe iniciar en {esperado.ToString(CultureInfo.InvariantCulture)} para mantener continuidad.");
                }
            }

            if (modo == "fijo_por_rango")
            {
                if (!tramo.HastaM3.HasValue || tramo.HastaM3.Value <= tramo.DesdeM3)
                {
                    ModelState.AddModelError($"{path}.{nameof(tramo.HastaM3)}", "En fijo_por_rango, HastaM3 debe ser mayor que DesdeM3.");
                }
            }

            if (modo == "por_m3")
            {
                tienePorM3 = true;

                if (tramo.HastaM3.HasValue)
                {
                    ModelState.AddModelError($"{path}.{nameof(tramo.HastaM3)}", "En por_m3, HastaM3 debe ir vacio (null).");
                }

                if (i != tramos.Count - 1)
                {
                    ModelState.AddModelError($"{path}.{nameof(tramo.ModoCobro)}", "El tramo por_m3 debe ser el ultimo.");
                }
            }
        }

        if (!tienePorM3)
        {
            ModelState.AddModelError(nameof(request.TramosConsumo), "Debes incluir un tramo final de tipo por_m3 para el exceso mayor.");
        }
    }
}
