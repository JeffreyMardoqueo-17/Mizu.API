using System.ComponentModel.DataAnnotations;

namespace Muzu.Api.Core.DTOs;

public sealed record CreateUserRequestDto(
    [param: Required, StringLength(100)] string Nombre,
    [param: Required, StringLength(100)] string Apellido,
    [param: Required, StringLength(20)] string DUI,
    [param: Required, EmailAddress, StringLength(255)] string Correo,
    [param: Required, StringLength(20)] string Telefono,
    [param: Required, StringLength(500)] string Direccion
);

public sealed record UpdateUserRequestDto(
    [param: Required, StringLength(100)] string Nombre,
    [param: Required, StringLength(100)] string Apellido,
    [param: Required, StringLength(20)] string DUI,
    [param: Required, EmailAddress, StringLength(255)] string Correo,
    [param: Required, StringLength(20)] string Telefono,
    [param: Required, StringLength(500)] string Direccion,
    bool Activo
);

public sealed record UserListItemDto(
    Guid Id,
    Guid TenantId,
    string Nombre,
    string Apellido,
    string DUI,
    string Correo,
    string Telefono,
    string Rol,
    bool Activo,
    DateTime FechaCreacion,
    DateTime? FechaActualizacion
);

public sealed record UserDetailDto(
    Guid Id,
    Guid TenantId,
    string Nombre,
    string Apellido,
    string DUI,
    string Correo,
    string Telefono,
    string Direccion,
    string Rol,
    bool Activo,
    bool Eliminado,
    bool MustChangePassword,
    DateTime FechaCreacion,
    DateTime? FechaActualizacion
);

public sealed record UserPaginatedResponseDto(
    IReadOnlyList<UserListItemDto> Items,
    int Page,
    int PageSize,
    int Total,
    int TotalPages,
    bool HasNext,
    bool HasPrevious
);

public sealed record UserCreateResponseDto(
    UserDetailDto Usuario,
    string TemporaryPassword
);
