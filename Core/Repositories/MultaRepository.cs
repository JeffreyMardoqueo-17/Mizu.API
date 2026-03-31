using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using Muzu.Api.Core.Interfaces;
using Muzu.Api.Core.Models;

namespace Muzu.Api.Core.Repositories
{
    public class MultaRepository : IMultaRepository
    {
        public async Task<Multa> CrearMultaAsync(Multa multa)
        {
            using var conn = DbConnectionFactory.Instance.CreateConnection();
            var sql = @"INSERT INTO multas (id, tenant_id, nombre, monto, descripcion)
                        VALUES (@Id, @TenantId, @Nombre, @Monto, @Descripcion)";
            await conn.ExecuteAsync(sql, multa);
            return multa;
        }

        public async Task<IEnumerable<Multa>> ObtenerPorTenantIdAsync(Guid tenantId)
        {
            using var conn = DbConnectionFactory.Instance.CreateConnection();
            var sql = "SELECT * FROM multas WHERE tenant_id = @tenantId";
            return await conn.QueryAsync<Multa>(sql, new { tenantId });
        }

        public async Task<Multa?> ObtenerPorIdAsync(Guid id)
        {
            using var conn = DbConnectionFactory.Instance.CreateConnection();
            var sql = "SELECT * FROM multas WHERE id = @id";
            return await conn.QueryFirstOrDefaultAsync<Multa>(sql, new { id });
        }

        public async Task<bool> ActualizarMultaAsync(Multa multa)
        {
            using var conn = DbConnectionFactory.Instance.CreateConnection();
            var sql = @"UPDATE multas SET nombre = @Nombre, monto = @Monto, descripcion = @Descripcion WHERE id = @Id";
            var rows = await conn.ExecuteAsync(sql, multa);
            return rows > 0;
        }

        public async Task<bool> EliminarMultaAsync(Guid id)
        {
            using var conn = DbConnectionFactory.Instance.CreateConnection();
            var sql = "DELETE FROM multas WHERE id = @id";
            var rows = await conn.ExecuteAsync(sql, new { id });
            return rows > 0;
        }
    }
}
