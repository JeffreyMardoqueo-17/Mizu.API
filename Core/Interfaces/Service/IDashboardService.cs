using Muzu.Api.Core.DTOs;

namespace Muzu.Api.Core.Interfaces.Service;

public interface IDashboardService
{
    Task<DashboardSummaryDto> GetSummaryAsync(Guid actorUsuarioId, Guid tenantId, CancellationToken cancellationToken = default);
    Task<UsersStatsDto> GetUsersStatsAsync(Guid actorUsuarioId, Guid tenantId, CancellationToken cancellationToken = default);
    Task<NextBillingCycleDto?> GetNextBillingCycleAsync(Guid actorUsuarioId, Guid tenantId, CancellationToken cancellationToken = default);
    Task<NextMeetingDto?> GetNextMeetingAsync(Guid actorUsuarioId, Guid tenantId, CancellationToken cancellationToken = default);
    Task<CurrentBoardDto?> GetCurrentBoardAsync(Guid actorUsuarioId, Guid tenantId, CancellationToken cancellationToken = default);
    Task<PenaltiesSummaryDto> GetPenaltiesSummaryAsync(Guid actorUsuarioId, Guid tenantId, CancellationToken cancellationToken = default);
    Task<DebtSummaryDto> GetDebtSummaryAsync(Guid actorUsuarioId, Guid tenantId, CancellationToken cancellationToken = default);
    Task<DebtorsListDto> GetDebtorsListAsync(Guid actorUsuarioId, Guid tenantId, int page, int pageSize, CancellationToken cancellationToken = default);
}
