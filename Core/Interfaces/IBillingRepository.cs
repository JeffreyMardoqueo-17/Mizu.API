using System.Data;
using Muzu.Api.Core.DTOs;
using Muzu.Api.Core.Models;

namespace Muzu.Api.Core.Interfaces;

public interface IBillingRepository
{
    Task<IReadOnlyList<Medidor>> ObtenerMedidoresActivosAsync(Guid tenantId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);

    Task<BillingCycle?> ObtenerCicloAsync(Guid tenantId, string periodCode, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<BillingCycle?> ObtenerCicloPorIdAsync(Guid tenantId, Guid billingCycleId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<BillingCycle> CrearCicloAsync(BillingCycle cycle, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<bool> ActualizarCicloAsync(BillingCycle cycle, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);

    Task<MeterReading?> ObtenerLecturaAsync(Guid tenantId, Guid meterId, Guid billingCycleId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<MeterReading?> ObtenerUltimaLecturaAsync(Guid tenantId, Guid meterId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<MeterReading> CrearLecturaAsync(MeterReading reading, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);

    Task<Invoice?> ObtenerFacturaAsync(Guid tenantId, Guid invoiceId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<Invoice?> ObtenerFacturaActivaPorMedidorYCicloAsync(Guid tenantId, Guid meterId, Guid billingCycleId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<Invoice?> ObtenerFacturaAnteriorAsync(Guid tenantId, Guid meterId, Guid billingCycleId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<Invoice> CrearFacturaAsync(Invoice invoice, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<bool> ActualizarFacturaAsync(Invoice invoice, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);

    Task<InvoiceLine> CrearLineaAsync(InvoiceLine line, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InvoiceLine>> ObtenerLineasAsync(Guid tenantId, Guid invoiceId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);

    Task<Payment> RegistrarPagoAsync(Payment payment, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);

    Task<bool> ExisteMoraGeneradaAsync(Guid tenantId, Guid sourceInvoiceId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<LateFeeHistory> RegistrarMoraAsync(LateFeeHistory history, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OperationalPenalty>> ObtenerMultasPendientesAsync(Guid tenantId, DateOnly limitDate, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<bool> AsignarMultaAsync(Guid tenantId, Guid penaltyId, Guid meterId, Guid invoiceId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<OperationalPenalty> RegistrarMultaOperativaPendienteAsync(OperationalPenalty penalty, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);

    Task<CarriedBalance> RegistrarSaldoArrastradoAsync(CarriedBalance carriedBalance, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<InvoiceAdjustment> RegistrarAjusteAsync(InvoiceAdjustment adjustment, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InvoiceSummaryDto>> ObtenerHistorialPorMedidorAsync(Guid tenantId, Guid meterId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserDebtSummaryMeterDto>> ObtenerResumenPorUsuarioAsync(Guid tenantId, Guid usuarioId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
}
