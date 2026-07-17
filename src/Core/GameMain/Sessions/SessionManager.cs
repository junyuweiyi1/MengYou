using System.Collections.Concurrent;
using MengYou.Runtime;


/// <summary>
/// 会话管理器：管理所有已挂载的游戏窗口 Session，负责创建/查询/停止。
/// 同时负责"前台锁"调度：确保同一时刻只有一个 Session 走前台通道。
/// </summary>
public sealed class SessionManager : IDisposable, IAsyncDisposable
{
    /// <summary>所有会话表：Key=SessionId。</summary>
    private readonly ConcurrentDictionary<Guid, IGameSession> _sessions = new();

    /// <summary>日志。</summary>
    private readonly Logger _logger;

    /// <summary>所有会话共享的前台操作协调器。</summary>
    private readonly IForegroundCoordinator _foregroundCoordinator;

    private int _disposed;

    /// <summary>构造。</summary>
    public SessionManager(IForegroundCoordinator? foregroundCoordinator = null)
    {
        _logger = new Logger(GetType().Name);
        _foregroundCoordinator = foregroundCoordinator ?? ForegroundCoordinator.Shared;
    }

    /// <summary>获取所有 Session 快照。</summary>
    public IReadOnlyCollection<IGameSession> All => _sessions.Values.ToArray();

    /// <summary>创建并注册一个新的 Session。</summary>
    public IGameSession Create(SessionData config)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
        var session = new GameSession(config, _foregroundCoordinator);
        _sessions[session.SessionId] = session;
        _logger.LogFormat("SessionManager: 已挂载 {0}", session.DisplayName);
        return session;
    }

    /// <summary>移除并释放某 Session。</summary>
    public async Task RemoveAsync(Guid id, CancellationToken ct = default)
    {
        if (_sessions.TryRemove(id, out var s))
        {
            await s.StopAsync(ct).ConfigureAwait(false);
            s.Dispose();
        }
    }

    /// <summary>停止并释放当前管理器中的所有会话。</summary>
    public async Task StopAllAsync(CancellationToken ct = default)
    {
        foreach (var id in _sessions.Keys.ToArray())
        {
            ct.ThrowIfCancellationRequested();
            await RemoveAsync(id, ct).ConfigureAwait(false);
        }
    }

    /// <summary>获取前台锁；使用完必须 Dispose 释放。</summary>
    public async Task<IDisposable> AcquireForegroundLockAsync(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
        return await _foregroundCoordinator
            .AcquireAsync(Guid.Empty, ct)
            .ConfigureAwait(false);
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        StopAllAsync().GetAwaiter().GetResult();
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        await StopAllAsync().ConfigureAwait(false);
    }
}
