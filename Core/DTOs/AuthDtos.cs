using System.ComponentModel.DataAnnotations;

namespace Muzu.Api.Core.DTOs;

public sealed record UsuarioRegistroDto(
    [param: Required, StringLength(100)] string Nombre,
    [param: Required, StringLength(100)] string Apellido,
    [param: Required, StringLength(20)] string DUI,
    [param: Required, EmailAddress, StringLength(255)] string Correo,
    [param: Required, StringLength(20)] string Telefono,
    [param: Required, StringLength(500)] string Direccion,
    [param: Required, MinLength(8), StringLength(200)] string Password
);

public sealed record TenantRegistroDto(
    [param: Required, StringLength(150)] string Nombre,
    [param: Required, StringLength(500)] string Direccion
);

public sealed record LoginDto(
    [param: Required, EmailAddress, StringLength(255)] string Correo,
    [param: Required, MinLength(8), StringLength(200)] string Password
);

public sealed record TenantUsuarioRegistroDto(
    [param: Required] TenantRegistroDto Tenant,
    [param: Required] UsuarioRegistroDto Usuario,
    InitialTenantConfigDto? ConfiguracionInicial
);

public sealed record InitialTenantConfigDto(
    [param: StringLength(10)] string? Moneda,
    [param: Range(typeof(decimal), "0", "999999999")] decimal? LimiteConsumoFijo,
    [param: Range(typeof(decimal), "0", "999999999")] decimal? PrecioConsumoFijo,
    IReadOnlyList<ConsumoTramoDto>? TramosConsumo,
    [param: Range(typeof(decimal), "0", "999999999")] decimal? MultaRetraso,
    [param: Range(typeof(decimal), "0", "999999999")] decimal? MultaNoAsistirReunion,
    [param: Range(typeof(decimal), "0", "999999999")] decimal? MultaNoAsistirTrabajo
);

public sealed record RefreshTokenRequestDto(
    [param: Required] string RefreshToken
);

public sealed record LogoutRequestDto(
    [param: Required] string RefreshToken
);

public sealed record LoginResponseDto(
    Guid TenantId,
    Guid UsuarioId,
    string Nombre,
    string Apellido,
    string Rol,
    string RefreshToken,
    bool MustChangePassword
);

public sealed record UsuarioResumenDto(
    Guid Id,
    Guid TenantId,
    string Nombre,
    string Apellido,
    string Correo,
    string Telefono,
    string Direccion,
    string Rol,
    DateTime FechaCreacion
);

public sealed record TenantResumenDto(
    Guid Id,
    string Nombre,
    string Direccion,
    string? LogoUrl,
    DateTime FechaCreacion,
    TenantConfigResponseDto Configuracion
);

public sealed record RegisterTenantResponseDto(
    UsuarioResumenDto Usuario,
    TenantResumenDto Tenant
);

public sealed record AuthenticatedCommandResultDto(
    string AccessToken,
    LoginResponseDto Response
);

public sealed record RegisterTenantCommandResultDto(
    string AccessToken,
    string RefreshToken,
    RegisterTenantResponseDto Response
);
