using System.ComponentModel.DataAnnotations;

namespace Muzu.Api.Core.DTOs;

public sealed record ChangeTemporaryPasswordRequestDto(
    [param: Required, MinLength(8), StringLength(200)] string NewPassword
);

public sealed record RegenerateTemporaryPasswordRequestDto(
    [param: Required] Guid UsuarioId
);

public sealed record RegenerateTemporaryPasswordResponseDto(
    Guid UsuarioId,
    string TemporaryPassword
);

public sealed record InvalidateBoardSessionsRequestDto(
    [param: Required] Guid BoardId
);
