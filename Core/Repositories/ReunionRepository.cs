using System.Data;
using Dapper;
using Muzu.Api.Core.Interfaces;
using Muzu.Api.Core.Models;

namespace Muzu.Api.Core.Repositories;

public sealed class ReunionRepository : RepositoryBase, IReunionRepository
{
    private sealed class ReunionRow
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public DateTime FechaReunion { get; set; }
        public TimeSpan HoraInicio { get; set; }
        public TimeSpan? HoraFin { get; set; }
        public string Estado { get; set; } = string.Empty;
        public string PuntosTratarJson { get; set; } = "[]";
        public string? AcuerdosJson { get; set; }
        public string? NotasSecretaria { get; set; }
        public Guid? CreadoPorUsuarioId { get; set; }
        public DateTime? IniciadaAt { get; set; }
        public DateTime? FinalizadaAt { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaActualizacion { get; set; }
    }

    public ReunionRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory)
    {
    }

    public Task<IReadOnlyList<Reunion>> ListarPorTenantAsync(Guid tenantId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                id,
                tenant_id AS "TenantId",
                titulo AS "Titulo",
                fecha_reunion AS "FechaReunion",
                hora_inicio AS "HoraInicio",
                hora_fin AS "HoraFin",
                estado AS "Estado",
                puntos_tratar_json AS "PuntosTratarJson",
                acuerdos_json AS "AcuerdosJson",
                notas_secretaria AS "NotasSecretaria",
                creado_por_usuario_id AS "CreadoPorUsuarioId",
                iniciada_at AS "IniciadaAt",
                finalizada_at AS "FinalizadaAt",
                fecha_creacion AS "FechaCreacion",
                fecha_actualizacion AS "FechaActualizacion"
            FROM reuniones
            WHERE tenant_id = @TenantId
            ORDER BY fecha_reunion DESC, hora_inicio DESC, fecha_creacion DESC;
            """;

        return WithConnectionAsync(transaction, async connection =>
        {
            var command = new CommandDefinition(sql, new { TenantId = tenantId }, transaction, cancellationToken: cancellationToken);
            var rows = await connection.QueryAsync<ReunionRow>(command);
            return (IReadOnlyList<Reunion>)rows.Select(MapRow).ToList();
        });
    }

    public Task<Reunion?> ObtenerPorIdAsync(Guid tenantId, Guid reunionId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                id,
                tenant_id AS "TenantId",
                titulo AS "Titulo",
                fecha_reunion AS "FechaReunion",
                hora_inicio AS "HoraInicio",
                hora_fin AS "HoraFin",
                estado AS "Estado",
                puntos_tratar_json AS "PuntosTratarJson",
                acuerdos_json AS "AcuerdosJson",
                notas_secretaria AS "NotasSecretaria",
                creado_por_usuario_id AS "CreadoPorUsuarioId",
                iniciada_at AS "IniciadaAt",
                finalizada_at AS "FinalizadaAt",
                fecha_creacion AS "FechaCreacion",
                fecha_actualizacion AS "FechaActualizacion"
            FROM reuniones
            WHERE tenant_id = @TenantId
              AND id = @ReunionId
            LIMIT 1;
            """;

        return WithConnectionAsync(transaction, async connection =>
        {
            var command = new CommandDefinition(sql, new { TenantId = tenantId, ReunionId = reunionId }, transaction, cancellationToken: cancellationToken);
            var row = await connection.QueryFirstOrDefaultAsync<ReunionRow>(command);
            return row is null ? null : MapRow(row);
        });
    }

    private static Reunion MapRow(ReunionRow row)
    {
        return new Reunion
        {
            Id = row.Id,
            TenantId = row.TenantId,
            Titulo = row.Titulo,
            FechaReunion = DateOnly.FromDateTime(row.FechaReunion),
            HoraInicio = TimeOnly.FromTimeSpan(row.HoraInicio),
            HoraFin = row.HoraFin.HasValue ? TimeOnly.FromTimeSpan(row.HoraFin.Value) : null,
            Estado = row.Estado,
            PuntosTratarJson = row.PuntosTratarJson,
            AcuerdosJson = row.AcuerdosJson,
            NotasSecretaria = row.NotasSecretaria,
            CreadoPorUsuarioId = row.CreadoPorUsuarioId,
            IniciadaAt = row.IniciadaAt,
            FinalizadaAt = row.FinalizadaAt,
            FechaCreacion = row.FechaCreacion,
            FechaActualizacion = row.FechaActualizacion
        };
    }

    public Task<Reunion> CrearAsync(Reunion reunion, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO reuniones (
                id,
                tenant_id,
                titulo,
                fecha_reunion,
                hora_inicio,
                hora_fin,
                estado,
                puntos_tratar_json,
                acuerdos_json,
                notas_secretaria,
                creado_por_usuario_id,
                iniciada_at,
                finalizada_at,
                fecha_creacion,
                fecha_actualizacion
            ) VALUES (
                @Id,
                @TenantId,
                @Titulo,
                @FechaReunion,
                @HoraInicio,
                @HoraFin,
                @Estado,
                CAST(@PuntosTratarJson AS jsonb),
                CAST(@AcuerdosJson AS jsonb),
                @NotasSecretaria,
                @CreadoPorUsuarioId,
                @IniciadaAt,
                @FinalizadaAt,
                @FechaCreacion,
                @FechaActualizacion
            );
            """;

        var parameters = new
        {
            reunion.Id,
            reunion.TenantId,
            reunion.Titulo,
            FechaReunion = reunion.FechaReunion.ToDateTime(TimeOnly.MinValue),
            HoraInicio = reunion.HoraInicio.ToTimeSpan(),
            HoraFin = reunion.HoraFin?.ToTimeSpan(),
            reunion.Estado,
            reunion.PuntosTratarJson,
            reunion.AcuerdosJson,
            reunion.NotasSecretaria,
            reunion.CreadoPorUsuarioId,
            reunion.IniciadaAt,
            reunion.FinalizadaAt,
            reunion.FechaCreacion,
            reunion.FechaActualizacion
        };

        return WithConnectionAsync(transaction, async connection =>
        {
            var command = new CommandDefinition(sql, parameters, transaction, cancellationToken: cancellationToken);
            await connection.ExecuteAsync(command);
            return reunion;
        });
    }

    public Task<bool> ActualizarAsync(Reunion reunion, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE reuniones
            SET titulo = @Titulo,
                fecha_reunion = @FechaReunion,
                hora_inicio = @HoraInicio,
                hora_fin = @HoraFin,
                puntos_tratar_json = CAST(@PuntosTratarJson AS jsonb),
                fecha_actualizacion = @FechaActualizacion
            WHERE id = @Id
              AND tenant_id = @TenantId
              AND estado <> 'Finalizada';
            """;

        var parameters = new
        {
            reunion.Id,
            reunion.TenantId,
            reunion.Titulo,
            FechaReunion = reunion.FechaReunion.ToDateTime(TimeOnly.MinValue),
            HoraInicio = reunion.HoraInicio.ToTimeSpan(),
            HoraFin = reunion.HoraFin?.ToTimeSpan(),
            reunion.PuntosTratarJson,
            reunion.FechaActualizacion
        };

        return WithConnectionAsync(transaction, async connection =>
        {
            var command = new CommandDefinition(sql, parameters, transaction, cancellationToken: cancellationToken);
            var affected = await connection.ExecuteAsync(command);
            return affected > 0;
        });
    }

    public Task<bool> CambiarEstadoAsync(Guid reunionId, Guid tenantId, string estado, DateTime? fechaInicio, DateTime? fechaFin, TimeOnly? horaFin = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE reuniones
            SET estado = @Estado,
                iniciada_at = COALESCE(@FechaInicio, iniciada_at),
                finalizada_at = COALESCE(@FechaFin, finalizada_at),
                hora_fin = CASE WHEN @Estado = 'Finalizada' THEN COALESCE(hora_fin, @HoraFin) ELSE hora_fin END,
                fecha_actualizacion = NOW()
            WHERE id = @ReunionId
              AND tenant_id = @TenantId;
            """;

        return WithConnectionAsync(transaction, async connection =>
        {
            var command = new CommandDefinition(sql, new
            {
                ReunionId = reunionId,
                TenantId = tenantId,
                Estado = estado,
                FechaInicio = fechaInicio,
                FechaFin = fechaFin,
                HoraFin = horaFin?.ToTimeSpan()
            }, transaction, cancellationToken: cancellationToken);

            var affected = await connection.ExecuteAsync(command);
            return affected > 0;
        });
    }

    public Task<bool> ActualizarAcuerdosAsync(Guid reunionId, Guid tenantId, string acuerdosJson, string? notasSecretaria, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE reuniones
            SET acuerdos_json = CAST(@AcuerdosJson AS jsonb),
                notas_secretaria = @NotasSecretaria,
                fecha_actualizacion = NOW()
            WHERE id = @ReunionId
              AND tenant_id = @TenantId
              AND estado <> 'Finalizada';
            """;

        return WithConnectionAsync(transaction, async connection =>
        {
            var command = new CommandDefinition(sql, new { ReunionId = reunionId, TenantId = tenantId, AcuerdosJson = acuerdosJson, NotasSecretaria = notasSecretaria }, transaction, cancellationToken: cancellationToken);
            var affected = await connection.ExecuteAsync(command);
            return affected > 0;
        });
    }

    public Task<IReadOnlyList<ReunionAsistencia>> ObtenerAsistenciasAsync(Guid reunionId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                id,
                reunion_id AS "ReunionId",
                usuario_id AS "UsuarioId",
                asistio AS "Asistio",
                observacion AS "Observacion",
                marcado_por_usuario_id AS "MarcadoPorUsuarioId",
                fecha_marcado AS "FechaMarcado",
                fecha_creacion AS "FechaCreacion",
                fecha_actualizacion AS "FechaActualizacion"
            FROM reunion_asistencias
            WHERE reunion_id = @ReunionId
            ORDER BY fecha_creacion ASC;
            """;

        return WithConnectionAsync(transaction, async connection =>
        {
            var command = new CommandDefinition(sql, new { ReunionId = reunionId }, transaction, cancellationToken: cancellationToken);
            var rows = await connection.QueryAsync<ReunionAsistencia>(command);
            return (IReadOnlyList<ReunionAsistencia>)rows.ToList();
        });
    }

    public Task<ReunionAsistencia> CrearAsistenciaAsync(ReunionAsistencia asistencia, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO reunion_asistencias (
                id,
                reunion_id,
                usuario_id,
                asistio,
                observacion,
                marcado_por_usuario_id,
                fecha_marcado,
                fecha_creacion,
                fecha_actualizacion
            ) VALUES (
                @Id,
                @ReunionId,
                @UsuarioId,
                @Asistio,
                @Observacion,
                @MarcadoPorUsuarioId,
                @FechaMarcado,
                @FechaCreacion,
                @FechaActualizacion
            )
            ON CONFLICT (reunion_id, usuario_id)
            DO UPDATE SET
                asistio = EXCLUDED.asistio,
                observacion = EXCLUDED.observacion,
                marcado_por_usuario_id = EXCLUDED.marcado_por_usuario_id,
                fecha_marcado = EXCLUDED.fecha_marcado,
                fecha_actualizacion = EXCLUDED.fecha_actualizacion;
            """;

        return WithConnectionAsync(transaction, async connection =>
        {
            var command = new CommandDefinition(sql, asistencia, transaction, cancellationToken: cancellationToken);
            await connection.ExecuteAsync(command);
            return asistencia;
        });
    }

    public Task<bool> ActualizarAsistenciaAsync(Guid reunionId, Guid usuarioId, bool asistio, string? observacion, Guid? marcadoPorUsuarioId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE reunion_asistencias
            SET asistio = @Asistio,
                observacion = @Observacion,
                marcado_por_usuario_id = @MarcadoPorUsuarioId,
                fecha_marcado = NOW(),
                fecha_actualizacion = NOW()
            WHERE reunion_id = @ReunionId
              AND usuario_id = @UsuarioId;
            """;

        return WithConnectionAsync(transaction, async connection =>
        {
            var command = new CommandDefinition(sql, new { ReunionId = reunionId, UsuarioId = usuarioId, Asistio = asistio, Observacion = observacion, MarcadoPorUsuarioId = marcadoPorUsuarioId }, transaction, cancellationToken: cancellationToken);
            var affected = await connection.ExecuteAsync(command);
            return affected > 0;
        });
    }

    public Task<IReadOnlyList<ReunionHistorial>> ObtenerHistorialAsync(Guid reunionId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                id,
                reunion_id AS "ReunionId",
                evento AS "Evento",
                descripcion AS "Descripcion",
                actor_usuario_id AS "ActorUsuarioId",
                fecha_creacion AS "FechaCreacion"
            FROM reunion_historial
            WHERE reunion_id = @ReunionId
            ORDER BY fecha_creacion DESC;
            """;

        return WithConnectionAsync(transaction, async connection =>
        {
            var command = new CommandDefinition(sql, new { ReunionId = reunionId }, transaction, cancellationToken: cancellationToken);
            var rows = await connection.QueryAsync<ReunionHistorial>(command);
            return (IReadOnlyList<ReunionHistorial>)rows.ToList();
        });
    }

    public Task<ReunionHistorial> AgregarHistorialAsync(ReunionHistorial historial, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO reunion_historial (
                id,
                reunion_id,
                evento,
                descripcion,
                actor_usuario_id,
                fecha_creacion
            ) VALUES (
                @Id,
                @ReunionId,
                @Evento,
                @Descripcion,
                @ActorUsuarioId,
                @FechaCreacion
            );
            """;

        return WithConnectionAsync(transaction, async connection =>
        {
            var command = new CommandDefinition(sql, historial, transaction, cancellationToken: cancellationToken);
            await connection.ExecuteAsync(command);
            return historial;
        });
    }
}