using System.Data;
using Dapper;
using Muzu.Api.Core.DTOs;
using Muzu.Api.Core.Interfaces;

namespace Muzu.Api.Core.Repositories;

public sealed class DashboardRepository : RepositoryBase, IDashboardRepository
{
    private sealed class TopDebtorRow
    {
        public Guid UsuarioId { get; init; }
        public string NombreCompleto { get; init; } = string.Empty;
        public decimal PendingAmount { get; init; }
        public int OverdueInvoices { get; init; }
        public DateTime? OldestDueDate { get; init; }
    }

    private sealed class DebtorRow
    {
        public Guid UsuarioId { get; init; }
        public string NombreCompleto { get; init; } = string.Empty;
        public string DUI { get; init; } = string.Empty;
        public string? Email { get; init; }
        public int TotalInvoices { get; init; }
        public int OverdueInvoices { get; init; }
        public decimal TotalPendingAmount { get; init; }
        public decimal OverdueAmount { get; init; }
        public DateTime? OldestDueDate { get; init; }
        public DateTime FechaRegistro { get; init; }
    }

    public DashboardRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory)
    {
    }

    public Task<UsersStatsDto> GetUsersStatsAsync(Guid tenantId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT 
                COUNT(*)::int AS "TotalUsers",
                COUNT(CASE WHEN u.activo = TRUE THEN 1 END)::int AS "ActiveUsers",
                COUNT(CASE WHEN u.activo = FALSE THEN 1 END)::int AS "InactiveUsers",
                COUNT(CASE WHEN u.fecha_creacion >= DATE_TRUNC('month', CURRENT_DATE) THEN 1 END)::int AS "NewUsersThisMonth",
                COUNT(DISTINCT CASE WHEN i.pending_amount > 0 AND LOWER(i.status) <> 'anulado' THEN u.id END)::int AS "UsersWithDebt"
            FROM usuarios u
            LEFT JOIN medidores m ON m.usuario_id = u.id AND m.eliminado = FALSE
            LEFT JOIN invoices i ON i.meter_id = m.id AND LOWER(i.status) <> 'anulado'
            WHERE u.tenant_id = @TenantId
              AND u.eliminado = FALSE
            """;

        return WithConnectionAsync(
            transaction,
            async connection =>
            {
                var command = new CommandDefinition(sql, new { TenantId = tenantId }, transaction, cancellationToken: cancellationToken);
                return await connection.QueryFirstAsync<UsersStatsDto>(command);
            });
    }

    public Task<int> GetTotalUsersAsync(Guid tenantId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT COUNT(*)
            FROM usuarios
            WHERE tenant_id = @TenantId
              AND eliminado = FALSE
            """;

        return WithConnectionAsync(
            transaction,
            async connection =>
            {
                var command = new CommandDefinition(sql, new { TenantId = tenantId }, transaction, cancellationToken: cancellationToken);
                return await connection.ExecuteScalarAsync<int>(command);
            });
    }

    public Task<int> GetActiveUsersAsync(Guid tenantId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT COUNT(*)
            FROM usuarios
            WHERE tenant_id = @TenantId
              AND eliminado = FALSE
              AND activo = TRUE
            """;

        return WithConnectionAsync(
            transaction,
            async connection =>
            {
                var command = new CommandDefinition(sql, new { TenantId = tenantId }, transaction, cancellationToken: cancellationToken);
                return await connection.ExecuteScalarAsync<int>(command);
            });
    }

    public Task<int> GetNewUsersThisMonthAsync(Guid tenantId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT COUNT(*)
            FROM usuarios
            WHERE tenant_id = @TenantId
              AND eliminado = FALSE
              AND fecha_creacion >= DATE_TRUNC('month', CURRENT_DATE)
            """;

        return WithConnectionAsync(
            transaction,
            async connection =>
            {
                var command = new CommandDefinition(sql, new { TenantId = tenantId }, transaction, cancellationToken: cancellationToken);
                return await connection.ExecuteScalarAsync<int>(command);
            });
    }

    public Task<int> GetUsersWithDebtAsync(Guid tenantId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT COUNT(DISTINCT u.id)
            FROM usuarios u
            INNER JOIN medidores m ON m.usuario_id = u.id AND m.eliminado = FALSE
            INNER JOIN invoices i ON i.meter_id = m.id AND LOWER(i.status) <> 'anulado' AND i.pending_amount > 0
            WHERE u.tenant_id = @TenantId
              AND u.eliminado = FALSE
            """;

        return WithConnectionAsync(
            transaction,
            async connection =>
            {
                var command = new CommandDefinition(sql, new { TenantId = tenantId }, transaction, cancellationToken: cancellationToken);
                return await connection.ExecuteScalarAsync<int>(command);
            });
    }

    public Task<int> GetTotalMetersAsync(Guid tenantId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT COUNT(*)
            FROM medidores
            WHERE tenant_id = @TenantId
              AND eliminado = FALSE
            """;

        return WithConnectionAsync(
            transaction,
            async connection =>
            {
                var command = new CommandDefinition(sql, new { TenantId = tenantId }, transaction, cancellationToken: cancellationToken);
                return await connection.ExecuteScalarAsync<int>(command);
            });
    }

    public Task<int> GetActiveMetersAsync(Guid tenantId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT COUNT(*)
            FROM medidores
            WHERE tenant_id = @TenantId
              AND eliminado = FALSE
              AND activo = TRUE
            """;

        return WithConnectionAsync(
            transaction,
            async connection =>
            {
                var command = new CommandDefinition(sql, new { TenantId = tenantId }, transaction, cancellationToken: cancellationToken);
                return await connection.ExecuteScalarAsync<int>(command);
            });
    }

    public Task<int> GetMetersWithConflictsAsync(Guid tenantId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = """
            WITH limite AS (
                SELECT CASE
                           WHEN COALESCE(tc.permitir_multiples_contadores, FALSE)
                               THEN GREATEST(COALESCE(tc.maximo_contadores_por_usuario, 1), 1)
                           ELSE 1
                       END AS maximo
                FROM tenant_configs tc
                WHERE tc.tenant_id = @TenantId
                UNION ALL
                SELECT 1
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM tenant_configs tc2
                    WHERE tc2.tenant_id = @TenantId
                )
                LIMIT 1
            ),
            usuarios_conflicto AS (
                SELECT m.usuario_id
                FROM medidores m
                CROSS JOIN limite l
                WHERE m.tenant_id = @TenantId
                  AND m.eliminado = FALSE
                  AND m.activo = TRUE
                GROUP BY m.usuario_id, l.maximo
                HAVING COUNT(m.id) > l.maximo
            )
            SELECT COUNT(m.id)::int
            FROM medidores m
            INNER JOIN usuarios_conflicto uc ON uc.usuario_id = m.usuario_id
            WHERE m.tenant_id = @TenantId
              AND m.eliminado = FALSE
              AND m.activo = TRUE
            """;

        return WithConnectionAsync(
            transaction,
            async connection =>
            {
                var command = new CommandDefinition(sql, new { TenantId = tenantId }, transaction, cancellationToken: cancellationToken);
                return await connection.ExecuteScalarAsync<int>(command);
            });
    }

    public Task<NextBillingCycleDto?> GetNextBillingCycleAsync(Guid tenantId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT 
                bc.id AS "Id",
                bc.period_code AS "PeriodCode",
                bc.period_start AS "PeriodStart",
                bc.period_end AS "PeriodEnd",
                bc.due_date AS "DueDate",
                bc.issue_date AS "IssueDate",
                bc.status AS "Status",
                COALESCE((SELECT COUNT(*) FROM invoices WHERE billing_cycle_id = bc.id), 0)::int AS "TotalInvoices",
                COALESCE((SELECT COUNT(*) FROM invoices WHERE billing_cycle_id = bc.id AND pending_amount > 0), 0)::int AS "PendingInvoices",
                COALESCE((SELECT COUNT(*) FROM invoices WHERE billing_cycle_id = bc.id AND pending_amount = 0 AND LOWER(status) <> 'anulado'), 0)::int AS "PaidInvoices",
                COALESCE((SELECT SUM(pending_amount) FROM invoices WHERE billing_cycle_id = bc.id), 0) AS "TotalPendingAmount",
                (bc.due_date - CURRENT_DATE) AS "DaysUntilDue"
            FROM billing_cycles bc
            WHERE bc.tenant_id = @TenantId
            ORDER BY bc.issue_date DESC
            LIMIT 1
            """;

        return WithConnectionAsync(
            transaction,
            async connection =>
            {
                var command = new CommandDefinition(sql, new { TenantId = tenantId }, transaction, cancellationToken: cancellationToken);
                return await connection.QueryFirstOrDefaultAsync<NextBillingCycleDto>(command);
            });
    }

    public Task<NextMeetingDto?> GetNextMeetingAsync(Guid tenantId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT 
                r.id AS "Id",
                COALESCE(r.titulo, 'Reunión General') AS "Titulo",
                                (r.fecha_reunion::timestamp + r.hora_inicio) AS "FechaHora",
                r.estado AS "Estado",
                                COALESCE((SELECT COUNT(*) FROM reunion_asistencias WHERE reunion_id = r.id), 0)::int AS "TotalMembers",
                                COALESCE((SELECT COUNT(*) FROM reunion_asistencias WHERE reunion_id = r.id AND asistio = TRUE), 0)::int AS "ConfirmedAttendees",
                                (COALESCE((SELECT COUNT(*) FROM reunion_asistencias WHERE reunion_id = r.id), 0) - 
                                COALESCE((SELECT COUNT(*) FROM reunion_asistencias WHERE reunion_id = r.id AND asistio = TRUE), 0))::int AS "PendingAttendees",
                                NULL::text AS "Lugar",
                                (r.fecha_reunion - CURRENT_DATE) AS "DaysUntilMeeting"
            FROM reuniones r
            WHERE r.tenant_id = @TenantId
                            AND (r.fecha_reunion::timestamp + r.hora_inicio) > NOW()
                            AND r.estado IN ('Programada', 'EnCurso')
                        ORDER BY r.fecha_reunion ASC, r.hora_inicio ASC
            LIMIT 1
            """;

        return WithConnectionAsync(
            transaction,
            async connection =>
            {
                var command = new CommandDefinition(sql, new { TenantId = tenantId }, transaction, cancellationToken: cancellationToken);
                return await connection.QueryFirstOrDefaultAsync<NextMeetingDto>(command);
            });
    }

    public Task<CurrentBoardDto?> GetCurrentBoardAsync(Guid tenantId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT 
                d.id AS "Id",
                d.nombre AS "Nombre",
                d.fecha_inicio AS "FechaInicio",
                d.fecha_fin AS "FechaFin",
                d.estado AS "Estado",
                                (SELECT COUNT(*)::int FROM directiva_miembros WHERE directiva_id = d.id) AS "TotalMembers",
                (d.fecha_fin - CURRENT_DATE) AS "DaysUntilExpiration"
            FROM directiva d
            WHERE d.tenant_id = @TenantId
              AND d.estado = 'Activa'
            LIMIT 1
            """;

        return WithConnectionAsync(
            transaction,
            async connection =>
            {
                var command = new CommandDefinition(sql, new { TenantId = tenantId }, transaction, cancellationToken: cancellationToken);
                return await connection.QueryFirstOrDefaultAsync<CurrentBoardDto>(command);
            });
    }

    public Task<PenaltiesSummaryDto> GetPenaltiesSummaryAsync(Guid tenantId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        return WithConnectionAsync(
            transaction,
            async connection =>
            {
                var total = await GetTotalPenaltiesAsync(tenantId, transaction, cancellationToken);
                var pending = await GetPendingPenaltiesAsync(tenantId, transaction, cancellationToken);
                var assigned = await GetAssignedPenaltiesAsync(tenantId, transaction, cancellationToken);
                var amount = await GetTotalPendingPenaltiesAmountAsync(tenantId, transaction, cancellationToken);
                var byType = await GetPenaltiesByTypeAsync(tenantId, transaction, cancellationToken);

                return new PenaltiesSummaryDto(total, pending, assigned, amount, byType);
            });
    }

    public Task<int> GetTotalPenaltiesAsync(Guid tenantId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT COUNT(*)
            FROM operational_penalties
            WHERE tenant_id = @TenantId
            """;

        return WithConnectionAsync(
            transaction,
            async connection =>
            {
                var command = new CommandDefinition(sql, new { TenantId = tenantId }, transaction, cancellationToken: cancellationToken);
                return await connection.ExecuteScalarAsync<int>(command);
            });
    }

    public Task<int> GetPendingPenaltiesAsync(Guid tenantId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT COUNT(*)
            FROM operational_penalties
            WHERE tenant_id = @TenantId
              AND status = 'pendiente'
            """;

        return WithConnectionAsync(
            transaction,
            async connection =>
            {
                var command = new CommandDefinition(sql, new { TenantId = tenantId }, transaction, cancellationToken: cancellationToken);
                return await connection.ExecuteScalarAsync<int>(command);
            });
    }

    public Task<int> GetAssignedPenaltiesAsync(Guid tenantId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT COUNT(*)
            FROM operational_penalties
            WHERE tenant_id = @TenantId
              AND status = 'asignada'
            """;

        return WithConnectionAsync(
            transaction,
            async connection =>
            {
                var command = new CommandDefinition(sql, new { TenantId = tenantId }, transaction, cancellationToken: cancellationToken);
                return await connection.ExecuteScalarAsync<int>(command);
            });
    }

    public Task<decimal> GetTotalPendingPenaltiesAmountAsync(Guid tenantId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT COALESCE(SUM(amount), 0)
            FROM operational_penalties
            WHERE tenant_id = @TenantId
              AND status = 'pendiente'
            """;

        return WithConnectionAsync(
            transaction,
            async connection =>
            {
                var command = new CommandDefinition(sql, new { TenantId = tenantId }, transaction, cancellationToken: cancellationToken);
                return await connection.ExecuteScalarAsync<decimal>(command);
            });
    }

    public Task<IReadOnlyList<PenaltyByTypeDto>> GetPenaltiesByTypeAsync(Guid tenantId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT 
                source_type AS "Type",
                CASE 
                    WHEN source_type = 'inasistencia_reunion' THEN 'Inasistencia a Reunión'
                    WHEN source_type = 'falta_trabajo' THEN 'Falta de Trabajo'
                    WHEN source_type = 'incumplimiento_acuerdo' THEN 'Incumplimiento de Acuerdo'
                    ELSE source_type
                END AS "Description",
                COUNT(*)::int AS "Count",
                COALESCE(SUM(amount), 0) AS "TotalAmount"
            FROM operational_penalties
            WHERE tenant_id = @TenantId
            GROUP BY source_type
            ORDER BY "Count" DESC
            """;

        return WithConnectionAsync(
            transaction,
            async connection =>
            {
                var command = new CommandDefinition(sql, new { TenantId = tenantId }, transaction, cancellationToken: cancellationToken);
                var rows = await connection.QueryAsync<PenaltyByTypeDto>(command);
                return (IReadOnlyList<PenaltyByTypeDto>)rows.ToList();
            });
    }

    public Task<DebtSummaryDto> GetDebtSummaryAsync(Guid tenantId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        return WithConnectionAsync(
            transaction,
            async connection =>
            {
                var totalDebtors = await GetTotalDebtorsAsync(tenantId, transaction, cancellationToken);
                var usersOverdue = await GetUsersOverdueAsync(tenantId, transaction, cancellationToken);
                var totalPending = await GetTotalPendingDebtAsync(tenantId, transaction, cancellationToken);
                var totalOverdue = await GetTotalOverdueDebtAsync(tenantId, transaction, cancellationToken);
                var topDebtors = await GetTopDebtorsAsync(tenantId, 10, transaction, cancellationToken);

                var avgDebt = totalDebtors > 0 ? totalPending / totalDebtors : 0;

                return new DebtSummaryDto(totalDebtors, usersOverdue, totalPending, totalOverdue, avgDebt, topDebtors);
            });
    }

    public Task<int> GetTotalDebtorsAsync(Guid tenantId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT COUNT(DISTINCT u.id)
            FROM usuarios u
            INNER JOIN medidores m ON m.usuario_id = u.id AND m.eliminado = FALSE
            INNER JOIN invoices i ON i.meter_id = m.id AND LOWER(i.status) <> 'anulado' AND i.pending_amount > 0
            WHERE u.tenant_id = @TenantId
              AND u.eliminado = FALSE
            """;

        return WithConnectionAsync(
            transaction,
            async connection =>
            {
                var command = new CommandDefinition(sql, new { TenantId = tenantId }, transaction, cancellationToken: cancellationToken);
                return await connection.ExecuteScalarAsync<int>(command);
            });
    }

    public Task<int> GetUsersOverdueAsync(Guid tenantId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT COUNT(DISTINCT u.id)
            FROM usuarios u
            INNER JOIN medidores m ON m.usuario_id = u.id AND m.eliminado = FALSE
            INNER JOIN invoices i ON i.meter_id = m.id AND LOWER(i.status) <> 'anulado' AND i.pending_amount > 0 AND i.due_date < CURRENT_DATE
            WHERE u.tenant_id = @TenantId
              AND u.eliminado = FALSE
            """;

        return WithConnectionAsync(
            transaction,
            async connection =>
            {
                var command = new CommandDefinition(sql, new { TenantId = tenantId }, transaction, cancellationToken: cancellationToken);
                return await connection.ExecuteScalarAsync<int>(command);
            });
    }

    public Task<decimal> GetTotalPendingDebtAsync(Guid tenantId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT COALESCE(SUM(i.pending_amount), 0)
            FROM invoices i
            INNER JOIN medidores m ON m.id = i.meter_id AND m.eliminado = FALSE
            WHERE i.tenant_id = @TenantId
              AND LOWER(i.status) <> 'anulado'
              AND i.pending_amount > 0
            """;

        return WithConnectionAsync(
            transaction,
            async connection =>
            {
                var command = new CommandDefinition(sql, new { TenantId = tenantId }, transaction, cancellationToken: cancellationToken);
                return await connection.ExecuteScalarAsync<decimal>(command);
            });
    }

    public Task<decimal> GetTotalOverdueDebtAsync(Guid tenantId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT COALESCE(SUM(i.pending_amount), 0)
            FROM invoices i
            INNER JOIN medidores m ON m.id = i.meter_id AND m.eliminado = FALSE
            WHERE i.tenant_id = @TenantId
              AND LOWER(i.status) <> 'anulado'
              AND i.pending_amount > 0
              AND i.due_date < CURRENT_DATE
            """;

        return WithConnectionAsync(
            transaction,
            async connection =>
            {
                var command = new CommandDefinition(sql, new { TenantId = tenantId }, transaction, cancellationToken: cancellationToken);
                return await connection.ExecuteScalarAsync<decimal>(command);
            });
    }

    public Task<IReadOnlyList<TopDebtorDto>> GetTopDebtorsAsync(Guid tenantId, int limit = 10, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT 
                u.id AS "UsuarioId",
                COALESCE(NULLIF(TRIM(CONCAT(u.nombre, ' ', u.apellido)), ''), u.correo) AS "NombreCompleto",
                COALESCE(SUM(i.pending_amount), 0) AS "PendingAmount",
                COUNT(CASE WHEN i.due_date < CURRENT_DATE THEN 1 END)::int AS "OverdueInvoices",
                MIN(CASE WHEN i.pending_amount > 0 THEN i.due_date ELSE NULL END) AS "OldestDueDate"
            FROM usuarios u
            INNER JOIN medidores m ON m.usuario_id = u.id AND m.eliminado = FALSE
            INNER JOIN invoices i ON i.meter_id = m.id AND LOWER(i.status) <> 'anulado' AND i.pending_amount > 0
            WHERE u.tenant_id = @TenantId
              AND u.eliminado = FALSE
            GROUP BY u.id, u.nombre, u.apellido, u.correo
            ORDER BY "PendingAmount" DESC
            LIMIT @Limit
            """;

        return WithConnectionAsync(
            transaction,
            async connection =>
            {
                var command = new CommandDefinition(sql, new { TenantId = tenantId, Limit = limit }, transaction, cancellationToken: cancellationToken);
                var rows = await connection.QueryAsync<TopDebtorRow>(command);
                return (IReadOnlyList<TopDebtorDto>)rows
                    .Select(row => new TopDebtorDto(
                        row.UsuarioId,
                        row.NombreCompleto,
                        row.PendingAmount,
                        row.OverdueInvoices,
                        row.OldestDueDate.HasValue ? DateOnly.FromDateTime(row.OldestDueDate.Value) : null))
                    .ToList();
            });
    }

    public Task<DebtorsListDto> GetDebtorsListAsync(Guid tenantId, int page, int pageSize, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string countSql = """
            SELECT COUNT(DISTINCT u.id)
            FROM usuarios u
            INNER JOIN medidores m ON m.usuario_id = u.id AND m.eliminado = FALSE
            INNER JOIN invoices i ON i.meter_id = m.id AND LOWER(i.status) <> 'anulado' AND i.pending_amount > 0
            WHERE u.tenant_id = @TenantId
              AND u.eliminado = FALSE
            """;

        const string dataSql = """
            SELECT 
                u.id AS "UsuarioId",
                COALESCE(NULLIF(TRIM(CONCAT(u.nombre, ' ', u.apellido)), ''), u.correo) AS "NombreCompleto",
                COALESCE(u.dui, '') AS "DUI",
                u.correo AS "Email",
                COUNT(i.id)::int AS "TotalInvoices",
                COUNT(CASE WHEN i.due_date < CURRENT_DATE THEN 1 END)::int AS "OverdueInvoices",
                COALESCE(SUM(i.pending_amount), 0) AS "TotalPendingAmount",
                COALESCE(SUM(CASE WHEN i.due_date < CURRENT_DATE THEN i.pending_amount ELSE 0 END), 0) AS "OverdueAmount",
                MIN(CASE WHEN i.pending_amount > 0 THEN i.due_date ELSE NULL END) AS "OldestDueDate",
                u.fecha_creacion AS "FechaRegistro"
            FROM usuarios u
            INNER JOIN medidores m ON m.usuario_id = u.id AND m.eliminado = FALSE
            INNER JOIN invoices i ON i.meter_id = m.id AND LOWER(i.status) <> 'anulado' AND i.pending_amount > 0
            WHERE u.tenant_id = @TenantId
              AND u.eliminado = FALSE
            GROUP BY u.id, u.nombre, u.apellido, u.correo, u.dui, u.fecha_creacion
            ORDER BY "TotalPendingAmount" DESC
            OFFSET @Offset ROWS
            FETCH NEXT @PageSize ROWS ONLY
            """;

        return WithConnectionAsync(
            transaction,
            async connection =>
            {
                var countCommand = new CommandDefinition(countSql, new { TenantId = tenantId }, transaction, cancellationToken: cancellationToken);
                var totalCount = await connection.ExecuteScalarAsync<int>(countCommand);

                var offset = (page - 1) * pageSize;
                var dataCommand = new CommandDefinition(dataSql, new { TenantId = tenantId, Offset = offset, PageSize = pageSize }, transaction, cancellationToken: cancellationToken);
                var rows = await connection.QueryAsync<DebtorRow>(dataCommand);
                var items = rows
                    .Select(row => new DebtorDto(
                        row.UsuarioId,
                        row.NombreCompleto,
                        row.DUI,
                        row.Email,
                        row.TotalInvoices,
                        row.OverdueInvoices,
                        row.TotalPendingAmount,
                        row.OverdueAmount,
                        row.OldestDueDate.HasValue ? DateOnly.FromDateTime(row.OldestDueDate.Value) : null,
                        row.FechaRegistro))
                    .ToList();

                return new DebtorsListDto(items, totalCount, page, pageSize);
            });
    }
}
