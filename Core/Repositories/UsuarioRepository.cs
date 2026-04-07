using System.Data;
using Dapper;
using Muzu.Api.Core.Interfaces;
using Muzu.Api.Core.Models;

namespace Muzu.Api.Core.Repositories;

public sealed class UsuarioRepository : RepositoryBase, IUsuarioRepository
{
    private const string SelectUsuarioSql = """
                                     SELECT u.id,
                                            u.tenant_id,
                                            u.nombre,
                                            u.apellido,
                                            u.dui,
                                            u.correo,
                                            u.telefono,
                                            u.direccion,
                                            u.password_hash,
                                            u.niu,
                                            COALESCE(rol.nombre, COALESCE(NULLIF(u.rol, ''), 'Socio')) AS rol,
                                              u.activo,
                                              u.eliminado,
                                              u.must_change_password,
                                              u.fecha_creacion,
                                              u.fecha_actualizacion,
                                              u.fecha_eliminacion,
                                              u.temp_password_generated_at,
                                              u.temp_password_viewed_at
                                     FROM usuarios u
                                     LEFT JOIN LATERAL (
                                         SELECT r.nombre
                                         FROM usuario_roles ur
                                         INNER JOIN roles r ON r.id = ur.rol_id
                                         WHERE ur.usuario_id = u.id
                                           AND ur.activo = TRUE
                                           AND (ur.fecha_fin IS NULL OR ur.fecha_fin >= CURRENT_DATE)
                                         ORDER BY ur.fecha_creacion DESC
                                         LIMIT 1
                                     ) rol ON TRUE
                                     """;

    public UsuarioRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory)
    {
    }

    public Task<Usuario> CrearUsuarioAsync(Usuario usuario, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = """
                           INSERT INTO usuarios (id, tenant_id, nombre, apellido, dui, correo, telefono, direccion, password_hash, niu, rol, activo, eliminado, must_change_password, fecha_creacion, fecha_actualizacion, temp_password_generated_at, temp_password_viewed_at)
                           VALUES (
                               @Id,
                               @TenantId,
                               @Nombre,
                               @Apellido,
                               @DUI,
                               @Correo,
                               @Telefono,
                               @Direccion,
                               @PasswordHash,
                               COALESCE(@Niu, (SELECT COALESCE(MAX(u2.niu), 0) + 1 FROM usuarios u2 WHERE u2.tenant_id = @TenantId)),
                               @Rol,
                               @Activo,
                               @Eliminado,
                               @MustChangePassword,
                               @FechaCreacion,
                               @FechaActualizacion,
                               @TempPasswordGeneratedAt,
                               @TempPasswordViewedAt
                           )
                           RETURNING niu
                           """;

        return WithConnectionAsync(
            transaction,
            async connection =>
            {
                var command = new CommandDefinition(sql, usuario, transaction, cancellationToken: cancellationToken);
                usuario.Niu = await connection.ExecuteScalarAsync<long>(command);
                return usuario;
            });
    }

    public Task<Usuario?> ObtenerPorCorreoAsync(string correo, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        var sql = SelectUsuarioSql + " WHERE LOWER(u.correo) = LOWER(@correo) AND u.eliminado = FALSE AND u.activo = TRUE";

        return WithConnectionAsync(
            transaction,
            async connection =>
            {
                var command = new CommandDefinition(sql, new { correo }, transaction, cancellationToken: cancellationToken);
                return await connection.QueryFirstOrDefaultAsync<Usuario>(command);
            });
    }

    public Task<Usuario?> ObtenerPorIdAsync(Guid id, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        var sql = SelectUsuarioSql + " WHERE u.id = @id AND u.eliminado = FALSE";

        return WithConnectionAsync(
            transaction,
            async connection =>
            {
                var command = new CommandDefinition(sql, new { id }, transaction, cancellationToken: cancellationToken);
                return await connection.QueryFirstOrDefaultAsync<Usuario>(command);
            });
    }

    public Task<Usuario?> ObtenerPorIdYTenantAsync(Guid id, Guid tenantId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        var sql = SelectUsuarioSql + " WHERE u.id = @id AND u.tenant_id = @tenantId AND u.eliminado = FALSE";

        return WithConnectionAsync(
            transaction,
            async connection =>
            {
                var command = new CommandDefinition(sql, new { id, tenantId }, transaction, cancellationToken: cancellationToken);
                return await connection.QueryFirstOrDefaultAsync<Usuario>(command);
            });
    }

    public Task<Usuario?> ObtenerPorIdYTenantIncluyendoInactivosAsync(Guid id, Guid tenantId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        var sql = SelectUsuarioSql + " WHERE u.id = @id AND u.tenant_id = @tenantId AND u.eliminado = FALSE";

        return WithConnectionAsync(
            transaction,
            async connection =>
            {
                var command = new CommandDefinition(sql, new { id, tenantId }, transaction, cancellationToken: cancellationToken);
                return await connection.QueryFirstOrDefaultAsync<Usuario>(command);
            });
    }

    public async Task AsignarRolAsync(Guid usuarioId, Guid rolId, string rolNombre, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = """
                           UPDATE usuario_roles
                           SET activo = FALSE,
                               fecha_fin = CURRENT_DATE
                           WHERE usuario_id = @usuarioId
                             AND activo = TRUE;

                           INSERT INTO usuario_roles (id, usuario_id, rol_id, fecha_inicio, activo, fecha_creacion)
                           VALUES (@id, @usuarioId, @rolId, CURRENT_DATE, TRUE, NOW());

                           UPDATE usuarios
                           SET rol = @rolNombre
                           WHERE id = @usuarioId;
                           """;

        await WithConnectionAsync(
            transaction,
            async connection =>
            {
                var parameters = new
                {
                    id = Guid.NewGuid(),
                    usuarioId,
                    rolId,
                    rolNombre
                };

                var command = new CommandDefinition(sql, parameters, transaction, cancellationToken: cancellationToken);
                await connection.ExecuteAsync(command);
                return 0;
            });
    }

    public Task<bool> ExisteOtroUsuarioConRolAsync(Guid tenantId, string rolNombre, Guid excludingUsuarioId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = """
                           SELECT COUNT(1)
                           FROM usuarios u
                           INNER JOIN usuario_roles ur ON ur.usuario_id = u.id
                           INNER JOIN roles r ON r.id = ur.rol_id
                           WHERE u.tenant_id = @tenantId
                             AND u.id <> @excludingUsuarioId
                             AND LOWER(r.nombre) = LOWER(@rolNombre)
                             AND ur.activo = TRUE
                             AND (ur.fecha_fin IS NULL OR ur.fecha_fin >= CURRENT_DATE)
                           """;

        return WithConnectionAsync(
            transaction,
            async connection =>
            {
                var command = new CommandDefinition(sql, new { tenantId, rolNombre, excludingUsuarioId }, transaction, cancellationToken: cancellationToken);
                var count = await connection.ExecuteScalarAsync<int>(command);
                return count > 0;
            });
    }

    public Task<IReadOnlyList<string>> ObtenerPermisosActivosAsync(Guid usuarioId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = """
                           SELECT DISTINCT p.codigo
                           FROM usuario_roles ur
                           INNER JOIN rol_permisos rp ON rp.rol_id = ur.rol_id
                           INNER JOIN permisos p ON p.id = rp.permiso_id
                           WHERE ur.usuario_id = @usuarioId
                             AND ur.activo = TRUE
                             AND (ur.fecha_fin IS NULL OR ur.fecha_fin >= CURRENT_DATE)
                           ORDER BY p.codigo
                           """;

        return WithConnectionAsync<IReadOnlyList<string>>(
            transaction,
            async connection =>
            {
                var command = new CommandDefinition(sql, new { usuarioId }, transaction, cancellationToken: cancellationToken);
                var rows = await connection.QueryAsync<string>(command);
                return rows.ToList();
            });
    }

    public Task<(IReadOnlyList<Usuario> Items, int Total)> ListarPorTenantAsync(
        Guid tenantId,
        string? search,
        int page,
        int pageSize,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        const string whereSql = """
                                WHERE u.tenant_id = @tenantId
                                                                    AND u.eliminado = FALSE
                                  AND (
                                      @search IS NULL
                                      OR @search = ''
                                      OR LOWER(CONCAT(u.nombre, ' ', u.apellido)) LIKE LOWER(@searchLike)
                                      OR regexp_replace(u.dui, '[^0-9]', '', 'g') LIKE @searchDuiLike
                                  )
                                """;

        var selectSql = SelectUsuarioSql + "\n" + whereSql + "\nORDER BY u.fecha_creacion DESC\nOFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY";
        var countSql = "SELECT COUNT(1) FROM usuarios u\n" + whereSql;

        return WithConnectionAsync(
            transaction,
            async connection =>
            {
                var searchValue = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
                var normalizedDui = string.IsNullOrWhiteSpace(searchValue)
                    ? null
                    : new string(searchValue.Where(char.IsDigit).ToArray());

                var parameters = new
                {
                    tenantId,
                    search = searchValue,
                    searchLike = searchValue is null ? null : $"%{searchValue}%",
                    searchDuiLike = normalizedDui is null ? null : $"%{normalizedDui}%",
                    offset = (page - 1) * pageSize,
                    pageSize
                };

                var countCommand = new CommandDefinition(countSql, parameters, transaction, cancellationToken: cancellationToken);
                var total = await connection.ExecuteScalarAsync<int>(countCommand);

                var listCommand = new CommandDefinition(selectSql, parameters, transaction, cancellationToken: cancellationToken);
                var items = (await connection.QueryAsync<Usuario>(listCommand)).ToList();

                return ((IReadOnlyList<Usuario>)items, total);
            });
    }

    public Task<(IReadOnlyList<Usuario> Items, int Total)> ListarAvanzadoPorTenantAsync(
        Guid tenantId,
        string? dui,
        string? nombre,
        string? correo,
        bool? estado,
        int page,
        int pageSize,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        const string whereSql = """
                                WHERE u.tenant_id = @tenantId
                                  AND u.eliminado = FALSE
                                  AND (@estado IS NULL OR u.activo = @estado)
                                  AND (@nombre IS NULL OR @nombre = '' OR LOWER(CONCAT(u.nombre, ' ', u.apellido)) LIKE LOWER(@nombreLike))
                                  AND (@correo IS NULL OR @correo = '' OR LOWER(u.correo) LIKE LOWER(@correoLike))
                                  AND (
                                      @dui IS NULL
                                      OR @dui = ''
                                      OR regexp_replace(u.dui, '[^0-9]', '', 'g') LIKE @duiLike
                                  )
                                """;

        var selectSql = SelectUsuarioSql + "\n" + whereSql + "\nORDER BY u.fecha_creacion DESC\nOFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY";
        var countSql = "SELECT COUNT(1) FROM usuarios u\n" + whereSql;

        return WithConnectionAsync(
            transaction,
            async connection =>
            {
                var normalizedDui = string.IsNullOrWhiteSpace(dui)
                    ? null
                    : new string(dui.Where(char.IsDigit).ToArray());

                var parameters = new
                {
                    tenantId,
                    estado,
                    nombre = string.IsNullOrWhiteSpace(nombre) ? null : nombre.Trim(),
                    nombreLike = string.IsNullOrWhiteSpace(nombre) ? null : $"%{nombre.Trim()}%",
                    correo = string.IsNullOrWhiteSpace(correo) ? null : correo.Trim(),
                    correoLike = string.IsNullOrWhiteSpace(correo) ? null : $"%{correo.Trim()}%",
                    dui = normalizedDui,
                    duiLike = normalizedDui is null ? null : $"%{normalizedDui}%",
                    offset = (page - 1) * pageSize,
                    pageSize
                };

                var countCommand = new CommandDefinition(countSql, parameters, transaction, cancellationToken: cancellationToken);
                var total = await connection.ExecuteScalarAsync<int>(countCommand);

                var listCommand = new CommandDefinition(selectSql, parameters, transaction, cancellationToken: cancellationToken);
                var items = (await connection.QueryAsync<Usuario>(listCommand)).ToList();

                return ((IReadOnlyList<Usuario>)items, total);
            });
    }

    public Task<Usuario?> ActualizarDatosAsync(Usuario usuario, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = """
                           UPDATE usuarios
                           SET nombre = @Nombre,
                               apellido = @Apellido,
                               dui = @DUI,
                               correo = @Correo,
                               telefono = @Telefono,
                               direccion = @Direccion,
                               activo = @Activo,
                               fecha_actualizacion = NOW()
                           WHERE id = @Id
                             AND tenant_id = @TenantId
                             AND eliminado = FALSE;
                           """;

        return WithConnectionAsync(
            transaction,
            async connection =>
            {
                var command = new CommandDefinition(sql, usuario, transaction, cancellationToken: cancellationToken);
                var affected = await connection.ExecuteAsync(command);
                return affected > 0 ? usuario : null;
            });
    }

    public Task<bool> SetActiveStateAsync(Guid id, Guid tenantId, bool activo, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = """
                           UPDATE usuarios
                           SET activo = @activo,
                               fecha_actualizacion = NOW()
                           WHERE id = @id
                             AND tenant_id = @tenantId
                             AND eliminado = FALSE
                           """;

        return WithConnectionAsync(
            transaction,
            async connection =>
            {
                var command = new CommandDefinition(sql, new { id, tenantId, activo }, transaction, cancellationToken: cancellationToken);
                var affected = await connection.ExecuteAsync(command);
                return affected > 0;
            });
    }

    public Task<bool> EliminarLogicoAsync(Guid id, Guid tenantId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = """
                           UPDATE usuarios
                           SET eliminado = TRUE,
                               activo = FALSE,
                               fecha_eliminacion = NOW(),
                               fecha_actualizacion = NOW()
                           WHERE id = @id
                             AND tenant_id = @tenantId
                             AND eliminado = FALSE
                           """;

        return WithConnectionAsync(
            transaction,
            async connection =>
            {
                var command = new CommandDefinition(sql, new { id, tenantId }, transaction, cancellationToken: cancellationToken);
                var affected = await connection.ExecuteAsync(command);
                return affected > 0;
            });
    }

    public Task<bool> EstablecerPasswordTemporalAsync(Guid id, Guid tenantId, string passwordHash, bool markAsViewed, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = """
                           UPDATE usuarios
                           SET password_hash = @passwordHash,
                               must_change_password = TRUE,
                               temp_password_generated_at = NOW(),
                               temp_password_viewed_at = CASE WHEN @markAsViewed THEN NOW() ELSE NULL END,
                               fecha_actualizacion = NOW()
                           WHERE id = @id
                             AND tenant_id = @tenantId
                             AND eliminado = FALSE
                           """;

        return WithConnectionAsync(
            transaction,
            async connection =>
            {
                var command = new CommandDefinition(sql, new { id, tenantId, passwordHash, markAsViewed }, transaction, cancellationToken: cancellationToken);
                var affected = await connection.ExecuteAsync(command);
                return affected > 0;
            });
    }

    public Task<bool> EstablecerPasswordDePeriodoAsync(Guid id, Guid tenantId, string passwordHash, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = """
                           UPDATE usuarios
                           SET password_hash = @passwordHash,
                               must_change_password = FALSE,
                               temp_password_generated_at = NOW(),
                               temp_password_viewed_at = NOW(),
                               fecha_actualizacion = NOW()
                           WHERE id = @id
                             AND tenant_id = @tenantId
                             AND eliminado = FALSE
                           """;

        return WithConnectionAsync(
            transaction,
            async connection =>
            {
                var command = new CommandDefinition(sql, new { id, tenantId, passwordHash }, transaction, cancellationToken: cancellationToken);
                var affected = await connection.ExecuteAsync(command);
                return affected > 0;
            });
    }

    public Task<bool> CambiarPasswordDefinitivaAsync(Guid id, Guid tenantId, string passwordHash, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = """
                           UPDATE usuarios
                           SET password_hash = @passwordHash,
                               must_change_password = FALSE,
                               fecha_actualizacion = NOW()
                           WHERE id = @id
                             AND tenant_id = @tenantId
                             AND eliminado = FALSE
                           """;

        return WithConnectionAsync(
            transaction,
            async connection =>
            {
                var command = new CommandDefinition(sql, new { id, tenantId, passwordHash }, transaction, cancellationToken: cancellationToken);
                var affected = await connection.ExecuteAsync(command);
                return affected > 0;
            });
    }
}
