using System.Data;
using Dapper;
using Muzu.Api.Core.Interfaces;
using Muzu.Api.Core.Models;

namespace Muzu.Api.Core.Repositories;

public sealed class RefreshTokenRepository : RepositoryBase, IRefreshTokenRepository
{
    public RefreshTokenRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory)
    {
    }

    public Task<RefreshToken> CrearAsync(RefreshToken refreshToken, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = """
                           INSERT INTO refresh_tokens (id, usuario_id, token, expira, revokeado, fecha_creacion)
                           VALUES (@Id, @UsuarioId, @Token, @Expira, @Revocado, @FechaCreacion)
                           """;

        return WithConnectionAsync(
            transaction,
            async connection =>
            {
                var command = new CommandDefinition(sql, refreshToken, transaction, cancellationToken: cancellationToken);
                await connection.ExecuteAsync(command);
                return refreshToken;
            });
    }

    public Task<RefreshToken?> ObtenerPorTokenAsync(string token, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = """
                           SELECT id,
                                  usuario_id,
                                  token,
                                  expira,
                                  revokeado AS revocado,
                                  fecha_creacion
                           FROM refresh_tokens
                           WHERE token = @token
                           """;

        return WithConnectionAsync(
            transaction,
            async connection =>
            {
                var command = new CommandDefinition(sql, new { token }, transaction, cancellationToken: cancellationToken);
                return await connection.QueryFirstOrDefaultAsync<RefreshToken>(command);
            });
    }

    public Task<bool> RevocarAsync(string token, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = "UPDATE refresh_tokens SET revokeado = TRUE WHERE token = @token";

        return WithConnectionAsync(
            transaction,
            async connection =>
            {
                var command = new CommandDefinition(sql, new { token }, transaction, cancellationToken: cancellationToken);
                var affectedRows = await connection.ExecuteAsync(command);
                return affectedRows > 0;
            });
    }

    public Task<bool> RevocarTodosDelUsuarioAsync(Guid usuarioId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = "UPDATE refresh_tokens SET revokeado = TRUE WHERE usuario_id = @usuarioId";

        return WithConnectionAsync(
            transaction,
            async connection =>
            {
                var command = new CommandDefinition(sql, new { usuarioId }, transaction, cancellationToken: cancellationToken);
                var affectedRows = await connection.ExecuteAsync(command);
                return affectedRows > 0;
            });
    }

    public Task<IEnumerable<RefreshToken>> ObtenerPorUsuarioIdAsync(Guid usuarioId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = """
                           SELECT id,
                                  usuario_id,
                                  token,
                                  expira,
                                  revokeado AS revocado,
                                  fecha_creacion
                           FROM refresh_tokens
                           WHERE usuario_id = @usuarioId
                           """;

        return WithConnectionAsync(
            transaction,
            async connection =>
            {
                var command = new CommandDefinition(sql, new { usuarioId }, transaction, cancellationToken: cancellationToken);
                return await connection.QueryAsync<RefreshToken>(command);
            });
    }
}
