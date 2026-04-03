using System.Collections.Concurrent;
using Muzu.Api.Core.Interfaces.Service;

namespace Muzu.Api.Core.Services;

public sealed class RoleMutationGuard : IRoleMutationGuard
{
    private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _tenantSemaphores = new();
    private readonly ConcurrentDictionary<Guid, int> _activeWindows = new();

    public bool IsRoleMutationBlocked(Guid tenantId)
    {
        return _activeWindows.ContainsKey(tenantId);
    }

    public async Task<IAsyncDisposable> EnterRoleMutationWindowAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var semaphore = _tenantSemaphores.GetOrAdd(tenantId, _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync(cancellationToken);

        _activeWindows.AddOrUpdate(tenantId, 1, (_, current) => current + 1);
        return new Releaser(this, tenantId, semaphore);
    }

    private sealed class Releaser : IAsyncDisposable
    {
        private readonly RoleMutationGuard _owner;
        private readonly Guid _tenantId;
        private readonly SemaphoreSlim _semaphore;
        private bool _released;

        public Releaser(RoleMutationGuard owner, Guid tenantId, SemaphoreSlim semaphore)
        {
            _owner = owner;
            _tenantId = tenantId;
            _semaphore = semaphore;
        }

        public ValueTask DisposeAsync()
        {
            if (_released)
            {
                return ValueTask.CompletedTask;
            }

            _released = true;

            _owner._activeWindows.AddOrUpdate(_tenantId, 0, (_, current) => Math.Max(0, current - 1));
            if (_owner._activeWindows.TryGetValue(_tenantId, out var currentCount) && currentCount == 0)
            {
                _owner._activeWindows.TryRemove(_tenantId, out _);
            }

            _semaphore.Release();
            return ValueTask.CompletedTask;
        }
    }
}
