using System.Data;
using Muzu.Api.Core.Models;

namespace Muzu.Api.Core.Interfaces;

public interface ITenantRepository
{
    Task<Tenant> CrearTenantAsync(Tenant tenant, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<Tenant?> ObtenerPorNombreAsync(string nombre, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
}
