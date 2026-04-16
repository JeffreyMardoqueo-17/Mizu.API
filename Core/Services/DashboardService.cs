using Muzu.Api.Core.DTOs;
using Muzu.Api.Core.Interfaces;
using Muzu.Api.Core.Interfaces.Service;
using Muzu.Api.Core.Rules;

namespace Muzu.Api.Core.Services;

public sealed class DashboardService : IDashboardService
{
    private readonly IDashboardRepository _dashboardRepository;

    public DashboardService(IDashboardRepository dashboardRepository)
    {
        _dashboardRepository = dashboardRepository;
    }

    public async Task<DashboardSummaryDto> GetSummaryAsync(Guid actorUsuarioId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        var usersStats = await _dashboardRepository.GetUsersStatsAsync(tenantId, cancellationToken: cancellationToken);
        var totalMeters = await _dashboardRepository.GetTotalMetersAsync(tenantId, cancellationToken: cancellationToken);
        var activeMeters = await _dashboardRepository.GetActiveMetersAsync(tenantId, cancellationToken: cancellationToken);
        var metersWithConflicts = await _dashboardRepository.GetMetersWithConflictsAsync(tenantId, cancellationToken: cancellationToken);
        var nextBillingCycle = await _dashboardRepository.GetNextBillingCycleAsync(tenantId, cancellationToken: cancellationToken);
        var nextMeeting = await _dashboardRepository.GetNextMeetingAsync(tenantId, cancellationToken: cancellationToken);
        var currentBoard = await _dashboardRepository.GetCurrentBoardAsync(tenantId, cancellationToken: cancellationToken);
        var penaltiesSummary = await _dashboardRepository.GetPenaltiesSummaryAsync(tenantId, cancellationToken: cancellationToken);
        var debtSummary = await _dashboardRepository.GetDebtSummaryAsync(tenantId, cancellationToken: cancellationToken);

        return new DashboardSummaryDto(
            TotalUsers: usersStats.TotalUsers,
            ActiveUsers: usersStats.ActiveUsers,
            UsersWithDebt: usersStats.UsersWithDebt,
            TotalMeters: totalMeters,
            ActiveMeters: activeMeters,
            MetersWithConflicts: metersWithConflicts,
            NextBillingCycle: nextBillingCycle,
            NextMeeting: nextMeeting,
            CurrentBoard: currentBoard,
            PenaltiesSummary: penaltiesSummary,
            DebtSummary: debtSummary
        );
    }

    public async Task<UsersStatsDto> GetUsersStatsAsync(Guid actorUsuarioId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        if (!TieneAccesoDashboard(actorUsuarioId, tenantId))
        {
            throw new UnauthorizedAccessException("No tiene acceso a las estadísticas de usuarios.");
        }

        return await _dashboardRepository.GetUsersStatsAsync(tenantId, cancellationToken: cancellationToken);
    }

    public async Task<NextBillingCycleDto?> GetNextBillingCycleAsync(Guid actorUsuarioId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        if (!TieneAccesoFinanciero(actorUsuarioId))
        {
            throw new UnauthorizedAccessException("No tiene acceso a la información de facturación.");
        }

        return await _dashboardRepository.GetNextBillingCycleAsync(tenantId, cancellationToken: cancellationToken);
    }

    public async Task<NextMeetingDto?> GetNextMeetingAsync(Guid actorUsuarioId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _dashboardRepository.GetNextMeetingAsync(tenantId, cancellationToken: cancellationToken);
    }

    public async Task<CurrentBoardDto?> GetCurrentBoardAsync(Guid actorUsuarioId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _dashboardRepository.GetCurrentBoardAsync(tenantId, cancellationToken: cancellationToken);
    }

    public async Task<PenaltiesSummaryDto> GetPenaltiesSummaryAsync(Guid actorUsuarioId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        if (!TieneAccesoFinanciero(actorUsuarioId))
        {
            throw new UnauthorizedAccessException("No tiene acceso a la información de multas.");
        }

        return await _dashboardRepository.GetPenaltiesSummaryAsync(tenantId, cancellationToken: cancellationToken);
    }

    public async Task<DebtSummaryDto> GetDebtSummaryAsync(Guid actorUsuarioId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        if (!TieneAccesoFinanciero(actorUsuarioId))
        {
            throw new UnauthorizedAccessException("No tiene acceso a la información de deudas.");
        }

        return await _dashboardRepository.GetDebtSummaryAsync(tenantId, cancellationToken: cancellationToken);
    }

    public async Task<DebtorsListDto> GetDebtorsListAsync(Guid actorUsuarioId, Guid tenantId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        if (!TieneAccesoFinanciero(actorUsuarioId))
        {
            throw new UnauthorizedAccessException("No tiene acceso a la lista de deudores.");
        }

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        return await _dashboardRepository.GetDebtorsListAsync(tenantId, page, pageSize, cancellationToken: cancellationToken);
    }

    private static bool TieneAccesoDashboard(Guid actorUsuarioId, Guid tenantId)
    {
        return true;
    }

    private static bool TieneAccesoFinanciero(Guid actorUsuarioId)
    {
        return true;
    }
}
