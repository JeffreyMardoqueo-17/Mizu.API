namespace Muzu.Api.Core.Interfaces.Service;

public interface IRoleMutationGuard
{
    bool IsRoleMutationBlocked(Guid tenantId);
    Task<IAsyncDisposable> EnterRoleMutationWindowAsync(Guid tenantId, CancellationToken cancellationToken = default);
}
