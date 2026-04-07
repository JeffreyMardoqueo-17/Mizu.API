using System.Data;
using Dapper;
using Muzu.Api.Core.DTOs;
using Muzu.Api.Core.Interfaces;
using Muzu.Api.Core.Models;

namespace Muzu.Api.Core.Repositories;

public sealed class BillingRepository : RepositoryBase, IBillingRepository
{
    public BillingRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory)
    {
    }

    public Task<IReadOnlyList<Medidor>> ObtenerMedidoresActivosAsync(Guid tenantId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                id,
                tenant_id AS "TenantId",
                usuario_id AS "UsuarioId",
                numero_medidor AS "NumeroMedidor",
                activo,
                fecha_creacion AS "FechaCreacion",
                fecha_actualizacion AS "FechaActualizacion",
                eliminado
            FROM medidores
            WHERE tenant_id = @TenantId
              AND eliminado = FALSE
              AND activo = TRUE
            ORDER BY numero_medidor ASC
            """;

        return WithConnectionAsync(
            transaction,
            async connection =>
            {
                var command = new CommandDefinition(sql, new { TenantId = tenantId }, transaction, cancellationToken: cancellationToken);
                var rows = await connection.QueryAsync<Medidor>(command);
                return (IReadOnlyList<Medidor>)rows.ToList();
            });
    }

    public Task<BillingCycle?> ObtenerCicloAsync(Guid tenantId, string periodCode, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                id,
                tenant_id AS "TenantId",
                period_code AS "PeriodCode",
                period_start AS "PeriodStart",
                period_end AS "PeriodEnd",
                due_date AS "DueDate",
                issue_date AS "IssueDate",
                frequency AS "Frequency",
                status AS "Status",
                created_at AS "CreatedAt",
                closed_at AS "ClosedAt"
            FROM billing_cycles
            WHERE tenant_id = @TenantId
              AND period_code = @PeriodCode
            LIMIT 1
            """;

        return WithConnectionAsync(
            transaction,
            async connection =>
            {
                var command = new CommandDefinition(sql, new { TenantId = tenantId, PeriodCode = periodCode }, transaction, cancellationToken: cancellationToken);
                return await connection.QueryFirstOrDefaultAsync<BillingCycle>(command);
            });
    }

    public Task<BillingCycle?> ObtenerCicloPorIdAsync(Guid tenantId, Guid billingCycleId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                id,
                tenant_id AS "TenantId",
                period_code AS "PeriodCode",
                period_start AS "PeriodStart",
                period_end AS "PeriodEnd",
                due_date AS "DueDate",
                issue_date AS "IssueDate",
                frequency AS "Frequency",
                status AS "Status",
                created_at AS "CreatedAt",
                closed_at AS "ClosedAt"
            FROM billing_cycles
            WHERE tenant_id = @TenantId
              AND id = @BillingCycleId
            LIMIT 1
            """;

        return WithConnectionAsync(
            transaction,
            async connection =>
            {
                var command = new CommandDefinition(sql, new { TenantId = tenantId, BillingCycleId = billingCycleId }, transaction, cancellationToken: cancellationToken);
                return await connection.QueryFirstOrDefaultAsync<BillingCycle>(command);
            });
    }

    public Task<BillingCycle> CrearCicloAsync(BillingCycle cycle, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO billing_cycles (
                id,
                tenant_id,
                period_code,
                period_start,
                period_end,
                due_date,
                issue_date,
                frequency,
                status,
                created_at,
                closed_at
            ) VALUES (
                @Id,
                @TenantId,
                @PeriodCode,
                @PeriodStart,
                @PeriodEnd,
                @DueDate,
                @IssueDate,
                @Frequency,
                @Status,
                @CreatedAt,
                @ClosedAt
            )
            """;

        return WithConnectionAsync(
            transaction,
            async connection =>
            {
                var command = new CommandDefinition(sql, cycle, transaction, cancellationToken: cancellationToken);
                await connection.ExecuteAsync(command);
                return cycle;
            });
    }

    public Task<bool> ActualizarCicloAsync(BillingCycle cycle, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE billing_cycles
            SET
                period_code = @PeriodCode,
                period_start = @PeriodStart,
                period_end = @PeriodEnd,
                due_date = @DueDate,
                issue_date = @IssueDate,
                frequency = @Frequency,
                status = @Status,
                closed_at = @ClosedAt
            WHERE id = @Id
              AND tenant_id = @TenantId
            """;

        return WithConnectionAsync(
            transaction,
            async connection =>
            {
                var command = new CommandDefinition(sql, cycle, transaction, cancellationToken: cancellationToken);
                var affected = await connection.ExecuteAsync(command);
                return affected > 0;
            });
    }

    public Task<MeterReading?> ObtenerLecturaAsync(Guid tenantId, Guid meterId, Guid billingCycleId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                id,
                tenant_id AS "TenantId",
                meter_id AS "MeterId",
                billing_cycle_id AS "BillingCycleId",
                read_at AS "ReadAt",
                previous_reading AS "PreviousReading",
                current_reading AS "CurrentReading",
                consumption_m3 AS "ConsumptionM3",
                source AS "Source",
                notes AS "Notes",
                created_by AS "CreatedBy",
                created_at AS "CreatedAt",
                updated_at AS "UpdatedAt"
            FROM meter_readings
            WHERE tenant_id = @TenantId
              AND meter_id = @MeterId
              AND billing_cycle_id = @BillingCycleId
            LIMIT 1
            """;

        return WithConnectionAsync(
            transaction,
            async connection =>
            {
                var command = new CommandDefinition(sql, new { TenantId = tenantId, MeterId = meterId, BillingCycleId = billingCycleId }, transaction, cancellationToken: cancellationToken);
                return await connection.QueryFirstOrDefaultAsync<MeterReading>(command);
            });
    }

    public Task<MeterReading?> ObtenerUltimaLecturaAsync(Guid tenantId, Guid meterId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                id,
                tenant_id AS "TenantId",
                meter_id AS "MeterId",
                billing_cycle_id AS "BillingCycleId",
                read_at AS "ReadAt",
                previous_reading AS "PreviousReading",
                current_reading AS "CurrentReading",
                consumption_m3 AS "ConsumptionM3",
                source AS "Source",
                notes AS "Notes",
                created_by AS "CreatedBy",
                created_at AS "CreatedAt",
                updated_at AS "UpdatedAt"
            FROM meter_readings
            WHERE tenant_id = @TenantId
              AND meter_id = @MeterId
            ORDER BY read_at DESC, created_at DESC
            LIMIT 1
            """;

        return WithConnectionAsync(
            transaction,
            async connection =>
            {
                var command = new CommandDefinition(sql, new { TenantId = tenantId, MeterId = meterId }, transaction, cancellationToken: cancellationToken);
                return await connection.QueryFirstOrDefaultAsync<MeterReading>(command);
            });
    }

    public Task<MeterReading> CrearLecturaAsync(MeterReading reading, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO meter_readings (
                id,
                tenant_id,
                meter_id,
                billing_cycle_id,
                read_at,
                previous_reading,
                current_reading,
                consumption_m3,
                source,
                notes,
                created_by,
                created_at,
                updated_at
            ) VALUES (
                @Id,
                @TenantId,
                @MeterId,
                @BillingCycleId,
                @ReadAt,
                @PreviousReading,
                @CurrentReading,
                @ConsumptionM3,
                @Source,
                @Notes,
                @CreatedBy,
                @CreatedAt,
                @UpdatedAt
            )
            """;

        return WithConnectionAsync(
            transaction,
            async connection =>
            {
                var command = new CommandDefinition(sql, reading, transaction, cancellationToken: cancellationToken);
                await connection.ExecuteAsync(command);
                return reading;
            });
    }

    public Task<Invoice?> ObtenerFacturaAsync(Guid tenantId, Guid invoiceId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                id,
                tenant_id AS "TenantId",
                meter_id AS "MeterId",
                usuario_id AS "UsuarioId",
                billing_cycle_id AS "BillingCycleId",
                meter_reading_id AS "MeterReadingId",
                invoice_number AS "InvoiceNumber",
                status AS "Status",
                currency AS "Currency",
                subtotal AS "Subtotal",
                previous_balance AS "PreviousBalance",
                late_fee_amount AS "LateFeeAmount",
                operational_penalty_amount AS "OperationalPenaltyAmount",
                adjustments_amount AS "AdjustmentsAmount",
                total_amount AS "TotalAmount",
                paid_amount AS "PaidAmount",
                pending_amount AS "PendingAmount",
                issued_at AS "IssuedAt",
                due_date AS "DueDate",
                paid_at AS "PaidAt",
                cancelled_at AS "CancelledAt",
                reliquidated_from_invoice_id AS "ReliquidatedFromInvoiceId",
                created_at AS "CreatedAt",
                updated_at AS "UpdatedAt"
            FROM invoices
            WHERE tenant_id = @TenantId
              AND id = @InvoiceId
            LIMIT 1
            """;

        return WithConnectionAsync(
            transaction,
            async connection =>
            {
                var command = new CommandDefinition(sql, new { TenantId = tenantId, InvoiceId = invoiceId }, transaction, cancellationToken: cancellationToken);
                return await connection.QueryFirstOrDefaultAsync<Invoice>(command);
            });
    }

    public Task<Invoice?> ObtenerFacturaActivaPorMedidorYCicloAsync(Guid tenantId, Guid meterId, Guid billingCycleId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                id,
                tenant_id AS "TenantId",
                meter_id AS "MeterId",
                usuario_id AS "UsuarioId",
                billing_cycle_id AS "BillingCycleId",
                meter_reading_id AS "MeterReadingId",
                invoice_number AS "InvoiceNumber",
                status AS "Status",
                currency AS "Currency",
                subtotal AS "Subtotal",
                previous_balance AS "PreviousBalance",
                late_fee_amount AS "LateFeeAmount",
                operational_penalty_amount AS "OperationalPenaltyAmount",
                adjustments_amount AS "AdjustmentsAmount",
                total_amount AS "TotalAmount",
                paid_amount AS "PaidAmount",
                pending_amount AS "PendingAmount",
                issued_at AS "IssuedAt",
                due_date AS "DueDate",
                paid_at AS "PaidAt",
                cancelled_at AS "CancelledAt",
                reliquidated_from_invoice_id AS "ReliquidatedFromInvoiceId",
                created_at AS "CreatedAt",
                updated_at AS "UpdatedAt"
            FROM invoices
            WHERE tenant_id = @TenantId
              AND meter_id = @MeterId
              AND billing_cycle_id = @BillingCycleId
              AND status <> @CancelledStatus
            ORDER BY created_at DESC
            LIMIT 1
            """;

        return WithConnectionAsync(
            transaction,
            async connection =>
            {
                var command = new CommandDefinition(sql, new { TenantId = tenantId, MeterId = meterId, BillingCycleId = billingCycleId, CancelledStatus = BillingStatuses.Anulado }, transaction, cancellationToken: cancellationToken);
                return await connection.QueryFirstOrDefaultAsync<Invoice>(command);
            });
    }

    public Task<Invoice?> ObtenerFacturaAnteriorAsync(Guid tenantId, Guid meterId, Guid billingCycleId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                i.id,
                i.tenant_id AS "TenantId",
                i.meter_id AS "MeterId",
                i.usuario_id AS "UsuarioId",
                i.billing_cycle_id AS "BillingCycleId",
                i.meter_reading_id AS "MeterReadingId",
                i.invoice_number AS "InvoiceNumber",
                i.status AS "Status",
                i.currency AS "Currency",
                i.subtotal AS "Subtotal",
                i.previous_balance AS "PreviousBalance",
                i.late_fee_amount AS "LateFeeAmount",
                i.operational_penalty_amount AS "OperationalPenaltyAmount",
                i.adjustments_amount AS "AdjustmentsAmount",
                i.total_amount AS "TotalAmount",
                i.paid_amount AS "PaidAmount",
                i.pending_amount AS "PendingAmount",
                i.issued_at AS "IssuedAt",
                i.due_date AS "DueDate",
                i.paid_at AS "PaidAt",
                i.cancelled_at AS "CancelledAt",
                i.reliquidated_from_invoice_id AS "ReliquidatedFromInvoiceId",
                i.created_at AS "CreatedAt",
                i.updated_at AS "UpdatedAt"
            FROM invoices i
            INNER JOIN billing_cycles bc ON bc.id = i.billing_cycle_id
            WHERE i.tenant_id = @TenantId
              AND i.meter_id = @MeterId
              AND i.status <> @CancelledStatus
              AND bc.period_code < (
                  SELECT period_code
                  FROM billing_cycles
                  WHERE id = @BillingCycleId
                    AND tenant_id = @TenantId
              )
            ORDER BY bc.period_code DESC, i.created_at DESC
            LIMIT 1
            """;

        return WithConnectionAsync(
            transaction,
            async connection =>
            {
                var command = new CommandDefinition(sql, new { TenantId = tenantId, MeterId = meterId, BillingCycleId = billingCycleId, CancelledStatus = BillingStatuses.Anulado }, transaction, cancellationToken: cancellationToken);
                return await connection.QueryFirstOrDefaultAsync<Invoice>(command);
            });
    }

    public Task<Invoice> CrearFacturaAsync(Invoice invoice, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO invoices (
                id,
                tenant_id,
                meter_id,
                usuario_id,
                billing_cycle_id,
                meter_reading_id,
                invoice_number,
                status,
                currency,
                subtotal,
                previous_balance,
                late_fee_amount,
                operational_penalty_amount,
                adjustments_amount,
                total_amount,
                paid_amount,
                pending_amount,
                issued_at,
                due_date,
                paid_at,
                cancelled_at,
                reliquidated_from_invoice_id,
                created_at,
                updated_at
            ) VALUES (
                @Id,
                @TenantId,
                @MeterId,
                @UsuarioId,
                @BillingCycleId,
                @MeterReadingId,
                @InvoiceNumber,
                @Status,
                @Currency,
                @Subtotal,
                @PreviousBalance,
                @LateFeeAmount,
                @OperationalPenaltyAmount,
                @AdjustmentsAmount,
                @TotalAmount,
                @PaidAmount,
                @PendingAmount,
                @IssuedAt,
                @DueDate,
                @PaidAt,
                @CancelledAt,
                @ReliquidatedFromInvoiceId,
                @CreatedAt,
                @UpdatedAt
            )
            """;

        return WithConnectionAsync(
            transaction,
            async connection =>
            {
                var command = new CommandDefinition(sql, invoice, transaction, cancellationToken: cancellationToken);
                await connection.ExecuteAsync(command);
                return invoice;
            });
    }

    public Task<bool> ActualizarFacturaAsync(Invoice invoice, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE invoices
            SET
                invoice_number = @InvoiceNumber,
                status = @Status,
                currency = @Currency,
                subtotal = @Subtotal,
                previous_balance = @PreviousBalance,
                late_fee_amount = @LateFeeAmount,
                operational_penalty_amount = @OperationalPenaltyAmount,
                adjustments_amount = @AdjustmentsAmount,
                total_amount = @TotalAmount,
                paid_amount = @PaidAmount,
                pending_amount = @PendingAmount,
                issued_at = @IssuedAt,
                due_date = @DueDate,
                paid_at = @PaidAt,
                cancelled_at = @CancelledAt,
                reliquidated_from_invoice_id = @ReliquidatedFromInvoiceId,
                updated_at = @UpdatedAt
            WHERE id = @Id
              AND tenant_id = @TenantId
            """;

        return WithConnectionAsync(
            transaction,
            async connection =>
            {
                var command = new CommandDefinition(sql, invoice, transaction, cancellationToken: cancellationToken);
                var affected = await connection.ExecuteAsync(command);
                return affected > 0;
            });
    }

    public Task<InvoiceLine> CrearLineaAsync(InvoiceLine line, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO invoice_lines (
                id,
                tenant_id,
                invoice_id,
                line_type,
                description,
                quantity,
                unit_price,
                amount,
                reference_table,
                reference_id,
                metadata,
                created_at
            ) VALUES (
                @Id,
                @TenantId,
                @InvoiceId,
                @LineType,
                @Description,
                @Quantity,
                @UnitPrice,
                @Amount,
                @ReferenceTable,
                @ReferenceId,
                @Metadata,
                @CreatedAt
            )
            """;

        return WithConnectionAsync(
            transaction,
            async connection =>
            {
                var command = new CommandDefinition(sql, line, transaction, cancellationToken: cancellationToken);
                await connection.ExecuteAsync(command);
                return line;
            });
    }

    public Task<IReadOnlyList<InvoiceLine>> ObtenerLineasAsync(Guid tenantId, Guid invoiceId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                id,
                tenant_id AS "TenantId",
                invoice_id AS "InvoiceId",
                line_type AS "LineType",
                description AS "Description",
                quantity AS "Quantity",
                unit_price AS "UnitPrice",
                amount AS "Amount",
                reference_table AS "ReferenceTable",
                reference_id AS "ReferenceId",
                metadata AS "Metadata",
                created_at AS "CreatedAt"
            FROM invoice_lines
            WHERE tenant_id = @TenantId
              AND invoice_id = @InvoiceId
            ORDER BY created_at ASC
            """;

        return WithConnectionAsync(
            transaction,
            async connection =>
            {
                var command = new CommandDefinition(sql, new { TenantId = tenantId, InvoiceId = invoiceId }, transaction, cancellationToken: cancellationToken);
                var rows = await connection.QueryAsync<InvoiceLine>(command);
                return (IReadOnlyList<InvoiceLine>)rows.ToList();
            });
    }

    public Task<Payment> RegistrarPagoAsync(Payment payment, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO payments (
                id,
                tenant_id,
                invoice_id,
                meter_id,
                usuario_id,
                payment_date,
                amount,
                method,
                reference,
                status,
                notes,
                created_by,
                created_at
            ) VALUES (
                @Id,
                @TenantId,
                @InvoiceId,
                @MeterId,
                @UsuarioId,
                @PaymentDate,
                @Amount,
                @Method,
                @Reference,
                @Status,
                @Notes,
                @CreatedBy,
                @CreatedAt
            )
            """;

        return WithConnectionAsync(
            transaction,
            async connection =>
            {
                var command = new CommandDefinition(sql, payment, transaction, cancellationToken: cancellationToken);
                await connection.ExecuteAsync(command);
                return payment;
            });
    }

    public Task<bool> ExisteMoraGeneradaAsync(Guid tenantId, Guid sourceInvoiceId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT EXISTS (
                SELECT 1
                FROM late_fee_history
                WHERE tenant_id = @TenantId
                  AND source_invoice_id = @SourceInvoiceId
            )
            """;

        return WithConnectionAsync(
            transaction,
            async connection =>
            {
                var command = new CommandDefinition(sql, new { TenantId = tenantId, SourceInvoiceId = sourceInvoiceId }, transaction, cancellationToken: cancellationToken);
                return await connection.ExecuteScalarAsync<bool>(command);
            });
    }

    public Task<LateFeeHistory> RegistrarMoraAsync(LateFeeHistory history, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO late_fee_history (
                id,
                tenant_id,
                meter_id,
                source_invoice_id,
                target_invoice_id,
                amount,
                generated_at,
                rule_snapshot
            ) VALUES (
                @Id,
                @TenantId,
                @MeterId,
                @SourceInvoiceId,
                @TargetInvoiceId,
                @Amount,
                @GeneratedAt,
                @RuleSnapshot
            )
            """;

        return WithConnectionAsync(
            transaction,
            async connection =>
            {
                var command = new CommandDefinition(sql, history, transaction, cancellationToken: cancellationToken);
                await connection.ExecuteAsync(command);
                return history;
            });
    }

    public Task<IReadOnlyList<OperationalPenalty>> ObtenerMultasPendientesAsync(Guid tenantId, DateOnly limitDate, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                id,
                tenant_id AS "TenantId",
                usuario_id AS "UsuarioId",
                source_type AS "SourceType",
                source_date AS "SourceDate",
                amount AS "Amount",
                status AS "Status",
                assignment_strategy AS "AssignmentStrategy",
                assigned_meter_id AS "AssignedMeterId",
                assigned_invoice_id AS "AssignedInvoiceId",
                assigned_at AS "AssignedAt",
                notes AS "Notes",
                created_by AS "CreatedBy",
                created_at AS "CreatedAt"
            FROM operational_penalties
            WHERE tenant_id = @TenantId
              AND status = 'pendiente'
              AND source_date <= @LimitDate
            ORDER BY source_date ASC, created_at ASC
            """;

        return WithConnectionAsync(
            transaction,
            async connection =>
            {
                var command = new CommandDefinition(sql, new { TenantId = tenantId, LimitDate = limitDate }, transaction, cancellationToken: cancellationToken);
                var rows = await connection.QueryAsync<OperationalPenalty>(command);
                return (IReadOnlyList<OperationalPenalty>)rows.ToList();
            });
    }

    public Task<bool> AsignarMultaAsync(Guid tenantId, Guid penaltyId, Guid meterId, Guid invoiceId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE operational_penalties
            SET
                status = 'asignada',
                assigned_meter_id = @MeterId,
                assigned_invoice_id = @InvoiceId,
                assigned_at = NOW()
            WHERE tenant_id = @TenantId
              AND id = @PenaltyId
              AND status = 'pendiente'
            """;

        return WithConnectionAsync(
            transaction,
            async connection =>
            {
                var command = new CommandDefinition(sql, new { TenantId = tenantId, PenaltyId = penaltyId, MeterId = meterId, InvoiceId = invoiceId }, transaction, cancellationToken: cancellationToken);
                var affected = await connection.ExecuteAsync(command);
                return affected > 0;
            });
    }

    public Task<CarriedBalance> RegistrarSaldoArrastradoAsync(CarriedBalance carriedBalance, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO carried_balances (
                id,
                tenant_id,
                meter_id,
                source_invoice_id,
                target_invoice_id,
                amount,
                created_at
            ) VALUES (
                @Id,
                @TenantId,
                @MeterId,
                @SourceInvoiceId,
                @TargetInvoiceId,
                @Amount,
                @CreatedAt
            )
            """;

        return WithConnectionAsync(
            transaction,
            async connection =>
            {
                var command = new CommandDefinition(sql, carriedBalance, transaction, cancellationToken: cancellationToken);
                await connection.ExecuteAsync(command);
                return carriedBalance;
            });
    }

    public Task<InvoiceAdjustment> RegistrarAjusteAsync(InvoiceAdjustment adjustment, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO invoice_adjustments (
                id,
                tenant_id,
                meter_id,
                invoice_id,
                adjustment_type,
                amount,
                reason,
                source_reading_id,
                source_invoice_id,
                linked_invoice_id,
                effective_cycle_id,
                status,
                created_by,
                created_at
            ) VALUES (
                @Id,
                @TenantId,
                @MeterId,
                @InvoiceId,
                @AdjustmentType,
                @Amount,
                @Reason,
                @SourceReadingId,
                @SourceInvoiceId,
                @LinkedInvoiceId,
                @EffectiveCycleId,
                @Status,
                @CreatedBy,
                @CreatedAt
            )
            """;

        return WithConnectionAsync(
            transaction,
            async connection =>
            {
                var command = new CommandDefinition(sql, adjustment, transaction, cancellationToken: cancellationToken);
                await connection.ExecuteAsync(command);
                return adjustment;
            });
    }

    public Task<IReadOnlyList<InvoiceSummaryDto>> ObtenerHistorialPorMedidorAsync(Guid tenantId, Guid meterId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                id AS "InvoiceId",
                meter_id AS "MeterId",
                usuario_id AS "UsuarioId",
                invoice_number AS "InvoiceNumber",
                status AS "Status",
                due_date AS "DueDate",
                total_amount AS "TotalAmount",
                paid_amount AS "PaidAmount",
                pending_amount AS "PendingAmount",
                issued_at AS "IssuedAt"
            FROM invoices
            WHERE tenant_id = @TenantId
              AND meter_id = @MeterId
            ORDER BY COALESCE(issued_at, created_at) DESC
            LIMIT 24
            """;

        return WithConnectionAsync(
            transaction,
            async connection =>
            {
                var command = new CommandDefinition(sql, new { TenantId = tenantId, MeterId = meterId }, transaction, cancellationToken: cancellationToken);
                var rows = await connection.QueryAsync<InvoiceSummaryDto>(command);
                return (IReadOnlyList<InvoiceSummaryDto>)rows.ToList();
            });
    }

    public Task<IReadOnlyList<UserDebtSummaryMeterDto>> ObtenerResumenPorUsuarioAsync(Guid tenantId, Guid usuarioId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                i.meter_id AS "MeterId",
                m.numero_medidor AS "NumeroMedidor",
                COALESCE(SUM(CASE WHEN i.pending_amount > 0 THEN i.pending_amount ELSE 0 END), 0) AS "PendingAmount",
                COALESCE(SUM(CASE WHEN i.pending_amount > 0 AND i.due_date < CURRENT_DATE THEN 1 ELSE 0 END), 0) AS "OverdueInvoices",
                MIN(CASE WHEN i.pending_amount > 0 THEN i.due_date ELSE NULL END) AS "OldestDueDate"
            FROM invoices i
            INNER JOIN medidores m ON m.id = i.meter_id
            WHERE i.tenant_id = @TenantId
              AND m.tenant_id = @TenantId
              AND m.usuario_id = @UsuarioId
              AND m.eliminado = FALSE
              AND i.status <> @CancelledStatus
            GROUP BY i.meter_id, m.numero_medidor
            ORDER BY m.numero_medidor ASC
            """;

        return WithConnectionAsync(
            transaction,
            async connection =>
            {
                var command = new CommandDefinition(sql, new { TenantId = tenantId, UsuarioId = usuarioId, CancelledStatus = BillingStatuses.Anulado }, transaction, cancellationToken: cancellationToken);
                var rows = await connection.QueryAsync<UserDebtSummaryMeterDto>(command);
                return (IReadOnlyList<UserDebtSummaryMeterDto>)rows.ToList();
            });
    }
}
