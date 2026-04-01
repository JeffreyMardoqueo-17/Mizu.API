using Muzu.Api.Core.DTOs;
using Muzu.Api.Core.Models;

namespace Muzu.Api.Core.Mappers;

public static class DtoMappingExtensions
{
    public static Tenant ToEntity(this TenantRegistroDto dto)
    {
        return new Tenant
        {
            Nombre = dto.Nombre.Trim(),
            Direccion = dto.Direccion.Trim()
        };
    }

    public static Usuario ToEntity(this UsuarioRegistroDto dto, Guid tenantId, string passwordHash)
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
            Rol = "Administrador"
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
        return new TenantConfigResponseDto(
            config.Id,
            config.TenantId,
            config.Moneda,
            config.LimiteConsumoFijo,
            config.PrecioConsumoFijo,
            config.LimiteConsumoExtra1,
            config.PorcentajeExtra1,
            config.LimiteConsumoExtra2,
            config.PorcentajeExtra2,
            config.MultaRetraso,
            config.MultaNoAsistirReunion,
            config.MultaNoAsistirTrabajo
        );
    }

    public static void Apply(this UpdateTenantConfigDto source, TenantConfig target)
    {
        target.Moneda = source.Moneda.Trim();
        target.LimiteConsumoFijo = source.LimiteConsumoFijo;
        target.PrecioConsumoFijo = source.PrecioConsumoFijo;
        target.LimiteConsumoExtra1 = source.LimiteConsumoExtra1;
        target.PorcentajeExtra1 = source.PorcentajeExtra1;
        target.LimiteConsumoExtra2 = source.LimiteConsumoExtra2;
        target.PorcentajeExtra2 = source.PorcentajeExtra2;
        target.MultaRetraso = source.MultaRetraso;
        target.MultaNoAsistirReunion = source.MultaNoAsistirReunion;
        target.MultaNoAsistirTrabajo = source.MultaNoAsistirTrabajo;
    }
}
