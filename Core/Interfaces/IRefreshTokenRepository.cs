using System.Data;
using Muzu.Api.Core.Models;

namespace Muzu.Api.Core.Interfaces;

public interface IRefreshTokenRepository
{
    Task<RefreshToken> CrearAsync(RefreshToken refreshToken, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<RefreshToken?> ObtenerPorTokenAsync(string token, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<bool> RevocarAsync(string token, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<bool> RevocarTodosDelUsuarioAsync(Guid usuarioId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<RefreshToken>> ObtenerPorUsuarioIdAsync(Guid usuarioId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
}
