namespace Muzu.Api.Core.DTOs;

public sealed record RoleDto(
    Guid Id,
    string Nombre,
    string? Descripcion,
    bool EsSistema,
    List<string> Permisos
);

public sealed record PermisoDto(
    Guid Id,
    string Codigo,
    string? Descripcion
);

public sealed record UpdateRolePermissionsRequestDto(
    Guid RoleId,
    List<string> PermisoCodigos
);

public sealed record RolePermissionResponseDto(
    Guid RoleId,
    string RoleName,
    List<PermisoDto> PermisosDisponibles,
    List<string> PermisosAsignados
);

public sealed record RoleWithPermissionsDto(
    Guid Id,
    string Nombre,
    List<PermisoDto> Permisos,
    bool EsSistema
);
