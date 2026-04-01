using System.Data;
using Muzu.Api.Core.Models;

namespace Muzu.Api.Core.Interfaces;

public interface IRolRepository
{
    Task<Rol?> ObtenerPorNombreAsync(string nombre, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> ObtenerPermisosPorRolIdAsync(Guid rolId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
}
