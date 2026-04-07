using Muzu.Api.Core.DTOs;

namespace Muzu.Api.Core.Interfaces.Service;

public interface IAuthSecurityService
{
    Task<bool> ChangeTemporaryPasswordAsync(Guid actorUsuarioId, Guid actorTenantId, string newPassword, CancellationToken cancellationToken = default);
    Task<RegenerateTemporaryPasswordResponseDto> RegenerateTemporaryPasswordAsync(Guid actorUsuarioId, Guid actorTenantId, Guid targetUsuarioId, CancellationToken cancellationToken = default);
    Task<int> InvalidateBoardSessionsAsync(Guid actorUsuarioId, Guid actorTenantId, Guid boardId, CancellationToken cancellationToken = default);
}
