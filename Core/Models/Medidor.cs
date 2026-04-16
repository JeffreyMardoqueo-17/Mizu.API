namespace Muzu.Api.Core.Models;

/// <summary>
/// Medidor (paja) asignado a un socio.
/// El numero del medidor es correlativo por tenant.
/// </summary>
public class Medidor
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid UsuarioId { get; set; }
    public long NumeroMedidor { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaActualizacion { get; set; }
    public bool Eliminado { get; set; }
}

public sealed class MedidorTransferencia
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid MedidorId { get; set; }
    public Guid UsuarioOrigenId { get; set; }
    public Guid UsuarioDestinoId { get; set; }
    public string TipoMovimiento { get; set; } = string.Empty;
    public string Motivo { get; set; } = string.Empty;
    public string? Observaciones { get; set; }
    public string? ReferenciaDocumento { get; set; }
    public Guid? ActorUsuarioId { get; set; }
    public DateTime FechaTransferencia { get; set; } = DateTime.UtcNow;
}

public sealed class MeterRuleConflictRow
{
    public Guid UsuarioId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public int TotalActivos { get; set; }
    public long[] NumerosMedidoresActivos { get; set; } = [];
}
