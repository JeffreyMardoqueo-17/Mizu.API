using System;

namespace Muzu.Api.Core.Models;

public static class BillingStatuses
{
    public const string Abierto = "abierto";
    public const string Borrador = "borrador";
    public const string Emitido = "emitido";
    public const string Pendiente = "pendiente";
    public const string Vencido = "vencido";
    public const string ParcialmentePagado = "parcialmente_pagado";
    public const string Pagado = "pagado";
    public const string Anulado = "anulado";
    public const string Reliquidado = "reliquidado";
}

public sealed class BillingCycle
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public string PeriodCode { get; set; } = string.Empty;
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public DateOnly DueDate { get; set; }
    public DateOnly IssueDate { get; set; }
    public string Frequency { get; set; } = "monthly";
    public string Status { get; set; } = BillingStatuses.Abierto;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ClosedAt { get; set; }
}

public sealed class MeterReading
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid MeterId { get; set; }
    public Guid BillingCycleId { get; set; }
    public DateOnly ReadAt { get; set; }
    public decimal PreviousReading { get; set; }
    public decimal CurrentReading { get; set; }
    public decimal ConsumptionM3 { get; set; }
    public string Source { get; set; } = "manual";
    public string? Notes { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public sealed class Invoice
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid MeterId { get; set; }
    public Guid UsuarioId { get; set; }
    public Guid BillingCycleId { get; set; }
    public Guid MeterReadingId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string Status { get; set; } = BillingStatuses.Borrador;
    public string Currency { get; set; } = "USD";
    public decimal Subtotal { get; set; }
    public decimal PreviousBalance { get; set; }
    public decimal LateFeeAmount { get; set; }
    public decimal OperationalPenaltyAmount { get; set; }
    public decimal AdjustmentsAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal PendingAmount { get; set; }
    public DateTime? IssuedAt { get; set; }
    public DateOnly DueDate { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public Guid? ReliquidatedFromInvoiceId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public sealed class InvoiceLine
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid InvoiceId { get; set; }
    public string LineType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal Amount { get; set; }
    public string? ReferenceTable { get; set; }
    public Guid? ReferenceId { get; set; }
    public string? Metadata { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class Payment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid InvoiceId { get; set; }
    public Guid MeterId { get; set; }
    public Guid UsuarioId { get; set; }
    public DateOnly PaymentDate { get; set; }
    public decimal Amount { get; set; }
    public string Method { get; set; } = string.Empty;
    public string? Reference { get; set; }
    public string Status { get; set; } = "aprobado";
    public string? Notes { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class LateFeeHistory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid MeterId { get; set; }
    public Guid SourceInvoiceId { get; set; }
    public Guid TargetInvoiceId { get; set; }
    public decimal Amount { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public string? RuleSnapshot { get; set; }
}

public sealed class OperationalPenalty
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid UsuarioId { get; set; }
    public string SourceType { get; set; } = string.Empty;
    public DateOnly SourceDate { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = "pendiente";
    public string AssignmentStrategy { get; set; } = "primary_meter";
    public Guid? AssignedMeterId { get; set; }
    public Guid? AssignedInvoiceId { get; set; }
    public DateTime? AssignedAt { get; set; }
    public string? Notes { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class CarriedBalance
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid MeterId { get; set; }
    public Guid SourceInvoiceId { get; set; }
    public Guid TargetInvoiceId { get; set; }
    public decimal Amount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class InvoiceAdjustment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid MeterId { get; set; }
    public Guid? InvoiceId { get; set; }
    public string AdjustmentType { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public Guid? SourceReadingId { get; set; }
    public Guid? SourceInvoiceId { get; set; }
    public Guid? LinkedInvoiceId { get; set; }
    public Guid? EffectiveCycleId { get; set; }
    public string Status { get; set; } = "aplicado";
    public Guid? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
