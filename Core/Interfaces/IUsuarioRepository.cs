using System.Data;
using Muzu.Api.Core.Models;

namespace Muzu.Api.Core.Interfaces;

public interface IUsuarioRepository
{
    Task<Usuario> CrearUsuarioAsync(Usuario usuario, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<Usuario?> ObtenerPorCorreoAsync(string correo, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<Usuario?> ObtenerPorIdAsync(Guid id, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<Usuario?> ObtenerPorIdYTenantAsync(Guid id, Guid tenantId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task AsignarRolAsync(Guid usuarioId, Guid rolId, string rolNombre, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<bool> ExisteOtroUsuarioConRolAsync(Guid tenantId, string rolNombre, Guid excludingUsuarioId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> ObtenerPermisosActivosAsync(Guid usuarioId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Usuario> Items, int Total)> ListarPorTenantAsync(
        Guid tenantId,
        string? search,
        int page,
        int pageSize,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default);
}
