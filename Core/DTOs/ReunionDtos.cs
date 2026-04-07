using System.ComponentModel.DataAnnotations;

namespace Muzu.Api.Core.DTOs;

public sealed record ReunionAttendanceDto(
    Guid UsuarioId,
    string NombreCompleto,
    string DUI,
    string Rol,
    bool Asistio,
    string? Observacion,
    Guid? MarcadoPorUsuarioId,
    DateTime? FechaMarcado
);

public sealed record ReunionHistoryItemDto(
    Guid Id,
    Guid ReunionId,
    string Evento,
    string Descripcion,
    Guid? ActorUsuarioId,
    DateTime FechaCreacion
);

public sealed record ReunionListItemDto(
    Guid Id,
    string Titulo,
    DateOnly FechaReunion,
    TimeOnly HoraInicio,
    TimeOnly? HoraFin,
    string Estado,
    int TotalSocios,
    int TotalPresentes,
    int TotalAusentes,
    DateTime FechaCreacion,
    DateTime? FinalizadaAt
);

public sealed record ReunionDetailDto(
    Guid Id,
    Guid TenantId,
    string Titulo,
    DateOnly FechaReunion,
    TimeOnly HoraInicio,
    TimeOnly? HoraFin,
    string Estado,
    IReadOnlyList<string> PuntosTratar,
    IReadOnlyList<string> Acuerdos,
    string? NotasSecretaria,
    Guid? CreadoPorUsuarioId,
    DateTime? IniciadaAt,
    DateTime? FinalizadaAt,
    DateTime FechaCreacion,
    DateTime? FechaActualizacion,
    IReadOnlyList<ReunionAttendanceDto> Asistencia,
    IReadOnlyList<ReunionHistoryItemDto> Historial
);

public sealed record CreateReunionRequestDto(
    [param: Required, StringLength(180)] string Titulo,
    [param: Required] DateOnly FechaReunion,
    [param: Required] TimeOnly HoraInicio,
    [param: Required, MinLength(1)] IReadOnlyList<string> PuntosTratar
);

public sealed record UpdateReunionRequestDto(
    [param: Required, StringLength(180)] string Titulo,
    [param: Required] DateOnly FechaReunion,
    [param: Required] TimeOnly HoraInicio,
    TimeOnly? HoraFin,
    [param: Required, MinLength(1)] IReadOnlyList<string> PuntosTratar
);

public sealed record UpdateReunionAsistenciaItemDto(
    [param: Required] Guid UsuarioId,
    bool Asistio,
    string? Observacion
);

public sealed record UpdateReunionAsistenciaRequestDto(
    [param: Required, MinLength(1)] IReadOnlyList<UpdateReunionAsistenciaItemDto> Asistencias
);

public sealed record UpdateReunionAcuerdosRequestDto(
    [param: Required] IReadOnlyList<string> Acuerdos,
    string? NotasSecretaria
);

public sealed record FinalizeReunionRequestDto(
    TimeOnly? HoraFin
);

public sealed record StartReunionRequestDto();