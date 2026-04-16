using System.Data;
using Muzu.Api.Core.DTOs;

namespace Muzu.Api.Core.Interfaces;

public interface IDashboardRepository
{
    Task<UsersStatsDto> GetUsersStatsAsync(Guid tenantId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<int> GetTotalUsersAsync(Guid tenantId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<int> GetActiveUsersAsync(Guid tenantId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<int> GetNewUsersThisMonthAsync(Guid tenantId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<int> GetUsersWithDebtAsync(Guid tenantId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<int> GetTotalMetersAsync(Guid tenantId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<int> GetActiveMetersAsync(Guid tenantId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<int> GetMetersWithConflictsAsync(Guid tenantId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<NextBillingCycleDto?> GetNextBillingCycleAsync(Guid tenantId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<NextMeetingDto?> GetNextMeetingAsync(Guid tenantId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<CurrentBoardDto?> GetCurrentBoardAsync(Guid tenantId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<PenaltiesSummaryDto> GetPenaltiesSummaryAsync(Guid tenantId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<int> GetTotalPenaltiesAsync(Guid tenantId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<int> GetPendingPenaltiesAsync(Guid tenantId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<int> GetAssignedPenaltiesAsync(Guid tenantId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<decimal> GetTotalPendingPenaltiesAmountAsync(Guid tenantId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PenaltyByTypeDto>> GetPenaltiesByTypeAsync(Guid tenantId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<DebtSummaryDto> GetDebtSummaryAsync(Guid tenantId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<int> GetTotalDebtorsAsync(Guid tenantId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<int> GetUsersOverdueAsync(Guid tenantId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<decimal> GetTotalPendingDebtAsync(Guid tenantId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<decimal> GetTotalOverdueDebtAsync(Guid tenantId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TopDebtorDto>> GetTopDebtorsAsync(Guid tenantId, int limit = 10, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<DebtorsListDto> GetDebtorsListAsync(Guid tenantId, int page, int pageSize, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
}
