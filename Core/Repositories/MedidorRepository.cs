using System.Data;
using Dapper;
using Muzu.Api.Core.Interfaces;
using Muzu.Api.Core.Models;

namespace Muzu.Api.Core.Repositories;

public sealed class MedidorRepository : RepositoryBase, IMedidorRepository
{
    private const string SelectMedidorSql = """
        SELECT
            id,
            tenant_id as "TenantId",
            usuario_id as "UsuarioId",
            numero_medidor as "NumeroMedidor",
            activo,
            fecha_creacion as "FechaCreacion",
            fecha_actualizacion as "FechaActualizacion",
            eliminado
        FROM medidores
        WHERE eliminado = FALSE
        """;

    public MedidorRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory)
    {
    }

    public Task<IReadOnlyList<Medidor>> ObtenerPorUsuarioAsync(
        Guid tenantId,
        Guid usuarioId,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        var sql = SelectMedidorSql + " AND tenant_id = @TenantId AND usuario_id = @UsuarioId ORDER BY numero_medidor ASC";

        return WithConnectionAsync(
            transaction,
            async connection =>
            {
                var command = new CommandDefinition(
                    sql,
                    new { TenantId = tenantId, UsuarioId = usuarioId },
                    transaction,
                    cancellationToken: cancellationToken);

                var rows = await connection.QueryAsync<Medidor>(command);
                return (IReadOnlyList<Medidor>)rows.ToList();
            });
    }

    public Task<Medidor?> ObtenerPorIdAsync(
        Guid tenantId,
        Guid medidorId,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        var sql = SelectMedidorSql + " AND tenant_id = @TenantId AND id = @MedidorId";

        return WithConnectionAsync(
            transaction,
            async connection =>
            {
                var command = new CommandDefinition(
                    sql,
                    new { TenantId = tenantId, MedidorId = medidorId },
                    transaction,
                    cancellationToken: cancellationToken);

                return await connection.QueryFirstOrDefaultAsync<Medidor>(command);
            });
    }

    public Task<int> ContarPorUsuarioAsync(
        Guid tenantId,
        Guid usuarioId,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT COUNT(1)
            FROM medidores
            WHERE tenant_id = @TenantId
              AND usuario_id = @UsuarioId
              AND eliminado = FALSE
            """;

        return WithConnectionAsync(
            transaction,
            async connection =>
            {
                var command = new CommandDefinition(
                    sql,
                    new { TenantId = tenantId, UsuarioId = usuarioId },
                    transaction,
                    cancellationToken: cancellationToken);

                return await connection.ExecuteScalarAsync<int>(command);
            });
    }

    public Task<int> ContarActivosPorUsuarioAsync(
        Guid tenantId,
        Guid usuarioId,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT COUNT(1)
            FROM medidores
            WHERE tenant_id = @TenantId
              AND usuario_id = @UsuarioId
              AND activo = TRUE
              AND eliminado = FALSE
            """;

        return WithConnectionAsync(
            transaction,
            async connection =>
            {
                var command = new CommandDefinition(
                    sql,
                    new { TenantId = tenantId, UsuarioId = usuarioId },
                    transaction,
                    cancellationToken: cancellationToken);

                return await connection.ExecuteScalarAsync<int>(command);
            });
    }

    public Task<long?> ObtenerNiuUsuarioAsync(
        Guid tenantId,
        Guid usuarioId,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT niu
            FROM usuarios
            WHERE id = @UsuarioId
              AND tenant_id = @TenantId
              AND eliminado = FALSE
            """;

        return WithConnectionAsync(
            transaction,
            async connection =>
            {
                var command = new CommandDefinition(
                    sql,
                    new { TenantId = tenantId, UsuarioId = usuarioId },
                    transaction,
                    cancellationToken: cancellationToken);

                return await connection.ExecuteScalarAsync<long?>(command);
            });
    }

    public Task<long> AsignarNiuCorrelativoSiFaltaAsync(
        Guid tenantId,
        Guid usuarioId,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        const string lockSql = "SELECT pg_advisory_xact_lock(hashtext(@Scope));";
        const string updateSql = """
            UPDATE usuarios
            SET niu = (
                SELECT COALESCE(MAX(u2.niu), 0) + 1
                FROM usuarios u2
                WHERE u2.tenant_id = @TenantId
            )
            WHERE id = @UsuarioId
              AND tenant_id = @TenantId
              AND eliminado = FALSE
              AND niu IS NULL
            RETURNING niu;
            """;

        return WithConnectionAsync(
            transaction,
            async connection =>
            {
                var lockCommand = new CommandDefinition(
                    lockSql,
                    new { Scope = $"usuarios_niu:{tenantId}" },
                    transaction,
                    cancellationToken: cancellationToken);
                await connection.ExecuteAsync(lockCommand);

                var updateCommand = new CommandDefinition(
                    updateSql,
                    new { TenantId = tenantId, UsuarioId = usuarioId },
                    transaction,
                    cancellationToken: cancellationToken);

                var updatedNiu = await connection.ExecuteScalarAsync<long?>(updateCommand);
                if (updatedNiu.HasValue)
                {
                    return updatedNiu.Value;
                }

                var existing = await ObtenerNiuUsuarioAsync(tenantId, usuarioId, transaction, cancellationToken);
                if (!existing.HasValue)
                {
                    throw new InvalidOperationException("No fue posible asignar NIU al usuario.");
                }

                return existing.Value;
            });
    }

    public Task<long> ObtenerSiguienteNumeroMedidorAsync(
        Guid tenantId,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        const string lockSql = "SELECT pg_advisory_xact_lock(hashtext(@Scope));";
        const string sql = """
            SELECT COALESCE(MAX(numero_medidor), 0) + 1
            FROM medidores
            WHERE tenant_id = @TenantId
            """;

        return WithConnectionAsync(
            transaction,
            async connection =>
            {
                var lockCommand = new CommandDefinition(
                    lockSql,
                    new { Scope = $"medidores:{tenantId}" },
                    transaction,
                    cancellationToken: cancellationToken);
                await connection.ExecuteAsync(lockCommand);

                var command = new CommandDefinition(
                    sql,
                    new { TenantId = tenantId },
                    transaction,
                    cancellationToken: cancellationToken);

                return await connection.ExecuteScalarAsync<long>(command);
            });
    }

    public Task<Medidor> CrearAsync(
        Medidor medidor,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO medidores (
                id,
                tenant_id,
                usuario_id,
                numero_medidor,
                activo,
                fecha_creacion,
                fecha_actualizacion,
                eliminado
            ) VALUES (
                @Id,
                @TenantId,
                @UsuarioId,
                @NumeroMedidor,
                @Activo,
                @FechaCreacion,
                @FechaActualizacion,
                @Eliminado
            )
            """;

        return WithConnectionAsync(
            transaction,
            async connection =>
            {
                var command = new CommandDefinition(sql, medidor, transaction, cancellationToken: cancellationToken);
                await connection.ExecuteAsync(command);
                return medidor;
            });
    }

    public Task<Medidor?> ActualizarEstadoAsync(
        Guid tenantId,
        Guid medidorId,
        bool activo,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE medidores
            SET activo = @Activo,
                fecha_actualizacion = @FechaActualizacion
            WHERE id = @MedidorId
              AND tenant_id = @TenantId
              AND eliminado = FALSE
            RETURNING
                id,
                tenant_id as "TenantId",
                usuario_id as "UsuarioId",
                numero_medidor as "NumeroMedidor",
                activo,
                fecha_creacion as "FechaCreacion",
                fecha_actualizacion as "FechaActualizacion",
                eliminado;
            """;

        return WithConnectionAsync(
            transaction,
            async connection =>
            {
                var command = new CommandDefinition(
                    sql,
                    new
                    {
                        TenantId = tenantId,
                        MedidorId = medidorId,
                        Activo = activo,
                        FechaActualizacion = DateTime.UtcNow
                    },
                    transaction,
                    cancellationToken: cancellationToken);

                return await connection.QueryFirstOrDefaultAsync<Medidor>(command);
            });
    }

    public Task<int> DesactivarTodosPorUsuarioAsync(
        Guid tenantId,
        Guid usuarioId,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE medidores
            SET activo = FALSE,
                fecha_actualizacion = @FechaActualizacion
            WHERE tenant_id = @TenantId
              AND usuario_id = @UsuarioId
              AND eliminado = FALSE
              AND activo = TRUE;
            """;

        return WithConnectionAsync(
            transaction,
            async connection =>
            {
                var command = new CommandDefinition(
                    sql,
                    new
                    {
                        TenantId = tenantId,
                        UsuarioId = usuarioId,
                        FechaActualizacion = DateTime.UtcNow
                    },
                    transaction,
                    cancellationToken: cancellationToken);

                return await connection.ExecuteAsync(command);
            });
    }

    public Task<int> SincronizarActivosPorUsuarioAsync(
        Guid tenantId,
        Guid usuarioId,
        int maximoActivos,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            WITH ordenados AS (
                SELECT id,
                       ROW_NUMBER() OVER (ORDER BY numero_medidor ASC) AS rn
                FROM medidores
                WHERE tenant_id = @TenantId
                  AND usuario_id = @UsuarioId
                  AND eliminado = FALSE
            )
            UPDATE medidores m
            SET activo = CASE WHEN o.rn <= @MaximoActivos THEN TRUE ELSE FALSE END,
                fecha_actualizacion = @FechaActualizacion
            FROM ordenados o
            WHERE m.id = o.id;
            """;

        return WithConnectionAsync(
            transaction,
            async connection =>
            {
                var command = new CommandDefinition(
                    sql,
                    new
                    {
                        TenantId = tenantId,
                        UsuarioId = usuarioId,
                        MaximoActivos = maximoActivos < 1 ? 1 : maximoActivos,
                        FechaActualizacion = DateTime.UtcNow
                    },
                    transaction,
                    cancellationToken: cancellationToken);

                return await connection.ExecuteAsync(command);
            });
    }

    public Task<IReadOnlyList<MeterRuleConflictRow>> ObtenerUsuariosConExcesoActivosAsync(
        Guid tenantId,
        int maximoActivosPermitidos,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                u.id AS "UsuarioId",
                u.nombre AS "Nombre",
                u.apellido AS "Apellido",
                u.correo AS "Correo",
                COUNT(m.id)::int AS "TotalActivos",
                ARRAY_AGG(m.numero_medidor ORDER BY m.numero_medidor) AS "NumerosMedidoresActivos"
            FROM usuarios u
            INNER JOIN medidores m
                ON m.usuario_id = u.id
               AND m.tenant_id = u.tenant_id
               AND m.eliminado = FALSE
               AND m.activo = TRUE
            WHERE u.tenant_id = @TenantId
              AND u.eliminado = FALSE
            GROUP BY u.id, u.nombre, u.apellido, u.correo
            HAVING COUNT(m.id) > @MaximoActivosPermitidos
            ORDER BY COUNT(m.id) DESC, u.nombre ASC, u.apellido ASC;
            """;

        return WithConnectionAsync(
            transaction,
            async connection =>
            {
                var command = new CommandDefinition(
                    sql,
                    new { TenantId = tenantId, MaximoActivosPermitidos = maximoActivosPermitidos },
                    transaction,
                    cancellationToken: cancellationToken);

                var rows = await connection.QueryAsync<MeterRuleConflictRow>(command);
                return (IReadOnlyList<MeterRuleConflictRow>)rows.ToList();
            });
    }
}
