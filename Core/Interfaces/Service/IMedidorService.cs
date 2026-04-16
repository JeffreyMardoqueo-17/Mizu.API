using Muzu.Api.Core.DTOs;

namespace Muzu.Api.Core.Interfaces.Service;

public interface IMedidorService
{
    Task<MeterListResponseDto> ObtenerPorUsuarioAsync(
        Guid actorUsuarioId,
        Guid actorTenantId,
        Guid usuarioId,
        CancellationToken cancellationToken = default);

    Task<MeterNextNumberResponseDto> ObtenerSiguienteNumeroAsync(
        Guid actorUsuarioId,
        Guid actorTenantId,
        Guid usuarioId,
        CancellationToken cancellationToken = default);

    Task<MeterAssignResponseDto> AsignarAsync(
        Guid actorUsuarioId,
        Guid actorTenantId,
        MeterAssignRequestDto request,
        CancellationToken cancellationToken = default);

    Task<MeterStatusResponseDto> ActualizarEstadoAsync(
        Guid actorUsuarioId,
        Guid actorTenantId,
        Guid medidorId,
        MeterStatusUpdateRequestDto request,
        CancellationToken cancellationToken = default);

    Task<MeterTransferResponseDto> TransferirAsync(
        Guid actorUsuarioId,
        Guid actorTenantId,
        Guid medidorId,
        MeterTransferRequestDto request,
        CancellationToken cancellationToken = default);

    Task<MeterTransferHistoryResponseDto> ObtenerHistorialTransferenciasAsync(
        Guid actorUsuarioId,
        Guid actorTenantId,
        Guid medidorId,
        CancellationToken cancellationToken = default);

    Task<MeterRuleConflictReportDto> ObtenerReporteConflictosActivosAsync(
        Guid actorUsuarioId,
        Guid actorTenantId,
        CancellationToken cancellationToken = default);
}
