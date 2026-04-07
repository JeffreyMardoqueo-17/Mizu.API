using System.ComponentModel.DataAnnotations;

namespace Muzu.Api.Core.DTOs;

public sealed record BoardMemberDto(
    Guid UsuarioId,
    string NombreCompleto,
    string DUI,
    string Rol,
    DateTime FechaCreacion
);

public sealed record BoardHistoryItemDto(
    Guid Id,
    Guid BoardId,
    string Evento,
    string Descripcion,
    Guid? ActorUsuarioId,
    DateTime FechaCreacion
);

public sealed record BoardDetailDto(
    Guid Id,
    Guid TenantId,
    string Nombre,
    string Slug,
    DateOnly FechaInicio,
    DateOnly FechaFin,
    string Estado,
    Guid AdministradorResponsableId,
    DateTime FechaCreacion,
    DateTime? FechaActualizacion,
    DateTime? FechaTransicion,
    IReadOnlyList<BoardMemberDto> Miembros
);

public sealed record BoardListItemDto(
    Guid Id,
    string Nombre,
    string Slug,
    DateOnly FechaInicio,
    DateOnly FechaFin,
    string Estado,
    DateTime FechaCreacion
);

public sealed record CreateBoardRequestDto(
    [param: Required, StringLength(120)] string Nombre,
    [param: Required] DateOnly FechaInicio,
    [param: Required] DateOnly FechaFin,
    [param: Required] Guid AdministradorResponsableId
);

public sealed record UpdateBoardRequestDto(
    [param: Required, StringLength(120)] string Nombre,
    [param: Required] DateOnly FechaInicio,
    [param: Required] DateOnly FechaFin,
    [param: Required] Guid AdministradorResponsableId
);

public sealed record AddBoardMemberRequestDto(
    [param: Required] Guid UsuarioId,
    [param: Required, StringLength(50)] string Rol
);

public sealed record BoardActivationResponseDto(
    Guid BoardId,
    string Estado,
    DateTime FechaTransicion
);
