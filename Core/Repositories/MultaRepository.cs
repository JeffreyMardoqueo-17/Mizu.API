using System.Data;
using Dapper;
using Muzu.Api.Core.Interfaces;
using Muzu.Api.Core.Models;

namespace Muzu.Api.Core.Repositories;

public sealed class MultaRepository : RepositoryBase, IMultaRepository
{
    public MultaRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory)
    {
    }

    public Task<Multa> CrearMultaAsync(Multa multa, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = """
                           INSERT INTO multas (id, tenant_id, nombre, monto, descripcion)
                           VALUES (@Id, @TenantId, @Nombre, @Monto, @Descripcion)
                           """;

        return WithConnectionAsync(
            transaction,
            async connection =>
            {
                var command = new CommandDefinition(sql, multa, transaction, cancellationToken: cancellationToken);
                await connection.ExecuteAsync(command);
                return multa;
            });
    }

    public Task<IEnumerable<Multa>> ObtenerPorTenantIdAsync(Guid tenantId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = """
                           SELECT id, tenant_id, nombre, monto, descripcion
                           FROM multas
                           WHERE tenant_id = @tenantId
                           """;

        return WithConnectionAsync(
            transaction,
            async connection =>
            {
                var command = new CommandDefinition(sql, new { tenantId }, transaction, cancellationToken: cancellationToken);
                return await connection.QueryAsync<Multa>(command);
            });
    }

    public Task<Multa?> ObtenerPorIdAsync(Guid id, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = """
                           SELECT id, tenant_id, nombre, monto, descripcion
                           FROM multas
                           WHERE id = @id
                           """;

        return WithConnectionAsync(
            transaction,
            async connection =>
            {
                var command = new CommandDefinition(sql, new { id }, transaction, cancellationToken: cancellationToken);
                return await connection.QueryFirstOrDefaultAsync<Multa>(command);
            });
    }

    public Task<bool> ActualizarMultaAsync(Multa multa, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = """
                           UPDATE multas
                           SET nombre = @Nombre,
                               monto = @Monto,
                               descripcion = @Descripcion
                           WHERE id = @Id
                           """;

        return WithConnectionAsync(
            transaction,
            async connection =>
            {
                var command = new CommandDefinition(sql, multa, transaction, cancellationToken: cancellationToken);
                var affectedRows = await connection.ExecuteAsync(command);
                return affectedRows > 0;
            });
    }

    public Task<bool> EliminarMultaAsync(Guid id, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = "DELETE FROM multas WHERE id = @id";

        return WithConnectionAsync(
            transaction,
            async connection =>
            {
                var command = new CommandDefinition(sql, new { id }, transaction, cancellationToken: cancellationToken);
                var affectedRows = await connection.ExecuteAsync(command);
                return affectedRows > 0;
            });
    }
}
