namespace Muzu.Api.Core.DTOs;

public sealed record MeterDto(
    Guid Id,
    Guid TenantId,
    Guid UsuarioId,
    long Niu,
    long NumeroMedidor,
    bool Activo,
    DateTime FechaCreacion,
    DateTime? FechaActualizacion
);

public sealed record MeterListResponseDto(
    Guid UsuarioId,
    long Niu,
    IReadOnlyList<MeterDto> Items,
    int Total,
    int TotalActivos,
    bool PermitirMultiplesContadores,
    int MaximoContadoresPorUsuario,
    int MaximoActivosPermitidos,
    bool ExcedeReglaActivos,
    bool PuedeAgregarOtro
);

public sealed record MeterNextNumberResponseDto(
    Guid UsuarioId,
    long Niu,
    long NextNumber
);

public sealed record MeterAssignRequestDto(
    Guid UsuarioId
);

public sealed record MeterAssignResponseDto(
    MeterDto Meter,
    string Message
);

public sealed record MeterStatusUpdateRequestDto(
    bool Activo
);

public sealed record MeterStatusResponseDto(
    MeterDto Meter,
    string Message
);

public sealed record MeterRuleConflictItemDto(
    Guid UsuarioId,
    string Nombre,
    string Apellido,
    string Correo,
    int TotalActivos,
    IReadOnlyList<long> NumerosMedidoresActivos
);

public sealed record MeterRuleConflictReportDto(
    bool ReglaMultiplesActiva,
    int MaximoActivosPermitidos,
    IReadOnlyList<MeterRuleConflictItemDto> UsuariosEnConflicto,
    int TotalUsuariosEnConflicto
);
