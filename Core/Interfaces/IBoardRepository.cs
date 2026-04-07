using System.Data;
using Muzu.Api.Core.Models;

namespace Muzu.Api.Core.Interfaces;

public interface IBoardRepository
{
    Task<IReadOnlyList<Board>> ListByTenantAsync(Guid tenantId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<Board?> GetByIdAsync(Guid boardId, Guid tenantId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<bool> ExistsSlugAsync(Guid tenantId, string slug, Guid? excludingBoardId = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<Board> CreateAsync(Board board, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<Board> UpdateAsync(Board board, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BoardMember>> GetMembersAsync(Guid boardId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task AddMemberAsync(BoardMember member, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<Board?> GetActiveBoardAsync(Guid tenantId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<bool> IsUserInActiveBoardAsync(Guid tenantId, Guid usuarioId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task SetBoardStateAsync(Guid boardId, string estado, DateTime? fechaTransicion, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BoardHistoryItem>> GetHistoryAsync(Guid boardId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task AddHistoryAsync(BoardHistoryItem historyItem, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
}
