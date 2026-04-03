using System.Data;
using Muzu.Api.Core.DTOs;
using Muzu.Api.Core.Models;

namespace Muzu.Api.Core.Interfaces;

public interface IRolRepository
{
    Task<Rol?> ObtenerPorNombreAsync(string nombre, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> ObtenerPermisosPorRolIdAsync(Guid rolId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RoleDto>> ObtenerTodosAsync(IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PermisoDto>> ObtenerTodosPermisosAsync(IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<PermisoDto?> ObtenerPermisoPorCodigoAsync(string codigo, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task ActualizarPermisosDelRolAsync(Guid rolId, List<string> permisoCodigos, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
}
