namespace MengYou.Runtime;

/// <summary>跨会话协调前台窗口与系统键鼠资源。</summary>
public interface IForegroundCoordinator
{
    Task<IDisposable> AcquireAsync(Guid sessionId, CancellationToken ct = default);
}

/// <summary>进程级前台协调器，所有会话共用一个实例。</summary>
public sealed class ForegroundCoordinator : IForegroundCoordinator
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public static IForegroundCoordinator Shared { get; } = new ForegroundCoordinator();

    public async Task<IDisposable> AcquireAsync(Guid sessionId, CancellationToken ct = default)
    {
        await _semaphore.WaitAsync(ct).ConfigureAwait(false);
        return new Releaser(_semaphore);
    }

    private sealed class Releaser : IDisposable
    {
        private SemaphoreSlim? _semaphore;

        public Releaser(SemaphoreSlim semaphore) => _semaphore = semaphore;

        public void Dispose() => Interlocked.Exchange(ref _semaphore, null)?.Release();
    }
}
