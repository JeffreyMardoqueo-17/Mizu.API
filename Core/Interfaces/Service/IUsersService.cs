using Muzu.Api.Core.DTOs;

namespace Muzu.Api.Core.Interfaces.Service;

public interface IUsersService
{
    Task<UserPaginatedResponseDto> GetUsersAsync(
        Guid actorUsuarioId,
        Guid actorTenantId,
        int page,
        int pageSize,
        string? dui,
        string? nombre,
        string? correo,
        bool? estado,
        CancellationToken cancellationToken = default);

    Task<UserCreateResponseDto> CreateUserAsync(
        Guid actorUsuarioId,
        Guid actorTenantId,
        CreateUserRequestDto request,
        CancellationToken cancellationToken = default);

    Task<UserDetailDto?> GetUserByIdAsync(Guid actorUsuarioId, Guid actorTenantId, Guid userId, CancellationToken cancellationToken = default);
    Task<UserDetailDto?> UpdateUserAsync(Guid actorUsuarioId, Guid actorTenantId, Guid userId, UpdateUserRequestDto request, CancellationToken cancellationToken = default);
    Task<bool> SoftDeleteUserAsync(Guid actorUsuarioId, Guid actorTenantId, Guid userId, CancellationToken cancellationToken = default);
}
