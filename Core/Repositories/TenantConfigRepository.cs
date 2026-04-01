using System.Data;
using Dapper;
using Muzu.Api.Core.Interfaces;
using Muzu.Api.Core.Models;

namespace Muzu.Api.Core.Repositories;

public sealed class TenantConfigRepository : RepositoryBase, ITenantConfigRepository
{
    public TenantConfigRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory)
    {
    }

    public Task<TenantConfig> CrearConfigAsync(TenantConfig config, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = """
                           INSERT INTO tenant_configs (id, tenant_id, moneda, limite_consumo_fijo, precio_consumo_fijo, limite_consumo_extra1, porcentaje_extra1, limite_consumo_extra2, porcentaje_extra2, multa_retraso, multa_no_asistir_reunion, multa_no_asistir_trabajo)
                           VALUES (@Id, @TenantId, @Moneda, @LimiteConsumoFijo, @PrecioConsumoFijo, @LimiteConsumoExtra1, @PorcentajeExtra1, @LimiteConsumoExtra2, @PorcentajeExtra2, @MultaRetraso, @MultaNoAsistirReunion, @MultaNoAsistirTrabajo)
                           """;

        return WithConnectionAsync(
            transaction,
            async connection =>
            {
                var command = new CommandDefinition(sql, config, transaction, cancellationToken: cancellationToken);
                await connection.ExecuteAsync(command);
                return config;
            });
    }

    public Task<TenantConfig?> ObtenerPorTenantIdAsync(Guid tenantId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = """
                           SELECT id, tenant_id, moneda, limite_consumo_fijo, precio_consumo_fijo, limite_consumo_extra1, porcentaje_extra1, limite_consumo_extra2, porcentaje_extra2, multa_retraso, multa_no_asistir_reunion, multa_no_asistir_trabajo
                           FROM tenant_configs
                           WHERE tenant_id = @tenantId
                           """;

        return WithConnectionAsync(
            transaction,
            async connection =>
            {
                var command = new CommandDefinition(sql, new { tenantId }, transaction, cancellationToken: cancellationToken);
                return await connection.QueryFirstOrDefaultAsync<TenantConfig>(command);
            });
    }

    public Task<bool> ActualizarAsync(TenantConfig config, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = """
                           UPDATE tenant_configs
                           SET moneda = @Moneda,
                               limite_consumo_fijo = @LimiteConsumoFijo,
                               precio_consumo_fijo = @PrecioConsumoFijo,
                               limite_consumo_extra1 = @LimiteConsumoExtra1,
                               porcentaje_extra1 = @PorcentajeExtra1,
                               limite_consumo_extra2 = @LimiteConsumoExtra2,
                               porcentaje_extra2 = @PorcentajeExtra2,
                               multa_retraso = @MultaRetraso,
                               multa_no_asistir_reunion = @MultaNoAsistirReunion,
                               multa_no_asistir_trabajo = @MultaNoAsistirTrabajo
                           WHERE tenant_id = @TenantId
                           """;

        return WithConnectionAsync(
            transaction,
            async connection =>
            {
                var command = new CommandDefinition(sql, config, transaction, cancellationToken: cancellationToken);
                var affectedRows = await connection.ExecuteAsync(command);
                return affectedRows > 0;
            });
    }
}
