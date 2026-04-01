using System.Data;
using Dapper;
using Muzu.Api.Core.Interfaces;
using Muzu.Api.Core.Models;

namespace Muzu.Api.Core.Repositories;

public sealed class TenantRepository : RepositoryBase, ITenantRepository
{
    public TenantRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory)
    {
    }

    public Task<Tenant> CrearTenantAsync(Tenant tenant, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = """
                           INSERT INTO tenants (id, nombre, direccion, logo_url, fecha_creacion)
                           VALUES (@Id, @Nombre, @Direccion, @LogoUrl, @FechaCreacion)
                           """;

        return WithConnectionAsync(
            transaction,
            async connection =>
            {
                var command = new CommandDefinition(sql, tenant, transaction, cancellationToken: cancellationToken);
                await connection.ExecuteAsync(command);
                return tenant;
            });
    }

    public Task<Tenant?> ObtenerPorNombreAsync(string nombre, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = """
                           SELECT id, nombre, direccion, logo_url, fecha_creacion
                           FROM tenants
                           WHERE LOWER(nombre) = LOWER(@nombre)
                           """;

        return WithConnectionAsync(
            transaction,
            async connection =>
            {
                var command = new CommandDefinition(sql, new { nombre }, transaction, cancellationToken: cancellationToken);
                return await connection.QueryFirstOrDefaultAsync<Tenant>(command);
            });
    }
}
