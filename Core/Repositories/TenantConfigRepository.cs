using System;
using System.Threading.Tasks;
using Dapper;
using Muzu.Api.Core.Interfaces;
using Muzu.Api.Core.Models;

namespace Muzu.Api.Core.Repositories
{
    public class TenantConfigRepository : ITenantConfigRepository
    {
        public async Task<TenantConfig> CrearConfigAsync(TenantConfig config)
        {
            using var conn = DbConnectionFactory.Instance.CreateConnection();
            var sql = @"INSERT INTO tenant_configs (id, tenant_id, moneda, limite_consumo_fijo, precio_consumo_fijo, limite_consumo_extra1, porcentaje_extra1, limite_consumo_extra2, porcentaje_extra2, multa_retraso, multa_no_asistir_reunion, multa_no_asistir_trabajo)
                        VALUES (@Id, @TenantId, @Moneda, @LimiteConsumoFijo, @PrecioConsumoFijo, @LimiteConsumoExtra1, @PorcentajeExtra1, @LimiteConsumoExtra2, @PorcentajeExtra2, @MultaRetraso, @MultaNoAsistirReunion, @MultaNoAsistirTrabajo)";
            await conn.ExecuteAsync(sql, config);
            return config;
        }

        public async Task<TenantConfig?> ObtenerPorTenantIdAsync(Guid tenantId)
        {
            using var conn = DbConnectionFactory.Instance.CreateConnection();
            var sql = "SELECT * FROM tenant_configs WHERE tenant_id = @tenantId";
            return await conn.QueryFirstOrDefaultAsync<TenantConfig>(sql, new { tenantId });
        }
    }
}
