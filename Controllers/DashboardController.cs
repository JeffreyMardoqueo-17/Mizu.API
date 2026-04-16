using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Muzu.Api.Core.DTOs;
using Muzu.Api.Core.Interfaces.Service;

namespace Muzu.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/dashboard")]
public sealed class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken)
    {
        if (!TryGetAuthContext(out var actorUsuarioId, out var actorTenantId))
        {
            return Unauthorized();
        }

        try
        {
            var result = await _dashboardService.GetSummaryAsync(actorUsuarioId, actorTenantId, cancellationToken);
            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpGet("users/stats")]
    public async Task<IActionResult> GetUsersStats(CancellationToken cancellationToken)
    {
        if (!TryGetAuthContext(out var actorUsuarioId, out var actorTenantId))
        {
            return Unauthorized();
        }

        try
        {
            var result = await _dashboardService.GetUsersStatsAsync(actorUsuarioId, actorTenantId, cancellationToken);
            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpGet("billing/next")]
    public async Task<IActionResult> GetNextBillingCycle(CancellationToken cancellationToken)
    {
        if (!TryGetAuthContext(out var actorUsuarioId, out var actorTenantId))
        {
            return Unauthorized();
        }

        try
        {
            var result = await _dashboardService.GetNextBillingCycleAsync(actorUsuarioId, actorTenantId, cancellationToken);
            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpGet("meetings/next")]
    public async Task<IActionResult> GetNextMeeting(CancellationToken cancellationToken)
    {
        if (!TryGetAuthContext(out var actorUsuarioId, out var actorTenantId))
        {
            return Unauthorized();
        }

        try
        {
            var result = await _dashboardService.GetNextMeetingAsync(actorUsuarioId, actorTenantId, cancellationToken);
            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpGet("board/current")]
    public async Task<IActionResult> GetCurrentBoard(CancellationToken cancellationToken)
    {
        if (!TryGetAuthContext(out var actorUsuarioId, out var actorTenantId))
        {
            return Unauthorized();
        }

        try
        {
            var result = await _dashboardService.GetCurrentBoardAsync(actorUsuarioId, actorTenantId, cancellationToken);
            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpGet("penalties/summary")]
    public async Task<IActionResult> GetPenaltiesSummary(CancellationToken cancellationToken)
    {
        if (!TryGetAuthContext(out var actorUsuarioId, out var actorTenantId))
        {
            return Unauthorized();
        }

        try
        {
            var result = await _dashboardService.GetPenaltiesSummaryAsync(actorUsuarioId, actorTenantId, cancellationToken);
            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpGet("debt/summary")]
    public async Task<IActionResult> GetDebtSummary(CancellationToken cancellationToken)
    {
        if (!TryGetAuthContext(out var actorUsuarioId, out var actorTenantId))
        {
            return Unauthorized();
        }

        try
        {
            var result = await _dashboardService.GetDebtSummaryAsync(actorUsuarioId, actorTenantId, cancellationToken);
            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpGet("debtors")]
    public async Task<IActionResult> GetDebtors(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetAuthContext(out var actorUsuarioId, out var actorTenantId))
        {
            return Unauthorized();
        }

        try
        {
            var result = await _dashboardService.GetDebtorsListAsync(actorUsuarioId, actorTenantId, page, pageSize, cancellationToken);
            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    private bool TryGetAuthContext(out Guid usuarioId, out Guid tenantId)
    {
        usuarioId = Guid.Empty;
        tenantId = Guid.Empty;

        var userIdRaw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var tenantIdRaw = User.FindFirstValue("tenant_id");

        if (!Guid.TryParse(userIdRaw, out usuarioId))
        {
            return false;
        }

        if (!Guid.TryParse(tenantIdRaw, out tenantId))
        {
            return false;
        }

        return true;
    }
}
