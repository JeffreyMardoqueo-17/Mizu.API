using System.Data;

namespace Muzu.Api.Core.Interfaces;

public interface IUnitOfWork
{
    Task ExecuteInTransactionAsync(Func<IDbTransaction, Task> operation, CancellationToken cancellationToken = default);
    Task<T> ExecuteInTransactionAsync<T>(Func<IDbTransaction, Task<T>> operation, CancellationToken cancellationToken = default);
}
