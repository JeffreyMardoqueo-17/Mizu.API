namespace Muzu.Api.Core.Models;

public sealed class Reunion
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public DateOnly FechaReunion { get; set; }
    public TimeOnly HoraInicio { get; set; }
    public TimeOnly? HoraFin { get; set; }
    public string Estado { get; set; } = "Programada";
    public string PuntosTratarJson { get; set; } = "[]";
    public string? AcuerdosJson { get; set; }
    public string? NotasSecretaria { get; set; }
    public Guid? CreadoPorUsuarioId { get; set; }
    public DateTime? IniciadaAt { get; set; }
    public DateTime? FinalizadaAt { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaActualizacion { get; set; }
}

public sealed class ReunionAsistencia
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ReunionId { get; set; }
    public Guid UsuarioId { get; set; }
    public bool Asistio { get; set; }
    public string? Observacion { get; set; }
    public Guid? MarcadoPorUsuarioId { get; set; }
    public DateTime? FechaMarcado { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaActualizacion { get; set; }
}

public sealed class ReunionHistorial
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ReunionId { get; set; }
    public string Evento { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public Guid? ActorUsuarioId { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
}

public sealed class ReunionSocioSnapshot
{
    public Guid UsuarioId { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
    public string DUI { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
}