using System.ComponentModel.DataAnnotations;

namespace Muzu.Api.Core.DTOs;

public sealed record BillingCycleDto(
    Guid Id,
    Guid TenantId,
    string PeriodCode,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    DateOnly DueDate,
    DateOnly IssueDate,
    string Frequency,
    string Status,
    DateTime CreatedAt,
    DateTime? ClosedAt
);

public sealed record CreateBillingCycleRequestDto(
    [param: Required, StringLength(20)] string PeriodCode,
    [param: Required] DateOnly PeriodStart,
    [param: Required] DateOnly PeriodEnd,
    [param: Required] DateOnly DueDate,
    [param: Required] DateOnly IssueDate,
    [param: Required, StringLength(20)] string Frequency
);

public sealed record GenerateBillingCycleResponseDto(
    string PeriodCode,
    int TotalMetersProcessed,
    int TotalInvoicesGenerated,
    decimal TotalBilledAmount,
    string Message
);

public sealed record MeterReadingCreateRequestDto(
    [param: Required] Guid MeterId,
    [param: Required] DateOnly BillingDate,
    [param: Required, Range(typeof(decimal), "0", "999999999")] decimal CurrentReading,
    [param: StringLength(20)] string? Source,
    string? Notes,
    Guid? BillingCycleId
);

public sealed record MeterReadingDto(
    Guid Id,
    Guid TenantId,
    Guid MeterId,
    Guid BillingCycleId,
    DateOnly ReadAt,
    decimal PreviousReading,
    decimal CurrentReading,
    decimal ConsumptionM3,
    string Source,
    string? Notes,
    Guid? CreatedBy,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public sealed record InvoiceLineDto(
    Guid Id,
    Guid TenantId,
    Guid InvoiceId,
    string LineType,
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    decimal Amount,
    string? ReferenceTable,
    Guid? ReferenceId,
    string? Metadata,
    DateTime CreatedAt
);

public sealed record InvoiceDto(
    Guid Id,
    Guid TenantId,
    Guid MeterId,
    Guid UsuarioId,
    Guid BillingCycleId,
    Guid MeterReadingId,
    string InvoiceNumber,
    string Status,
    string Currency,
    decimal Subtotal,
    decimal PreviousBalance,
    decimal LateFeeAmount,
    decimal OperationalPenaltyAmount,
    decimal AdjustmentsAmount,
    decimal TotalAmount,
    decimal PaidAmount,
    decimal PendingAmount,
    DateTime? IssuedAt,
    DateOnly DueDate,
    DateTime? PaidAt,
    DateTime? CancelledAt,
    Guid? ReliquidatedFromInvoiceId,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    IReadOnlyList<InvoiceLineDto> Lines
);

public sealed record InvoiceSummaryDto(
    Guid InvoiceId,
    Guid MeterId,
    Guid UsuarioId,
    string InvoiceNumber,
    string Status,
    DateOnly DueDate,
    decimal TotalAmount,
    decimal PaidAmount,
    decimal PendingAmount,
    DateTime? IssuedAt
);

public sealed record MeterInvoiceHistoryResponseDto(
    Guid MeterId,
    IReadOnlyList<InvoiceSummaryDto> Items
);

public sealed record UserDebtSummaryMeterDto(
    Guid MeterId,
    long NumeroMedidor,
    decimal PendingAmount,
    int OverdueInvoices,
    DateOnly? OldestDueDate
);

public sealed record UserDebtSummaryResponseDto(
    Guid UsuarioId,
    decimal TotalPendingAmount,
    int TotalOverdueInvoices,
    IReadOnlyList<UserDebtSummaryMeterDto> ByMeter
);

public sealed record InvoicePayRequestDto(
    [param: Required, Range(typeof(decimal), "0.01", "999999999")] decimal Amount,
    [param: Required, StringLength(30)] string Method,
    string? Reference,
    string? Notes
);

public sealed record InvoicePayResponseDto(
    InvoiceDto Invoice,
    PaymentDto Payment,
    string Message
);

public sealed record PaymentDto(
    Guid Id,
    Guid TenantId,
    Guid InvoiceId,
    Guid MeterId,
    Guid UsuarioId,
    DateOnly PaymentDate,
    decimal Amount,
    string Method,
    string? Reference,
    string Status,
    string? Notes,
    Guid? CreatedBy,
    DateTime CreatedAt
);

public sealed record InvoiceReliquidateRequestDto(
    [param: Required, Range(typeof(decimal), "0.01", "999999999")] decimal DeltaAmount,
    [param: Required, StringLength(500)] string Reason,
    [param: Required, StringLength(30)] string AdjustmentType
);

public sealed record InvoiceReliquidateResponseDto(
    InvoiceAdjustmentDto Adjustment,
    InvoiceDto? GeneratedInvoice,
    string Message
);

public sealed record InvoiceAdjustmentDto(
    Guid Id,
    Guid TenantId,
    Guid MeterId,
    Guid? InvoiceId,
    string AdjustmentType,
    decimal Amount,
    string Reason,
    Guid? SourceReadingId,
    Guid? SourceInvoiceId,
    Guid? LinkedInvoiceId,
    Guid? EffectiveCycleId,
    string Status,
    Guid? CreatedBy,
    DateTime CreatedAt
);

public sealed record AssignOperationalPenaltyRequestDto(
    [param: Required] Guid UsuarioId,
    [param: Required, StringLength(30)] string SourceType,
    [param: Required] DateOnly SourceDate,
    [param: Required, Range(typeof(decimal), "0.01", "999999999")] decimal Amount,
    string? Notes
);
