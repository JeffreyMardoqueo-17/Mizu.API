using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Muzu.Api.Core.Models;

namespace Muzu.Api.Core.Interfaces
{
    public interface ITenantRepository
    {
        Task<Tenant> CrearTenantAsync(Tenant tenant);
        Task<Tenant?> ObtenerPorNombreAsync(string nombre);
    }

    public interface IUsuarioRepository
    {
        Task<Usuario> CrearUsuarioAsync(Usuario usuario);
        Task<Usuario?> ObtenerPorCorreoAsync(string correo);
        Task<Usuario?> ObtenerPorIdAsync(Guid id);
    }

    public interface ITenantConfigRepository
    {
        Task<TenantConfig> CrearConfigAsync(TenantConfig config);
        Task<TenantConfig?> ObtenerPorTenantIdAsync(Guid tenantId);
    }

    public interface IMultaRepository
    {
        Task<Multa> CrearMultaAsync(Multa multa);
        Task<IEnumerable<Multa>> ObtenerPorTenantIdAsync(Guid tenantId);
        Task<Multa?> ObtenerPorIdAsync(Guid id);
        Task<bool> ActualizarMultaAsync(Multa multa);
        Task<bool> EliminarMultaAsync(Guid id);
    }

    public interface IRefreshTokenRepository
    {
        Task<RefreshToken> CrearAsync(RefreshToken refreshToken);
        Task<RefreshToken?> ObtenerPorTokenAsync(string token);
        Task<bool> RevocarAsync(string token);
        Task<bool> RevocarTodosDelUsuarioAsync(Guid usuarioId);
        Task<IEnumerable<RefreshToken>> ObtenerPorUsuarioIdAsync(Guid usuarioId);
    }
}
