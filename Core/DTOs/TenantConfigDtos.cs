using System.ComponentModel.DataAnnotations;

namespace Muzu.Api.Core.DTOs;

public sealed record TenantConfigResponseDto(
    Guid Id,
    Guid TenantId,
    string Moneda,
    decimal LimiteConsumoFijo,
    decimal PrecioConsumoFijo,
    decimal LimiteConsumoExtra1,
    decimal PorcentajeExtra1,
    decimal LimiteConsumoExtra2,
    decimal PorcentajeExtra2,
    decimal MultaRetraso,
    decimal MultaNoAsistirReunion,
    decimal MultaNoAsistirTrabajo
);

public sealed record UpdateTenantConfigDto(
    [property: Required, StringLength(10)] string Moneda,
    [property: Range(typeof(decimal), "0", "999999999")] decimal LimiteConsumoFijo,
    [property: Range(typeof(decimal), "0", "999999999")] decimal PrecioConsumoFijo,
    [property: Range(typeof(decimal), "0", "999999999")] decimal LimiteConsumoExtra1,
    [property: Range(typeof(decimal), "0", "999999999")] decimal PorcentajeExtra1,
    [property: Range(typeof(decimal), "0", "999999999")] decimal LimiteConsumoExtra2,
    [property: Range(typeof(decimal), "0", "999999999")] decimal PorcentajeExtra2,
    [property: Range(typeof(decimal), "0", "999999999")] decimal MultaRetraso,
    [property: Range(typeof(decimal), "0", "999999999")] decimal MultaNoAsistirReunion,
    [property: Range(typeof(decimal), "0", "999999999")] decimal MultaNoAsistirTrabajo
);
