using System.ComponentModel.DataAnnotations;

namespace Muzu.Api.Core.DTOs;

public sealed record CrearUsuarioDto(
    [param: Required, StringLength(100)] string Nombre,
    [param: Required, StringLength(100)] string Apellido,
    [param: Required, StringLength(20)] string DUI,
    [param: Required, EmailAddress, StringLength(255)] string Correo,
    [param: Required, StringLength(20)] string Telefono,
    [param: Required, StringLength(500)] string Direccion,
    [param: Required, MinLength(8), StringLength(200)] string Password,
    [param: Required, StringLength(50)] string Rol
);

public sealed record ActualizarRolUsuarioDto(
    [param: Required, StringLength(50)] string Rol
);

public sealed record UsuarioGestionResponseDto(
    Guid Id,
    Guid TenantId,
    string Nombre,
    string Apellido,
    string Correo,
    string Telefono,
    string Direccion,
    string Rol,
    IReadOnlyList<string> Permisos,
    DateTime FechaCreacion
);

public sealed record UsuarioListaItemDto(
    Guid Id,
    Guid TenantId,
    string Nombre,
    string Apellido,
    string DUI,
    string Correo,
    string Telefono,
    string Rol,
    DateTime FechaCreacion
);

public sealed record UsuarioListadoResponseDto(
    IReadOnlyList<UsuarioListaItemDto> Items,
    int Page,
    int PageSize,
    int Total,
    int TotalPages,
    bool HasNext,
    bool HasPrevious
);
