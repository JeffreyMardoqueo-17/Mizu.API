using Muzu.Api.Core.DTOs;

namespace Muzu.Api.Core.Interfaces.Service;

public interface IBillingService
{
    Task<GenerateBillingCycleResponseDto> GenerarCicloAsync(
        Guid actorUsuarioId,
        Guid actorTenantId,
        CreateBillingCycleRequestDto request,
        CancellationToken cancellationToken = default);

    Task<MeterReadingDto> RegistrarLecturaAsync(
        Guid actorUsuarioId,
        Guid actorTenantId,
        MeterReadingCreateRequestDto request,
        CancellationToken cancellationToken = default);

    Task<InvoiceDto> ObtenerFacturaAsync(
        Guid actorUsuarioId,
        Guid actorTenantId,
        Guid invoiceId,
        CancellationToken cancellationToken = default);

    Task<MeterInvoiceHistoryResponseDto> ObtenerHistorialMedidorAsync(
        Guid actorUsuarioId,
        Guid actorTenantId,
        Guid meterId,
        CancellationToken cancellationToken = default);

    Task<UserDebtSummaryResponseDto> ObtenerResumenDeudaUsuarioAsync(
        Guid actorUsuarioId,
        Guid actorTenantId,
        Guid usuarioId,
        CancellationToken cancellationToken = default);

    Task<InvoicePayResponseDto> RegistrarPagoAsync(
        Guid actorUsuarioId,
        Guid actorTenantId,
        Guid invoiceId,
        InvoicePayRequestDto request,
        CancellationToken cancellationToken = default);

    Task<InvoiceReliquidateResponseDto> ReliquidarFacturaAsync(
        Guid actorUsuarioId,
        Guid actorTenantId,
        Guid invoiceId,
        InvoiceReliquidateRequestDto request,
        CancellationToken cancellationToken = default);

    Task<InvoiceAdjustmentDto> RegistrarMultaOperativaAsync(
        Guid actorUsuarioId,
        Guid actorTenantId,
        AssignOperationalPenaltyRequestDto request,
        CancellationToken cancellationToken = default);
}
