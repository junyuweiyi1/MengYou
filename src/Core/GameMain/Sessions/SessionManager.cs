using System.Collections.Concurrent;


/// <summary>
/// 会话管理器：管理所有已挂载的游戏窗口 Session，负责创建/查询/停止。
/// 同时负责"前台锁"调度：确保同一时刻只有一个 Session 走前台通道。
/// </summary>
public sealed class SessionManager
{
    /// <summary>所有会话表：Key=SessionId。</summary>
    private readonly ConcurrentDictionary<Guid, IGameSession> _sessions = new();

    /// <summary>根 DI 容器。</summary>
    private readonly IServiceProvider _rootProvider;

    /// <summary>日志。</summary>
    private readonly Logger _logger;

    /// <summary>前台操作互斥锁：仅前台模式使用。</summary>
    private readonly SemaphoreSlim _foregroundLock = new(1, 1);

    /// <summary>构造。</summary>
    public SessionManager(IServiceProvider rootProvider)
    {
        _rootProvider = rootProvider;
        _logger = new Logger(GetType().Name);
    }

    /// <summary>获取所有 Session 快照。</summary>
    public IReadOnlyCollection<IGameSession> All => _sessions.Values.ToArray();

    /// <summary>创建并注册一个新的 Session。</summary>
    public IGameSession Create(SessionData config)
    {
        var session = new GameSession(config, _rootProvider);
        _sessions[session.SessionId] = session;
        _logger.LogFormat("SessionManager: 已挂载 {0}", session.DisplayName);
        return session;
    }

    /// <summary>移除并释放某 Session。</summary>
    public async Task RemoveAsync(Guid id)
    {
        if (_sessions.TryRemove(id, out var s))
        {
            await s.StopAsync();
            s.Dispose();
        }
    }

    /// <summary>获取前台锁；使用完必须 Dispose 释放。</summary>
    public async Task<IDisposable> AcquireForegroundLockAsync(CancellationToken ct = default)
    {
        await _foregroundLock.WaitAsync(ct);
        return new Releaser(_foregroundLock);
    }

    /// <summary>信号量释放句柄。</summary>
    private sealed class Releaser : IDisposable
    {
        /// <summary>持有的信号量。</summary>
        private readonly SemaphoreSlim _sem;

        /// <summary>是否已释放。</summary>
        private bool _released;

        /// <summary>构造。</summary>
        public Releaser(SemaphoreSlim sem) => _sem = sem;

        /// <summary>释放锁。</summary>
        public void Dispose()
        {
            if (_released) return;
            _released = true;
            _sem.Release();
        }
    }
}
