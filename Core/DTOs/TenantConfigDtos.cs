using System.ComponentModel.DataAnnotations;

namespace Muzu.Api.Core.DTOs;

public sealed record ConsumoTramoDto(
    [param: Range(typeof(decimal), "0", "999999999")] decimal DesdeM3,
    [param: Range(typeof(decimal), "0", "999999999")] decimal? HastaM3,
    [param: Range(typeof(decimal), "0", "999999999")] decimal Cargo,
    [param: Required, StringLength(30)] string ModoCobro
);

public sealed record TenantConfigResponseDto(
    Guid Id,
    Guid TenantId,
    string Moneda,
    decimal LimiteConsumoFijo,
    decimal PrecioConsumoFijo,
    decimal LimiteConsumoExtra1,
    decimal CargoExtra1,
    decimal LimiteConsumoExtra2,
    decimal CargoExtra2,
    decimal LimiteConsumoExtra3,
    decimal CargoExtra3,
    decimal CargoExcesoMayor,
    IReadOnlyList<ConsumoTramoDto> TramosConsumo,
    decimal MultaRetraso,
    decimal MultaNoAsistirReunion,
    decimal MultaNoAsistirTrabajo,
    bool PermitirMultiplesContadores,
    int MaximoContadoresPorUsuario
);

public sealed record UpdateTenantConfigDto(
    [param: Required, StringLength(10)] string Moneda,
    [param: Range(typeof(decimal), "0", "999999999")] decimal LimiteConsumoFijo,
    [param: Range(typeof(decimal), "0", "999999999")] decimal PrecioConsumoFijo,
    [param: Range(typeof(decimal), "0", "999999999")] decimal LimiteConsumoExtra1,
    [param: Range(typeof(decimal), "0", "999999999")] decimal CargoExtra1,
    [param: Range(typeof(decimal), "0", "999999999")] decimal LimiteConsumoExtra2,
    [param: Range(typeof(decimal), "0", "999999999")] decimal CargoExtra2,
    [param: Range(typeof(decimal), "0", "999999999")] decimal LimiteConsumoExtra3,
    [param: Range(typeof(decimal), "0", "999999999")] decimal CargoExtra3,
    [param: Range(typeof(decimal), "0", "999999999")] decimal CargoExcesoMayor,
    [param: Required, MinLength(1)] IReadOnlyList<ConsumoTramoDto> TramosConsumo,
    [param: Range(typeof(decimal), "0", "999999999")] decimal MultaRetraso,
    [param: Range(typeof(decimal), "0", "999999999")] decimal MultaNoAsistirReunion,
    [param: Range(typeof(decimal), "0", "999999999")] decimal MultaNoAsistirTrabajo,
    bool PermitirMultiplesContadores,
    [param: Range(1, 100)] int MaximoContadoresPorUsuario
);
