using System;
using System.Threading.Tasks;
using Dapper;
using Muzu.Api.Core.Interfaces;
using Muzu.Api.Core.Models;

namespace Muzu.Api.Core.Repositories
{
    public class TenantRepository : ITenantRepository
    {
        public async Task<Tenant> CrearTenantAsync(Tenant tenant)
        {
            using var conn = DbConnectionFactory.Instance.CreateConnection();
            var sql = @"INSERT INTO tenants (id, nombre, direccion, logo_url, fecha_creacion)
                        VALUES (@Id, @Nombre, @Direccion, @LogoUrl, @FechaCreacion)";
            await conn.ExecuteAsync(sql, tenant);
            return tenant;
        }

        public async Task<Tenant?> ObtenerPorNombreAsync(string nombre)
        {
            using var conn = DbConnectionFactory.Instance.CreateConnection();
            var sql = "SELECT * FROM tenants WHERE nombre = @nombre";
            return await conn.QueryFirstOrDefaultAsync<Tenant>(sql, new { nombre });
        }
    }
}
