using System.Data;
using Dapper;
using Muzu.Api.Core.DTOs;
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

    public Task<IReadOnlyList<RoleDto>> ObtenerTodosAsync(IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = """
                           SELECT r.id,
                                  r.nombre,
                                  r.descripcion,
                                  r.es_sistema,
                                  COALESCE(STRING_AGG(p.codigo, ','), '') AS permisos
                           FROM roles r
                           LEFT JOIN rol_permisos rp ON rp.rol_id = r.id
                           LEFT JOIN permisos p ON p.id = rp.permiso_id
                           GROUP BY r.id, r.nombre, r.descripcion, r.es_sistema
                           ORDER BY r.nombre
                           """;

        return WithConnectionAsync<IReadOnlyList<RoleDto>>(
            transaction,
            async connection =>
            {
                var command = new CommandDefinition(sql, transaction: transaction, cancellationToken: cancellationToken);
                var roles = await connection.QueryAsync<(Guid id, string nombre, string? descripcion, bool es_sistema, string permisos)>(command);
                
                var dtos = roles.Select(r => new RoleDto(
                    r.id,
                    r.nombre,
                    r.descripcion,
                    r.es_sistema,
                    string.IsNullOrWhiteSpace(r.permisos) ? new() : r.permisos.Split(',').ToList()
                )).ToList();

                return dtos;
            });
    }

    public Task<IReadOnlyList<PermisoDto>> ObtenerTodosPermisosAsync(IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = """
                           SELECT id, codigo, descripcion
                           FROM permisos
                           ORDER BY codigo
                           """;

        return WithConnectionAsync<IReadOnlyList<PermisoDto>>(
            transaction,
            async connection =>
            {
                var command = new CommandDefinition(sql, transaction: transaction, cancellationToken: cancellationToken);
                var permisos = await connection.QueryAsync<PermisoDto>(command);
                return permisos.ToList();
            });
    }

    public Task<PermisoDto?> ObtenerPermisoPorCodigoAsync(string codigo, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = """
                           SELECT id, codigo, descripcion
                           FROM permisos
                           WHERE LOWER(codigo) = LOWER(@codigo)
                           """;

        return WithConnectionAsync(
            transaction,
            async connection =>
            {
                var command = new CommandDefinition(sql, new { codigo }, transaction, cancellationToken: cancellationToken);
                return await connection.QueryFirstOrDefaultAsync<PermisoDto>(command);
            });
    }

    public Task ActualizarPermisosDelRolAsync(Guid rolId, List<string> permisoCodigos, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = """
                           DELETE FROM rol_permisos
                           WHERE rol_id = @rolId;

                           INSERT INTO rol_permisos (rol_id, permiso_id, fecha_creacion)
                           SELECT @rolId, p.id, NOW()
                           FROM permisos p
                           WHERE LOWER(p.codigo) = ANY(LOWER(CAST(@codigos AS TEXT[])))
                           ON CONFLICT (rol_id, permiso_id) DO NOTHING;
                           """;

        return WithConnectionAsync(
            transaction,
            async connection =>
            {
                var parameters = new
                {
                    rolId,
                    codigos = permisoCodigos.ToArray()
                };

                var command = new CommandDefinition(sql, parameters, transaction, cancellationToken: cancellationToken);
                await connection.ExecuteAsync(command);
                return 0;
            });
    }
}
