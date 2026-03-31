using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using Muzu.Api.Core.Interfaces;
using Muzu.Api.Core.Models;

namespace Muzu.Api.Core.Repositories
{
    public class RefreshTokenRepository : IRefreshTokenRepository
    {
        public async Task<RefreshToken> CrearAsync(RefreshToken refreshToken)
        {
            using var conn = DbConnectionFactory.Instance.CreateConnection();
            var sql = @"INSERT INTO refresh_tokens (id, usuario_id, token, expira, revokeado, fecha_creacion)
                        VALUES (@Id, @UsuarioId, @Token, @Expira, @Revocado, @FechaCreacion)";
            await conn.ExecuteAsync(sql, refreshToken);
            return refreshToken;
        }

        public async Task<RefreshToken?> ObtenerPorTokenAsync(string token)
        {
            using var conn = DbConnectionFactory.Instance.CreateConnection();
            DefaultTypeMap.MatchNamesWithUnderscores = true;
            var sql = "SELECT id, usuario_id, token, expira, revokeado, fecha_creacion FROM refresh_tokens WHERE token = @token";
            return await conn.QueryFirstOrDefaultAsync<RefreshToken>(sql, new { token });
        }

        public async Task<bool> RevocarAsync(string token)
        {
            using var conn = DbConnectionFactory.Instance.CreateConnection();
            var sql = "UPDATE refresh_tokens SET revokeado = TRUE WHERE token = @token";
            var rows = await conn.ExecuteAsync(sql, new { token });
            return rows > 0;
        }

        public async Task<bool> RevocarTodosDelUsuarioAsync(Guid usuarioId)
        {
            using var conn = DbConnectionFactory.Instance.CreateConnection();
            var sql = "UPDATE refresh_tokens SET revokeado = TRUE WHERE usuario_id = @usuarioId";
            var rows = await conn.ExecuteAsync(sql, new { usuarioId });
            return rows > 0;
        }

        public async Task<IEnumerable<RefreshToken>> ObtenerPorUsuarioIdAsync(Guid usuarioId)
        {
            using var conn = DbConnectionFactory.Instance.CreateConnection();
            DefaultTypeMap.MatchNamesWithUnderscores = true;
            var sql = "SELECT id, usuario_id, token, expira, revokeado, fecha_creacion FROM refresh_tokens WHERE usuario_id = @usuarioId";
            return await conn.QueryAsync<RefreshToken>(sql, new { usuarioId });
        }
    }
}
