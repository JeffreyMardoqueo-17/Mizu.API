using System.Data;
using Muzu.Api.Core.Interfaces;

namespace Muzu.Api.Core.Repositories;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly IDbConnectionFactory _connectionFactory;

    public UnitOfWork(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public Task ExecuteInTransactionAsync(Func<IDbTransaction, Task> operation, CancellationToken cancellationToken = default)
    {
        return ExecuteInTransactionAsync<object?>(
            async transaction =>
            {
                await operation(transaction);
                return null;
            },
            cancellationToken);
    }

    public async Task<T> ExecuteInTransactionAsync<T>(Func<IDbTransaction, Task<T>> operation, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        if (connection.State != ConnectionState.Open)
            connection.Open();
        

        using var transaction = connection.BeginTransaction();

        try
        {
            var result = await operation(transaction);
            transaction.Commit();
            return result;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }
}
