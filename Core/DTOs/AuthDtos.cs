using System.ComponentModel.DataAnnotations;

namespace Muzu.Api.Core.DTOs;

public sealed record UsuarioRegistroDto(
    [property: Required, StringLength(100)] string Nombre,
    [property: Required, StringLength(100)] string Apellido,
    [property: Required, StringLength(20)] string DUI,
    [property: Required, EmailAddress, StringLength(255)] string Correo,
    [property: Required, StringLength(20)] string Telefono,
    [property: Required, StringLength(500)] string Direccion,
    [property: Required, MinLength(8), StringLength(200)] string Password
);

public sealed record TenantRegistroDto(
    [property: Required, StringLength(150)] string Nombre,
    [property: Required, StringLength(500)] string Direccion
);

public sealed record LoginDto(
    [property: Required, EmailAddress, StringLength(255)] string Correo,
    [property: Required, MinLength(8), StringLength(200)] string Password
);

public sealed record TenantUsuarioRegistroDto(
    [property: Required] TenantRegistroDto Tenant,
    [property: Required] UsuarioRegistroDto Usuario
);

public sealed record RefreshTokenRequestDto(
    [property: Required] string RefreshToken
);

public sealed record LogoutRequestDto(
    [property: Required] string RefreshToken
);

public sealed record LoginResponseDto(
    Guid TenantId,
    Guid UsuarioId,
    string Rol,
    string RefreshToken
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
