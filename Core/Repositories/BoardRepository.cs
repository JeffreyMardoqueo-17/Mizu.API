using System.Data;
using Dapper;
using Muzu.Api.Core.Interfaces;
using Muzu.Api.Core.Models;

namespace Muzu.Api.Core.Repositories;

public sealed class BoardRepository : RepositoryBase, IBoardRepository
{
    public BoardRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory)
    {
    }

    public Task<IReadOnlyList<Board>> ListByTenantAsync(Guid tenantId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = """
                           SELECT id,
                                  tenant_id,
                                  nombre,
                                  slug,
                                  fecha_inicio,
                                  fecha_fin,
                                  estado,
                                  administrador_responsable_id,
                                  fecha_creacion,
                                  fecha_actualizacion,
                                  fecha_transicion
                           FROM directiva
                           WHERE tenant_id = @tenantId
                           ORDER BY fecha_creacion DESC
                           """;

        return WithConnectionAsync<IReadOnlyList<Board>>(
            transaction,
            async connection =>
            {
                var command = new CommandDefinition(sql, new { tenantId }, transaction, cancellationToken: cancellationToken);
                var items = await connection.QueryAsync<Board>(command);
                return items.ToList();
            });
    }

    public Task<Board?> GetByIdAsync(Guid boardId, Guid tenantId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = """
                           SELECT id,
                                  tenant_id,
                                  nombre,
                                  slug,
                                  fecha_inicio,
                                  fecha_fin,
                                  estado,
                                  administrador_responsable_id,
                                  fecha_creacion,
                                  fecha_actualizacion,
                                  fecha_transicion
                           FROM directiva
                           WHERE id = @boardId
                             AND tenant_id = @tenantId
                           """;

        return WithConnectionAsync(
            transaction,
            async connection =>
            {
                var command = new CommandDefinition(sql, new { boardId, tenantId }, transaction, cancellationToken: cancellationToken);
                return await connection.QueryFirstOrDefaultAsync<Board>(command);
            });
    }

    public Task<bool> ExistsSlugAsync(Guid tenantId, string slug, Guid? excludingBoardId = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = """
                           SELECT 1
                           FROM directiva
                           WHERE tenant_id = @tenantId
                             AND slug = @slug
                             AND (@excludingBoardId IS NULL OR id <> @excludingBoardId)
                           LIMIT 1
                           """;

        return WithConnectionAsync(
            transaction,
            async connection =>
            {
                var command = new CommandDefinition(sql, new { tenantId, slug, excludingBoardId }, transaction, cancellationToken: cancellationToken);
                var exists = await connection.QueryFirstOrDefaultAsync<int?>(command);
                return exists.HasValue;
            });
    }

    public Task<Board> CreateAsync(Board board, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = """
                           INSERT INTO directiva (id, tenant_id, nombre, slug, fecha_inicio, fecha_fin, periodo_inicio, periodo_fin, estado, administrador_responsable_id, fecha_creacion, fecha_actualizacion)
                           VALUES (@Id, @TenantId, @Nombre, @Slug, @FechaInicio, @FechaFin, @FechaInicio, @FechaFin, @Estado, @AdministradorResponsableId, @FechaCreacion, @FechaActualizacion)
                           """;

        return WithConnectionAsync(
            transaction,
            async connection =>
            {
                var parameters = new
                {
                    board.Id,
                    board.TenantId,
                    board.Nombre,
                    board.Slug,
                    board.FechaInicio,
                    board.FechaFin,
                    board.Estado,
                    board.AdministradorResponsableId,
                    board.FechaCreacion,
                    board.FechaActualizacion
                };
                var command = new CommandDefinition(sql, parameters, transaction, cancellationToken: cancellationToken);
                await connection.ExecuteAsync(command);
                return board;
            });
    }

    public Task<Board> UpdateAsync(Board board, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = """
                           UPDATE directiva
                           SET nombre = @Nombre,
                               slug = @Slug,
                               fecha_inicio = @FechaInicio,
                               fecha_fin = @FechaFin,
                               periodo_inicio = @FechaInicio,
                               periodo_fin = @FechaFin,
                               administrador_responsable_id = @AdministradorResponsableId,
                               fecha_actualizacion = @FechaActualizacion
                           WHERE id = @Id
                           """;

        return WithConnectionAsync(
            transaction,
            async connection =>
            {
                var parameters = new
                {
                    board.Id,
                    board.Nombre,
                    board.Slug,
                    board.FechaInicio,
                    board.FechaFin,
                    board.AdministradorResponsableId,
                    board.FechaActualizacion
                };
                var command = new CommandDefinition(sql, parameters, transaction, cancellationToken: cancellationToken);
                await connection.ExecuteAsync(command);
                return board;
            });
    }

    public Task<IReadOnlyList<BoardMember>> GetMembersAsync(Guid boardId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = """
                           SELECT dm.id,
                                  dm.directiva_id AS board_id,
                                  dm.usuario_id,
                                  dm.rol_id,
                                  COALESCE(dm.cargo, r.nombre) AS cargo,
                                  dm.fecha_creacion
                           FROM directiva_miembros dm
                           INNER JOIN roles r ON r.id = dm.rol_id
                           WHERE dm.directiva_id = @boardId
                           ORDER BY dm.fecha_creacion ASC
                           """;

        return WithConnectionAsync<IReadOnlyList<BoardMember>>(
            transaction,
            async connection =>
            {
                var command = new CommandDefinition(sql, new { boardId }, transaction, cancellationToken: cancellationToken);
                var items = await connection.QueryAsync<BoardMember>(command);
                return items.ToList();
            });
    }

    public Task AddMemberAsync(BoardMember member, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = """
                           INSERT INTO directiva_miembros (id, directiva_id, usuario_id, rol_id, cargo, fecha_creacion)
                           VALUES (@Id, @BoardId, @UsuarioId, @RolId, @Cargo, @FechaCreacion)
                           ON CONFLICT (directiva_id, usuario_id)
                           DO UPDATE SET
                               rol_id = EXCLUDED.rol_id,
                               cargo = EXCLUDED.cargo
                           """;

        return WithConnectionAsync(
            transaction,
            async connection =>
            {
                var command = new CommandDefinition(sql, member, transaction, cancellationToken: cancellationToken);
                await connection.ExecuteAsync(command);
                return 0;
            });
    }

    public Task<Board?> GetActiveBoardAsync(Guid tenantId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = """
                           SELECT id,
                                  tenant_id,
                                  nombre,
                                  slug,
                                  fecha_inicio,
                                  fecha_fin,
                                  estado,
                                  administrador_responsable_id,
                                  fecha_creacion,
                                  fecha_actualizacion,
                                  fecha_transicion
                           FROM directiva
                           WHERE tenant_id = @tenantId
                             AND estado = 'Activa'
                           LIMIT 1
                           """;

        return WithConnectionAsync(
            transaction,
            async connection =>
            {
                var command = new CommandDefinition(sql, new { tenantId }, transaction, cancellationToken: cancellationToken);
                return await connection.QueryFirstOrDefaultAsync<Board>(command);
            });
    }

    public Task SetBoardStateAsync(Guid boardId, string estado, DateTime? fechaTransicion, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = """
                           UPDATE directiva
                           SET estado = @estado,
                               fecha_actualizacion = NOW(),
                               fecha_transicion = @fechaTransicion
                           WHERE id = @boardId
                           """;

        return WithConnectionAsync(
            transaction,
            async connection =>
            {
                var command = new CommandDefinition(sql, new { boardId, estado, fechaTransicion }, transaction, cancellationToken: cancellationToken);
                await connection.ExecuteAsync(command);
                return 0;
            });
    }

    public Task<IReadOnlyList<BoardHistoryItem>> GetHistoryAsync(Guid boardId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = """
                           SELECT id,
                                  board_id,
                                  evento,
                                  descripcion,
                                  actor_usuario_id,
                                  fecha_creacion
                           FROM directiva_historial
                           WHERE board_id = @boardId
                           ORDER BY fecha_creacion DESC
                           """;

        return WithConnectionAsync<IReadOnlyList<BoardHistoryItem>>(
            transaction,
            async connection =>
            {
                var command = new CommandDefinition(sql, new { boardId }, transaction, cancellationToken: cancellationToken);
                var items = await connection.QueryAsync<BoardHistoryItem>(command);
                return items.ToList();
            });
    }

    public Task AddHistoryAsync(BoardHistoryItem historyItem, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = """
                           INSERT INTO directiva_historial (id, board_id, evento, descripcion, actor_usuario_id, fecha_creacion)
                           VALUES (@Id, @BoardId, @Evento, @Descripcion, @ActorUsuarioId, @FechaCreacion)
                           """;

        return WithConnectionAsync(
            transaction,
            async connection =>
            {
                var command = new CommandDefinition(sql, historyItem, transaction, cancellationToken: cancellationToken);
                await connection.ExecuteAsync(command);
                return 0;
            });
    }
}
