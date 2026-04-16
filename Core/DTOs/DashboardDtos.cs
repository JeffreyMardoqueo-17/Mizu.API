namespace Muzu.Api.Core.DTOs;

public sealed record DashboardSummaryDto(
    int TotalUsers,
    int ActiveUsers,
    int UsersWithDebt,
    int TotalMeters,
    int ActiveMeters,
    int MetersWithConflicts,
    NextBillingCycleDto? NextBillingCycle,
    NextMeetingDto? NextMeeting,
    CurrentBoardDto? CurrentBoard,
    PenaltiesSummaryDto PenaltiesSummary,
    DebtSummaryDto DebtSummary
);

public sealed record UsersStatsDto(
    int TotalUsers,
    int ActiveUsers,
    int InactiveUsers,
    int NewUsersThisMonth,
    int UsersWithDebt
);

public sealed record NextBillingCycleDto(
    Guid Id,
    string PeriodCode,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    DateOnly DueDate,
    DateOnly IssueDate,
    string Status,
    int TotalInvoices,
    int PendingInvoices,
    int PaidInvoices,
    decimal TotalPendingAmount,
    int DaysUntilDue
);

public sealed record NextMeetingDto(
    Guid Id,
    string Titulo,
    DateTime FechaHora,
    string Estado,
    int TotalMembers,
    int ConfirmedAttendees,
    int PendingAttendees,
    string? Lugar,
    int DaysUntilMeeting
);

public sealed record CurrentBoardDto(
    Guid Id,
    string Nombre,
    DateOnly FechaInicio,
    DateOnly FechaFin,
    string Estado,
    int TotalMembers,
    int DaysUntilExpiration
);

public sealed record PenaltiesSummaryDto(
    int TotalPenalties,
    int PendingPenalties,
    int AssignedPenalties,
    decimal TotalPendingAmount,
    IReadOnlyList<PenaltyByTypeDto> ByType
);

public sealed record PenaltyByTypeDto(
    string Type,
    string Description,
    int Count,
    decimal TotalAmount
);

public sealed record DebtSummaryDto(
    int TotalDebtors,
    int UsersOverdue,
    decimal TotalPendingAmount,
    decimal TotalOverdueAmount,
    decimal AverageDebt,
    IReadOnlyList<TopDebtorDto> TopDebtors
);

public sealed record TopDebtorDto(
    Guid UsuarioId,
    string NombreCompleto,
    decimal PendingAmount,
    int OverdueInvoices,
    DateOnly? OldestDueDate
);

public sealed record DebtorsListDto(
    IReadOnlyList<DebtorDto> Items,
    int TotalCount,
    int Page,
    int PageSize
);

public sealed record DebtorDto(
    Guid UsuarioId,
    string NombreCompleto,
    string DUI,
    string? Email,
    int TotalInvoices,
    int OverdueInvoices,
    decimal TotalPendingAmount,
    decimal OverdueAmount,
    DateOnly? OldestDueDate,
    DateTime FechaRegistro
);

public sealed record MeetingAttendanceSummaryDto(
    Guid MeetingId,
    string Titulo,
    int TotalMembers,
    int Attended,
    int Absent,
    decimal AttendanceRate,
    IReadOnlyList<MissingMemberDto> MissingMembers
);

public sealed record MissingMemberDto(
    Guid UsuarioId,
    string NombreCompleto,
    string DUI
);

public sealed record DashboardUpdateEventDto(
    string EventType,
    DateTime Timestamp,
    DashboardUpdatePayloadDto Payload
);

public sealed record DashboardUpdatePayloadDto(
    string? MetricName,
    int? PreviousValue,
    int? NewValue,
    string? Description
);
