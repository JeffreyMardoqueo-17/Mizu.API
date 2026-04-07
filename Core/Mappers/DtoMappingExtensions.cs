using Muzu.Api.Core.DTOs;
using Muzu.Api.Core.Models;
using System.Text.Json;

namespace Muzu.Api.Core.Mappers;

public static class DtoMappingExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static Tenant ToEntity(this TenantRegistroDto dto)
    {
        return new Tenant
        {
            Nombre = dto.Nombre.Trim(),
            Direccion = dto.Direccion.Trim()
        };
    }

    public static Usuario ToEntity(this UsuarioRegistroDto dto, Guid tenantId, string passwordHash, string rol)
    {
        return new Usuario
        {
            TenantId = tenantId,
            Nombre = dto.Nombre.Trim(),
            Apellido = dto.Apellido.Trim(),
            DUI = dto.DUI.Trim(),
            Correo = dto.Correo.Trim(),
            Telefono = dto.Telefono.Trim(),
            Direccion = dto.Direccion.Trim(),
            PasswordHash = passwordHash,
            Rol = rol
        };
    }

    public static UsuarioResumenDto ToResumenDto(this Usuario usuario)
    {
        return new UsuarioResumenDto(
            usuario.Id,
            usuario.TenantId,
            usuario.Nombre,
            usuario.Apellido,
            usuario.Correo,
            usuario.Telefono,
            usuario.Direccion,
            usuario.Rol,
            usuario.FechaCreacion
        );
    }

    public static TenantResumenDto ToResumenDto(this Tenant tenant, TenantConfigResponseDto configuracion)
    {
        return new TenantResumenDto(
            tenant.Id,
            tenant.Nombre,
            tenant.Direccion,
            tenant.LogoUrl,
            tenant.FechaCreacion,
            configuracion
        );
    }

    public static TenantConfigResponseDto ToResponseDto(this TenantConfig config)
    {
        var tramos = ParseTramosOrDefault(config);

        return new TenantConfigResponseDto(
            config.Id,
            config.TenantId,
            config.Moneda,
            config.LimiteConsumoFijo,
            config.PrecioConsumoFijo,
            config.LimiteConsumoExtra1,
            config.CargoExtra1,
            config.LimiteConsumoExtra2,
            config.CargoExtra2,
            config.LimiteConsumoExtra3,
            config.CargoExtra3,
            config.CargoExcesoMayor,
            tramos,
            config.MultaRetraso,
            config.MultaNoAsistirReunion,
            config.MultaNoAsistirTrabajo,
            config.PermitirMultiplesContadores,
            config.MaximoContadoresPorUsuario
        );
    }

    public static void Apply(this UpdateTenantConfigDto source, TenantConfig target)
    {
        var normalizedTramos = NormalizeTramos(source.TramosConsumo);

        target.Moneda = source.Moneda.Trim();
        target.LimiteConsumoFijo = source.LimiteConsumoFijo;
        target.PrecioConsumoFijo = source.PrecioConsumoFijo;

        target.TramosConsumoJson = JsonSerializer.Serialize(normalizedTramos, JsonOptions);

        var tramosFijos = normalizedTramos
            .Where(x => string.Equals(x.ModoCobro, "fijo_por_rango", StringComparison.OrdinalIgnoreCase) && x.HastaM3.HasValue)
            .OrderBy(x => x.DesdeM3)
            .ToList();

        var tramoExceso = normalizedTramos
            .FirstOrDefault(x => string.Equals(x.ModoCobro, "por_m3", StringComparison.OrdinalIgnoreCase));

        target.LimiteConsumoExtra1 = tramosFijos.ElementAtOrDefault(0)?.HastaM3 ?? source.LimiteConsumoExtra1;
        target.CargoExtra1 = tramosFijos.ElementAtOrDefault(0)?.Cargo ?? source.CargoExtra1;
        target.LimiteConsumoExtra2 = tramosFijos.ElementAtOrDefault(1)?.HastaM3 ?? source.LimiteConsumoExtra2;
        target.CargoExtra2 = tramosFijos.ElementAtOrDefault(1)?.Cargo ?? source.CargoExtra2;
        target.LimiteConsumoExtra3 = tramosFijos.ElementAtOrDefault(2)?.HastaM3 ?? source.LimiteConsumoExtra3;
        target.CargoExtra3 = tramosFijos.ElementAtOrDefault(2)?.Cargo ?? source.CargoExtra3;
        target.CargoExcesoMayor = tramoExceso?.Cargo ?? source.CargoExcesoMayor;
        target.MultaRetraso = source.MultaRetraso;
        target.MultaNoAsistirReunion = source.MultaNoAsistirReunion;
        target.MultaNoAsistirTrabajo = source.MultaNoAsistirTrabajo;
        target.PermitirMultiplesContadores = source.PermitirMultiplesContadores;
        target.MaximoContadoresPorUsuario = source.MaximoContadoresPorUsuario < 1 ? 1 : source.MaximoContadoresPorUsuario;
    }

    private static IReadOnlyList<ConsumoTramoDto> ParseTramosOrDefault(TenantConfig config)
    {
        if (!string.IsNullOrWhiteSpace(config.TramosConsumoJson))
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<List<ConsumoTramoDto>>(config.TramosConsumoJson, JsonOptions);
                if (parsed is { Count: > 0 })
                {
                    return NormalizeTramos(parsed);
                }
            }
            catch (JsonException)
            {
            }
        }

        return BuildDefaultTramos(config);
    }

    private static IReadOnlyList<ConsumoTramoDto> BuildDefaultTramos(TenantConfig config)
    {
        return new List<ConsumoTramoDto>
        {
            new(config.LimiteConsumoFijo, config.LimiteConsumoExtra1, config.CargoExtra1, "fijo_por_rango"),
            new(config.LimiteConsumoExtra1, config.LimiteConsumoExtra2, config.CargoExtra2, "fijo_por_rango"),
            new(config.LimiteConsumoExtra2, config.LimiteConsumoExtra3, config.CargoExtra3, "fijo_por_rango"),
            new(config.LimiteConsumoExtra3, null, config.CargoExcesoMayor, "por_m3")
        };
    }

    private static IReadOnlyList<ConsumoTramoDto> NormalizeTramos(IEnumerable<ConsumoTramoDto> tramos)
    {
        return tramos
            .Select(t => new ConsumoTramoDto(t.DesdeM3, t.HastaM3, t.Cargo, t.ModoCobro.Trim().ToLowerInvariant()))
            .OrderBy(t => t.DesdeM3)
            .ToList();
    }
}
