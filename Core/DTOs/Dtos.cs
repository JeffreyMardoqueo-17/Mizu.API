using System;

namespace Muzu.Api.Core.DTOs
{
    public record UsuarioRegistroDto(
        string Nombre,
        string Apellido,
        string DUI,
        string Correo,
        string Telefono,
        string Direccion,
        string Password
    );

    public record TenantRegistroDto(
        string Nombre,
        string Direccion
    );

    public record LoginDto(
        string Correo,
        string Password
    );

    public record TenantUsuarioRegistroDto(
        TenantRegistroDto Tenant,
        UsuarioRegistroDto Usuario
    );

    public record RefreshTokenRequestDto(
        string RefreshToken
    );

    public record LoginResponseDto(
        string AccessToken,
        string RefreshToken,
        Guid TenantId,
        Guid UsuarioId,
        string Rol
    );
}
