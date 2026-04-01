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
                   INSERT INTO tenant_configs (id, tenant_id, moneda, limite_consumo_fijo, precio_consumo_fijo, limite_consumo_extra1, cargo_extra1, limite_consumo_extra2, cargo_extra2, limite_consumo_extra3, cargo_extra3, cargo_exceso_mayor, tramos_consumo_json, multa_retraso, multa_no_asistir_reunion, multa_no_asistir_trabajo)
                   VALUES (@Id, @TenantId, @Moneda, @LimiteConsumoFijo, @PrecioConsumoFijo, @LimiteConsumoExtra1, @CargoExtra1, @LimiteConsumoExtra2, @CargoExtra2, @LimiteConsumoExtra3, @CargoExtra3, @CargoExcesoMayor, CAST(@TramosConsumoJson AS jsonb), @MultaRetraso, @MultaNoAsistirReunion, @MultaNoAsistirTrabajo)
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
                   SELECT id, tenant_id, moneda, limite_consumo_fijo, precio_consumo_fijo, limite_consumo_extra1, cargo_extra1, limite_consumo_extra2, cargo_extra2, limite_consumo_extra3, cargo_extra3, cargo_exceso_mayor, tramos_consumo_json AS TramosConsumoJson, multa_retraso, multa_no_asistir_reunion, multa_no_asistir_trabajo
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
                               cargo_extra1 = @CargoExtra1,
                               limite_consumo_extra2 = @LimiteConsumoExtra2,
                               cargo_extra2 = @CargoExtra2,
                               limite_consumo_extra3 = @LimiteConsumoExtra3,
                               cargo_extra3 = @CargoExtra3,
                               cargo_exceso_mayor = @CargoExcesoMayor,
                               tramos_consumo_json = CAST(@TramosConsumoJson AS jsonb),
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
