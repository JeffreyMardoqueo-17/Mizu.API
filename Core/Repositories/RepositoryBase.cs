using System.Data;
using Npgsql;
using Muzu.Api.Core.Interfaces;

namespace Muzu.Api.Core.Repositories;

public abstract class RepositoryBase
{
    private readonly IDbConnectionFactory _connectionFactory;

    protected RepositoryBase(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    protected async Task<T> WithConnectionAsync<T>(IDbTransaction? transaction, Func<IDbConnection, Task<T>> operation)
    {
        if (transaction?.Connection is not null)
        {
            return await operation(transaction.Connection);
        }

        using var connection = _connectionFactory.CreateConnection();

        try
        {
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }

            return await operation(connection);
        }
        catch (NpgsqlException)
        {
            using var fallback = _connectionFactory.CreateFallbackConnection();
            if (fallback is null)
            {
                throw;
            }

            if (fallback.State != ConnectionState.Open)
            {
                fallback.Open();
            }

            return await operation(fallback);
        }
    }
}
