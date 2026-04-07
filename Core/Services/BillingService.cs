using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Muzu.Api.Core.DTOs;
using Muzu.Api.Core.Interfaces;
using Muzu.Api.Core.Interfaces.Service;
using Muzu.Api.Core.Models;
using Muzu.Api.Core.Rules;

namespace Muzu.Api.Core.Services;

public sealed class BillingService : IBillingService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IBillingRepository _billingRepository;
    private readonly IMedidorRepository _medidorRepository;
    private readonly ITenantConfigRepository _tenantConfigRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<BillingService> _logger;

    public BillingService(
        IBillingRepository billingRepository,
        IMedidorRepository medidorRepository,
        ITenantConfigRepository tenantConfigRepository,
        IUnitOfWork unitOfWork,
        ILogger<BillingService> logger)
    {
        _billingRepository = billingRepository;
        _medidorRepository = medidorRepository;
        _tenantConfigRepository = tenantConfigRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<GenerateBillingCycleResponseDto> GenerarCicloAsync(
        Guid actorUsuarioId,
        Guid actorTenantId,
        CreateBillingCycleRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.PeriodCode))
        {
            throw new ArgumentException("El periodo es obligatorio.", nameof(request.PeriodCode));
        }

        return await _unitOfWork.ExecuteInTransactionAsync(async transaction =>
        {
            var cycle = await _billingRepository.ObtenerCicloAsync(actorTenantId, request.PeriodCode, transaction, cancellationToken);
            if (cycle is null)
            {
                cycle = new BillingCycle
                {
                    TenantId = actorTenantId,
                    PeriodCode = request.PeriodCode,
                    PeriodStart = request.PeriodStart,
                    PeriodEnd = request.PeriodEnd,
                    DueDate = request.DueDate,
                    IssueDate = request.IssueDate,
                    Frequency = request.Frequency,
                    Status = BillingStatuses.Abierto,
                    CreatedAt = DateTime.UtcNow,
                    ClosedAt = null
                };

                await _billingRepository.CrearCicloAsync(cycle, transaction, cancellationToken);
            }

            var config = await ObtenerConfigAsync(actorTenantId, transaction, cancellationToken);
            var meters = await _billingRepository.ObtenerMedidoresActivosAsync(actorTenantId, transaction, cancellationToken);
            var invoicesGenerated = 0;
            decimal totalBilledAmount = 0;

            foreach (var meter in meters)
            {
                var reading = await _billingRepository.ObtenerLecturaAsync(actorTenantId, meter.Id, cycle.Id, transaction, cancellationToken);
                if (reading is null)
                {
                    continue;
                }

                var existingInvoice = await _billingRepository.ObtenerFacturaActivaPorMedidorYCicloAsync(actorTenantId, meter.Id, cycle.Id, transaction, cancellationToken);
                if (existingInvoice is not null)
                {
                    continue;
                }

                var previousInvoice = await _billingRepository.ObtenerFacturaAnteriorAsync(actorTenantId, meter.Id, cycle.Id, transaction, cancellationToken);
                var previousBalance = previousInvoice?.PendingAmount ?? 0;
                var shouldApplyLateFee = previousInvoice is not null
                    && previousInvoice.PendingAmount > 0
                    && previousInvoice.DueDate < cycle.IssueDate;

                var lateFeeAmount = shouldApplyLateFee && previousInvoice is not null && !await _billingRepository.ExisteMoraGeneradaAsync(actorTenantId, previousInvoice.Id, transaction, cancellationToken)
                    ? config.MultaRetraso
                    : 0;

                var tariffLines = BuildTariffLines(reading.ConsumptionM3, config);
                var operationalPenaltyLines = await BuildOperationalPenaltyLinesAsync(actorTenantId, meter, cycle, transaction, cancellationToken);
                var subtotal = tariffLines.Sum(line => line.Amount);
                var operationalPenaltyAmount = operationalPenaltyLines.Sum(line => line.Amount);
                var adjustmentsAmount = 0m;
                var totalAmount = subtotal + previousBalance + lateFeeAmount + operationalPenaltyAmount + adjustmentsAmount;

                var invoice = new Invoice
                {
                    TenantId = actorTenantId,
                    MeterId = meter.Id,
                    UsuarioId = meter.UsuarioId,
                    BillingCycleId = cycle.Id,
                    MeterReadingId = reading.Id,
                    InvoiceNumber = BuildInvoiceNumber(cycle, meter),
                    Status = BillingStatuses.Emitido,
                    Currency = config.Moneda,
                    Subtotal = subtotal,
                    PreviousBalance = previousBalance,
                    LateFeeAmount = lateFeeAmount,
                    OperationalPenaltyAmount = operationalPenaltyAmount,
                    AdjustmentsAmount = adjustmentsAmount,
                    TotalAmount = totalAmount,
                    PaidAmount = 0,
                    PendingAmount = totalAmount,
                    IssuedAt = DateTime.UtcNow,
                    DueDate = cycle.DueDate,
                    PaidAt = null,
                    CancelledAt = null,
                    ReliquidatedFromInvoiceId = null,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = null
                };

                await _billingRepository.CrearFacturaAsync(invoice, transaction, cancellationToken);

                foreach (var line in tariffLines)
                {
                    await _billingRepository.CrearLineaAsync(
                        new InvoiceLine
                        {
                            TenantId = actorTenantId,
                            InvoiceId = invoice.Id,
                            LineType = line.LineType,
                            Description = line.Description,
                            Quantity = line.Quantity,
                            UnitPrice = line.UnitPrice,
                            Amount = line.Amount,
                            ReferenceTable = line.ReferenceTable,
                            ReferenceId = line.ReferenceId,
                            Metadata = line.Metadata,
                            CreatedAt = DateTime.UtcNow
                        },
                        transaction,
                        cancellationToken);
                }

                if (previousBalance > 0)
                {
                    await _billingRepository.RegistrarSaldoArrastradoAsync(
                        new CarriedBalance
                        {
                            TenantId = actorTenantId,
                            MeterId = meter.Id,
                            SourceInvoiceId = previousInvoice!.Id,
                            TargetInvoiceId = invoice.Id,
                            Amount = previousBalance,
                            CreatedAt = DateTime.UtcNow
                        },
                        transaction,
                        cancellationToken);

                    await _billingRepository.CrearLineaAsync(
                        new InvoiceLine
                        {
                            TenantId = actorTenantId,
                            InvoiceId = invoice.Id,
                            LineType = "saldo_arrastrado",
                            Description = $"Saldo arrastrado de factura {previousInvoice.InvoiceNumber}",
                            Quantity = 1,
                            UnitPrice = previousBalance,
                            Amount = previousBalance,
                            ReferenceTable = "invoices",
                            ReferenceId = previousInvoice.Id,
                            Metadata = null,
                            CreatedAt = DateTime.UtcNow
                        },
                        transaction,
                        cancellationToken);
                }

                foreach (var penaltyLine in operationalPenaltyLines)
                {
                    await _billingRepository.AsignarMultaAsync(actorTenantId, penaltyLine.ReferenceId, meter.Id, invoice.Id, transaction, cancellationToken);

                    await _billingRepository.CrearLineaAsync(
                        new InvoiceLine
                        {
                            TenantId = actorTenantId,
                            InvoiceId = invoice.Id,
                            LineType = "multa_operativa",
                            Description = penaltyLine.Description,
                            Quantity = 1,
                            UnitPrice = penaltyLine.Amount,
                            Amount = penaltyLine.Amount,
                            ReferenceTable = "operational_penalties",
                            ReferenceId = penaltyLine.ReferenceId,
                            Metadata = penaltyLine.Metadata,
                            CreatedAt = DateTime.UtcNow
                        },
                        transaction,
                        cancellationToken);
                }

                if (lateFeeAmount > 0 && previousInvoice is not null)
                {
                    await _billingRepository.RegistrarMoraAsync(
                        new LateFeeHistory
                        {
                            TenantId = actorTenantId,
                            MeterId = meter.Id,
                            SourceInvoiceId = previousInvoice.Id,
                            TargetInvoiceId = invoice.Id,
                            Amount = lateFeeAmount,
                            GeneratedAt = DateTime.UtcNow,
                            RuleSnapshot = JsonSerializer.Serialize(new
                            {
                                config.MultaRetraso,
                                cycle.PeriodCode,
                                previousInvoice.InvoiceNumber
                            })
                        },
                        transaction,
                        cancellationToken);

                    await _billingRepository.CrearLineaAsync(
                        new InvoiceLine
                        {
                            TenantId = actorTenantId,
                            InvoiceId = invoice.Id,
                            LineType = "mora",
                            Description = $"Mora por factura {previousInvoice.InvoiceNumber}",
                            Quantity = 1,
                            UnitPrice = lateFeeAmount,
                            Amount = lateFeeAmount,
                            ReferenceTable = "late_fee_history",
                            ReferenceId = previousInvoice.Id,
                            Metadata = null,
                            CreatedAt = DateTime.UtcNow
                        },
                        transaction,
                        cancellationToken);
                }

                invoicesGenerated++;
                totalBilledAmount += totalAmount;
            }

            cycle.Status = BillingStatuses.Emitido;
            cycle.ClosedAt = DateTime.UtcNow;
            await _billingRepository.ActualizarCicloAsync(cycle, transaction, cancellationToken);

            _logger.LogInformation(
                "Ciclo {PeriodCode} generado para tenant {TenantId}. Medidores procesados: {Meters}, facturas: {Invoices}",
                cycle.PeriodCode,
                actorTenantId,
                meters.Count,
                invoicesGenerated);

            return new GenerateBillingCycleResponseDto(
                cycle.PeriodCode,
                meters.Count,
                invoicesGenerated,
                totalBilledAmount,
                "Ciclo generado correctamente.");
        }, cancellationToken);
    }

    public async Task<MeterReadingDto> RegistrarLecturaAsync(
        Guid actorUsuarioId,
        Guid actorTenantId,
        MeterReadingCreateRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var meter = await _medidorRepository.ObtenerPorIdAsync(actorTenantId, request.MeterId, null, cancellationToken);
        if (meter is null)
        {
            throw new InvalidOperationException("El medidor no existe o no pertenece al tenant actual.");
        }

        if (!meter.Activo || meter.Eliminado)
        {
            throw new InvalidOperationException("El medidor no está activo.");
        }

        var cycle = request.BillingCycleId.HasValue
            ? await _billingRepository.ObtenerCicloPorIdAsync(actorTenantId, request.BillingCycleId.Value, null, cancellationToken)
            : null;

        if (cycle is null)
        {
            var periodCode = BuildPeriodCode(request.BillingDate);
            cycle = await _billingRepository.ObtenerCicloAsync(actorTenantId, periodCode, null, cancellationToken);
            if (cycle is null)
            {
                cycle = new BillingCycle
                {
                    TenantId = actorTenantId,
                    PeriodCode = periodCode,
                    PeriodStart = new DateOnly(request.BillingDate.Year, request.BillingDate.Month, 1),
                    PeriodEnd = new DateOnly(request.BillingDate.Year, request.BillingDate.Month, DateTime.DaysInMonth(request.BillingDate.Year, request.BillingDate.Month)),
                    DueDate = new DateOnly(request.BillingDate.Year, request.BillingDate.Month, DateTime.DaysInMonth(request.BillingDate.Year, request.BillingDate.Month)).AddDays(10),
                    IssueDate = request.BillingDate,
                    Frequency = "monthly",
                    Status = BillingStatuses.Abierto,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.ExecuteInTransactionAsync(async transaction =>
                {
                    await _billingRepository.CrearCicloAsync(cycle, transaction, cancellationToken);
                }, cancellationToken);
            }
        }

        return await _unitOfWork.ExecuteInTransactionAsync(async transaction =>
        {
            var existing = await _billingRepository.ObtenerLecturaAsync(actorTenantId, request.MeterId, cycle.Id, transaction, cancellationToken);
            if (existing is not null)
            {
                throw new InvalidOperationException("Ya existe una lectura registrada para este medidor en el periodo indicado.");
            }

            var lastReading = await _billingRepository.ObtenerUltimaLecturaAsync(actorTenantId, request.MeterId, transaction, cancellationToken);
            var previousValue = lastReading?.CurrentReading ?? 0;

            if (request.CurrentReading < previousValue)
            {
                throw new InvalidOperationException("La lectura actual no puede ser menor que la lectura anterior.");
            }

            var consumption = request.CurrentReading - previousValue;
            var reading = new MeterReading
            {
                TenantId = actorTenantId,
                MeterId = request.MeterId,
                BillingCycleId = cycle.Id,
                ReadAt = request.BillingDate,
                PreviousReading = previousValue,
                CurrentReading = request.CurrentReading,
                ConsumptionM3 = consumption,
                Source = string.IsNullOrWhiteSpace(request.Source) ? "manual" : request.Source.Trim(),
                Notes = request.Notes,
                CreatedBy = actorUsuarioId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = null
            };

            await _billingRepository.CrearLecturaAsync(reading, transaction, cancellationToken);
            return MapReading(reading);
        }, cancellationToken);
    }

    public async Task<InvoiceDto> ObtenerFacturaAsync(
        Guid actorUsuarioId,
        Guid actorTenantId,
        Guid invoiceId,
        CancellationToken cancellationToken = default)
    {
        var invoice = await _billingRepository.ObtenerFacturaAsync(actorTenantId, invoiceId, null, cancellationToken);
        if (invoice is null)
        {
            throw new InvalidOperationException("La factura no existe.");
        }

        var lines = await _billingRepository.ObtenerLineasAsync(actorTenantId, invoice.Id, null, cancellationToken);
        return MapInvoice(invoice, lines);
    }

    public async Task<MeterInvoiceHistoryResponseDto> ObtenerHistorialMedidorAsync(
        Guid actorUsuarioId,
        Guid actorTenantId,
        Guid meterId,
        CancellationToken cancellationToken = default)
    {
        var meter = await _medidorRepository.ObtenerPorIdAsync(actorTenantId, meterId, null, cancellationToken);
        if (meter is null)
        {
            throw new InvalidOperationException("El medidor no existe.");
        }

        var items = await _billingRepository.ObtenerHistorialPorMedidorAsync(actorTenantId, meterId, null, cancellationToken);
        return new MeterInvoiceHistoryResponseDto(meterId, items);
    }

    public async Task<UserDebtSummaryResponseDto> ObtenerResumenDeudaUsuarioAsync(
        Guid actorUsuarioId,
        Guid actorTenantId,
        Guid usuarioId,
        CancellationToken cancellationToken = default)
    {
        var items = await _billingRepository.ObtenerResumenPorUsuarioAsync(actorTenantId, usuarioId, null, cancellationToken);
        var totalPendingAmount = items.Sum(item => item.PendingAmount);
        var totalOverdueInvoices = items.Sum(item => item.OverdueInvoices);
        return new UserDebtSummaryResponseDto(usuarioId, totalPendingAmount, totalOverdueInvoices, items);
    }

    public async Task<InvoicePayResponseDto> RegistrarPagoAsync(
        Guid actorUsuarioId,
        Guid actorTenantId,
        Guid invoiceId,
        InvoicePayRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (request.Amount <= 0)
        {
            throw new ArgumentException("El monto del pago debe ser mayor a cero.", nameof(request.Amount));
        }

        return await _unitOfWork.ExecuteInTransactionAsync(async transaction =>
        {
            var invoice = await _billingRepository.ObtenerFacturaAsync(actorTenantId, invoiceId, transaction, cancellationToken);
            if (invoice is null)
            {
                throw new InvalidOperationException("La factura no existe.");
            }

            if (invoice.PendingAmount <= 0)
            {
                throw new InvalidOperationException("La factura ya se encuentra pagada.");
            }

            if (request.Amount > invoice.PendingAmount)
            {
                throw new InvalidOperationException("El monto supera el saldo pendiente de la factura.");
            }

            var newPaidAmount = invoice.PaidAmount + request.Amount;
            var newPendingAmount = invoice.TotalAmount - newPaidAmount;
            var newStatus = newPendingAmount <= 0
                ? BillingStatuses.Pagado
                : BillingStatuses.ParcialmentePagado;

            var payment = new Payment
            {
                TenantId = actorTenantId,
                InvoiceId = invoice.Id,
                MeterId = invoice.MeterId,
                UsuarioId = invoice.UsuarioId,
                PaymentDate = DateOnly.FromDateTime(DateTime.UtcNow),
                Amount = request.Amount,
                Method = request.Method.Trim(),
                Reference = request.Reference,
                Status = "aprobado",
                Notes = request.Notes,
                CreatedBy = actorUsuarioId,
                CreatedAt = DateTime.UtcNow
            };

            await _billingRepository.RegistrarPagoAsync(payment, transaction, cancellationToken);

            invoice.PaidAmount = newPaidAmount;
            invoice.PendingAmount = newPendingAmount;
            invoice.Status = newStatus;
            invoice.PaidAt = newPendingAmount <= 0 ? DateTime.UtcNow : invoice.PaidAt;
            invoice.UpdatedAt = DateTime.UtcNow;

            await _billingRepository.ActualizarFacturaAsync(invoice, transaction, cancellationToken);

            var updatedLines = await _billingRepository.ObtenerLineasAsync(actorTenantId, invoice.Id, transaction, cancellationToken);
            var updatedInvoice = MapInvoice(invoice, updatedLines);

            return new InvoicePayResponseDto(updatedInvoice, MapPayment(payment), "Pago registrado correctamente.");
        }, cancellationToken);
    }

    public async Task<InvoiceReliquidateResponseDto> ReliquidarFacturaAsync(
        Guid actorUsuarioId,
        Guid actorTenantId,
        Guid invoiceId,
        InvoiceReliquidateRequestDto request,
        CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.ExecuteInTransactionAsync(async transaction =>
        {
            var invoice = await _billingRepository.ObtenerFacturaAsync(actorTenantId, invoiceId, transaction, cancellationToken);
            if (invoice is null)
            {
                throw new InvalidOperationException("La factura no existe.");
            }

            var adjustment = new InvoiceAdjustment
            {
                TenantId = actorTenantId,
                MeterId = invoice.MeterId,
                InvoiceId = invoice.Id,
                AdjustmentType = request.AdjustmentType.Trim(),
                Amount = request.DeltaAmount,
                Reason = request.Reason.Trim(),
                SourceInvoiceId = invoice.Id,
                LinkedInvoiceId = null,
                EffectiveCycleId = invoice.BillingCycleId,
                Status = "aplicado",
                CreatedBy = actorUsuarioId,
                CreatedAt = DateTime.UtcNow
            };

            var reliquidatedInvoice = new Invoice
            {
                TenantId = actorTenantId,
                MeterId = invoice.MeterId,
                UsuarioId = invoice.UsuarioId,
                BillingCycleId = invoice.BillingCycleId,
                MeterReadingId = invoice.MeterReadingId,
                InvoiceNumber = $"{invoice.InvoiceNumber}-R1",
                Status = BillingStatuses.Emitido,
                Currency = invoice.Currency,
                Subtotal = 0,
                PreviousBalance = 0,
                LateFeeAmount = 0,
                OperationalPenaltyAmount = 0,
                AdjustmentsAmount = request.DeltaAmount,
                TotalAmount = request.DeltaAmount,
                PaidAmount = 0,
                PendingAmount = request.DeltaAmount,
                IssuedAt = DateTime.UtcNow,
                DueDate = DateOnly.FromDateTime(DateTime.UtcNow),
                PaidAt = null,
                CancelledAt = null,
                ReliquidatedFromInvoiceId = invoice.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = null
            };

            await _billingRepository.CrearFacturaAsync(reliquidatedInvoice, transaction, cancellationToken);
            adjustment.LinkedInvoiceId = reliquidatedInvoice.Id;
            await _billingRepository.RegistrarAjusteAsync(adjustment, transaction, cancellationToken);
            await _billingRepository.CrearLineaAsync(
                new InvoiceLine
                {
                    TenantId = actorTenantId,
                    InvoiceId = reliquidatedInvoice.Id,
                    LineType = "reliquidacion",
                    Description = request.Reason.Trim(),
                    Quantity = 1,
                    UnitPrice = request.DeltaAmount,
                    Amount = request.DeltaAmount,
                    ReferenceTable = "invoice_adjustments",
                    ReferenceId = adjustment.Id,
                    Metadata = JsonSerializer.Serialize(new
                    {
                        originalInvoice = invoice.InvoiceNumber,
                        adjustment.Amount,
                        request.AdjustmentType
                    }),
                    CreatedAt = DateTime.UtcNow
                },
                transaction,
                cancellationToken);

            var lines = await _billingRepository.ObtenerLineasAsync(actorTenantId, reliquidatedInvoice.Id, transaction, cancellationToken);
            return new InvoiceReliquidateResponseDto(
                MapAdjustment(adjustment),
                MapInvoice(reliquidatedInvoice, lines),
                "Factura reliquidada correctamente.");
        }, cancellationToken);
    }

    public async Task<InvoiceAdjustmentDto> RegistrarMultaOperativaAsync(
        Guid actorUsuarioId,
        Guid actorTenantId,
        AssignOperationalPenaltyRequestDto request,
        CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.ExecuteInTransactionAsync(async transaction =>
        {
            var meters = await _billingRepository.ObtenerMedidoresActivosAsync(actorTenantId, transaction, cancellationToken);
            var meter = meters
                .Where(item => item.UsuarioId == request.UsuarioId)
                .OrderBy(item => item.NumeroMedidor)
                .FirstOrDefault();

            if (meter is null)
            {
                throw new InvalidOperationException("El usuario no tiene medidores activos para asignar la multa.");
            }

            var adjustment = new InvoiceAdjustment
            {
                TenantId = actorTenantId,
                MeterId = meter.Id,
                InvoiceId = null,
                AdjustmentType = "multa_operativa",
                Amount = request.Amount,
                Reason = request.SourceType.Trim(),
                SourceReadingId = null,
                SourceInvoiceId = null,
                LinkedInvoiceId = null,
                EffectiveCycleId = null,
                Status = "aplicado",
                CreatedBy = actorUsuarioId,
                CreatedAt = DateTime.UtcNow
            };

            await _billingRepository.RegistrarAjusteAsync(adjustment, transaction, cancellationToken);
            return MapAdjustment(adjustment);
        }, cancellationToken);
    }

    private async Task<TenantConfig> ObtenerConfigAsync(Guid tenantId, System.Data.IDbTransaction transaction, CancellationToken cancellationToken)
    {
        var config = await _tenantConfigRepository.ObtenerPorTenantIdAsync(tenantId, transaction, cancellationToken);
        if (config is null)
        {
            throw new InvalidOperationException("El tenant no tiene configuracion de facturacion.");
        }

        return config;
    }

    private static string BuildPeriodCode(DateOnly date)
    {
        return $"{date:yyyy-MM}";
    }

    private static string BuildInvoiceNumber(BillingCycle cycle, Medidor meter)
    {
        return $"FAC-{cycle.PeriodCode}-{meter.NumeroMedidor:0000}";
    }

    private async Task<IReadOnlyList<OperationalPenaltyLine>> BuildOperationalPenaltyLinesAsync(
        Guid tenantId,
        Medidor meter,
        BillingCycle cycle,
        System.Data.IDbTransaction transaction,
        CancellationToken cancellationToken)
    {
        var pendingPenalties = await _billingRepository.ObtenerMultasPendientesAsync(tenantId, cycle.PeriodEnd, transaction, cancellationToken);
        var meterPrimaryByUser = await _billingRepository.ObtenerMedidoresActivosAsync(tenantId, transaction, cancellationToken);
        var primaryByUser = meterPrimaryByUser
            .GroupBy(item => item.UsuarioId)
            .ToDictionary(group => group.Key, group => group.OrderBy(item => item.NumeroMedidor).First().Id);

        if (!primaryByUser.TryGetValue(meter.UsuarioId, out var primaryMeterId) || primaryMeterId != meter.Id)
        {
            return Array.Empty<OperationalPenaltyLine>();
        }

        var result = new List<OperationalPenaltyLine>();
        foreach (var penalty in pendingPenalties.Where(item => item.UsuarioId == meter.UsuarioId))
        {
            result.Add(new OperationalPenaltyLine(
                penalty.Id,
                penalty.Amount,
                $"Multa operativa {penalty.SourceType} {penalty.SourceDate:yyyy-MM-dd}",
                JsonSerializer.Serialize(new
                {
                    penalty.SourceType,
                    penalty.SourceDate,
                    penalty.Notes
                })));
        }

        return result;
    }

    private static IReadOnlyList<TariffLine> BuildTariffLines(decimal consumption, TenantConfig config)
    {
        var steps = ResolveTariffSteps(config);
        var lines = new List<TariffLine>();

        foreach (var step in steps)
        {
            if (consumption <= step.DesdeM3)
            {
                continue;
            }

            var mode = NormalizeMode(step.ModoCobro);
            if (mode is "variable" or "por_m3")
            {
                var upperBound = step.HastaM3 ?? consumption;
                var quantity = Math.Max(0, Math.Min(consumption, upperBound) - step.DesdeM3);
                if (quantity <= 0)
                {
                    continue;
                }

                lines.Add(new TariffLine(
                    "consumo_variable",
                    $"Consumo de {step.DesdeM3:0.##} a {upperBound:0.##} m3",
                    quantity,
                    step.Cargo,
                    quantity * step.Cargo,
                    "billing_tariff",
                    null,
                    JsonSerializer.Serialize(new { step.DesdeM3, step.HastaM3, step.Cargo, step.ModoCobro })));
                continue;
            }

            lines.Add(new TariffLine(
                "consumo_fijo",
                step.HastaM3.HasValue
                    ? $"Cargo fijo de {step.DesdeM3:0.##} a {step.HastaM3:0.##} m3"
                    : $"Cargo fijo desde {step.DesdeM3:0.##} m3",
                1,
                step.Cargo,
                step.Cargo,
                "billing_tariff",
                null,
                JsonSerializer.Serialize(new { step.DesdeM3, step.HastaM3, step.Cargo, step.ModoCobro })));
        }

        if (!lines.Any())
        {
            lines.Add(new TariffLine("consumo_fijo", "Consumo base", 1, 0, 0, "billing_tariff", null, null));
        }

        return lines;
    }

    private static IReadOnlyList<TariffStep> ResolveTariffSteps(TenantConfig config)
    {
        if (!string.IsNullOrWhiteSpace(config.TramosConsumoJson))
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<List<ConsumoTramoDto>>(config.TramosConsumoJson, JsonOptions);
                if (parsed is { Count: > 0 })
                {
                    return parsed
                        .OrderBy(item => item.DesdeM3)
                        .Select(item => new TariffStep(item.DesdeM3, item.HastaM3, item.Cargo, item.ModoCobro))
                        .ToList();
                }
            }
            catch
            {
            }
        }

        return new List<TariffStep>
        {
            new(0, config.LimiteConsumoFijo, config.PrecioConsumoFijo, "fijo_por_rango"),
            new(config.LimiteConsumoFijo, config.LimiteConsumoExtra1, config.CargoExtra1, "fijo_por_rango"),
            new(config.LimiteConsumoExtra1, config.LimiteConsumoExtra2, config.CargoExtra2, "fijo_por_rango"),
            new(config.LimiteConsumoExtra2, config.LimiteConsumoExtra3, config.CargoExtra3, "fijo_por_rango"),
            new(config.LimiteConsumoExtra3, null, config.CargoExcesoMayor, "por_m3")
        };
    }

    private static string NormalizeMode(string mode)
    {
        mode = mode.Trim().ToLowerInvariant();
        if (mode.Contains("m3") || mode.Contains("variable"))
        {
            return "variable";
        }

        return "fijo";
    }

    private static MeterReadingDto MapReading(MeterReading reading)
    {
        return new MeterReadingDto(
            reading.Id,
            reading.TenantId,
            reading.MeterId,
            reading.BillingCycleId,
            reading.ReadAt,
            reading.PreviousReading,
            reading.CurrentReading,
            reading.ConsumptionM3,
            reading.Source,
            reading.Notes,
            reading.CreatedBy,
            reading.CreatedAt,
            reading.UpdatedAt);
    }

    private async Task<InvoiceDto> MapInvoiceAsync(Guid tenantId, Invoice invoice)
    {
        var lines = await _billingRepository.ObtenerLineasAsync(tenantId, invoice.Id, null, CancellationToken.None);
        return MapInvoice(invoice, lines);
    }

    private static InvoiceDto MapInvoice(Invoice invoice, IReadOnlyList<InvoiceLine> lines)
    {
        return new InvoiceDto(
            invoice.Id,
            invoice.TenantId,
            invoice.MeterId,
            invoice.UsuarioId,
            invoice.BillingCycleId,
            invoice.MeterReadingId,
            invoice.InvoiceNumber,
            invoice.Status,
            invoice.Currency,
            invoice.Subtotal,
            invoice.PreviousBalance,
            invoice.LateFeeAmount,
            invoice.OperationalPenaltyAmount,
            invoice.AdjustmentsAmount,
            invoice.TotalAmount,
            invoice.PaidAmount,
            invoice.PendingAmount,
            invoice.IssuedAt,
            invoice.DueDate,
            invoice.PaidAt,
            invoice.CancelledAt,
            invoice.ReliquidatedFromInvoiceId,
            invoice.CreatedAt,
            invoice.UpdatedAt,
            lines.Select(MapLine).ToList());
    }

    private static InvoiceLineDto MapLine(InvoiceLine line)
    {
        return new InvoiceLineDto(
            line.Id,
            line.TenantId,
            line.InvoiceId,
            line.LineType,
            line.Description,
            line.Quantity,
            line.UnitPrice,
            line.Amount,
            line.ReferenceTable,
            line.ReferenceId,
            line.Metadata,
            line.CreatedAt);
    }

    private static PaymentDto MapPayment(Payment payment)
    {
        return new PaymentDto(
            payment.Id,
            payment.TenantId,
            payment.InvoiceId,
            payment.MeterId,
            payment.UsuarioId,
            payment.PaymentDate,
            payment.Amount,
            payment.Method,
            payment.Reference,
            payment.Status,
            payment.Notes,
            payment.CreatedBy,
            payment.CreatedAt);
    }

    private static InvoiceAdjustmentDto MapAdjustment(InvoiceAdjustment adjustment)
    {
        return new InvoiceAdjustmentDto(
            adjustment.Id,
            adjustment.TenantId,
            adjustment.MeterId,
            adjustment.InvoiceId,
            adjustment.AdjustmentType,
            adjustment.Amount,
            adjustment.Reason,
            adjustment.SourceReadingId,
            adjustment.SourceInvoiceId,
            adjustment.LinkedInvoiceId,
            adjustment.EffectiveCycleId,
            adjustment.Status,
            adjustment.CreatedBy,
            adjustment.CreatedAt);
    }

    private sealed record TariffStep(decimal DesdeM3, decimal? HastaM3, decimal Cargo, string ModoCobro);

    private sealed record TariffLine(
        string LineType,
        string Description,
        decimal Quantity,
        decimal UnitPrice,
        decimal Amount,
        string ReferenceTable,
        Guid? ReferenceId,
        string? Metadata);

    private sealed record OperationalPenaltyLine(
        Guid ReferenceId,
        decimal Amount,
        string Description,
        string? Metadata);
}
