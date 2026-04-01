using System.Data;
using Dapper;
using Muzu.Api.Core.Interfaces;
using Muzu.Api.Core.Models;

namespace Muzu.Api.Core.Repositories;

public sealed class RolRepository : RepositoryBase, IRolRepository
{
    public RolRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory)
    {
    }

    public Task<Rol?> ObtenerPorNombreAsync(string nombre, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = """
                           SELECT id, nombre
                           FROM roles
                           WHERE LOWER(nombre) = LOWER(@nombre)
                           """;

        return WithConnectionAsync(
            transaction,
            async connection =>
            {
                var command = new CommandDefinition(sql, new { nombre }, transaction, cancellationToken: cancellationToken);
                return await connection.QueryFirstOrDefaultAsync<Rol>(command);
            });
    }

    public Task<IReadOnlyList<string>> ObtenerPermisosPorRolIdAsync(Guid rolId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = """
                           SELECT p.codigo
                           FROM rol_permisos rp
                           INNER JOIN permisos p ON p.id = rp.permiso_id
                           WHERE rp.rol_id = @rolId
                           ORDER BY p.codigo
                           """;

        return WithConnectionAsync<IReadOnlyList<string>>(
            transaction,
            async connection =>
            {
                var command = new CommandDefinition(sql, new { rolId }, transaction, cancellationToken: cancellationToken);
                var permisos = await connection.QueryAsync<string>(command);
                return permisos.ToList();
            });
    }
}
