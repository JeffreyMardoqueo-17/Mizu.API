using Muzu.Api.Core.DTOs;

namespace Muzu.Api.Core.Interfaces.Service;

public interface IBoardsService
{
    Task<IReadOnlyList<BoardListItemDto>> GetBoardsAsync(Guid actorUsuarioId, Guid actorTenantId, CancellationToken cancellationToken = default);
    Task<BoardDetailDto?> GetBoardByIdAsync(Guid actorUsuarioId, Guid actorTenantId, Guid boardId, CancellationToken cancellationToken = default);
    Task<BoardDetailDto> CreateBoardAsync(Guid actorUsuarioId, Guid actorTenantId, CreateBoardRequestDto request, CancellationToken cancellationToken = default);
    Task<BoardDetailDto?> UpdateBoardAsync(Guid actorUsuarioId, Guid actorTenantId, Guid boardId, UpdateBoardRequestDto request, CancellationToken cancellationToken = default);
    Task<BoardDetailDto?> AddMemberAsync(Guid actorUsuarioId, Guid actorTenantId, Guid boardId, AddBoardMemberRequestDto request, CancellationToken cancellationToken = default);
    Task<BoardActivationResponseDto?> ActivateBoardAsync(Guid actorUsuarioId, Guid actorTenantId, Guid boardId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BoardHistoryItemDto>> GetBoardHistoryAsync(Guid actorUsuarioId, Guid actorTenantId, Guid boardId, CancellationToken cancellationToken = default);
}
