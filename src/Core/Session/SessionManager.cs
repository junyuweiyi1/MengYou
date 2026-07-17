/// <summary>
/// 单会话宿主。当前版本刻意只允许挂载一个 GameSession，避免多窗口调度复杂度进入业务层。
/// </summary>
public sealed class SessionManager : IAsyncDisposable, IDisposable
{
    private readonly object _sync = new();
    private readonly Logger _logger = new(nameof(SessionManager));
    private IGameSession? _current;
    private bool _disposed;

    public IGameSession? Current
    {
        get { lock (_sync) return _current; }
    }

    /// <summary>为兼容现有调用保留集合视图，元素数量只可能是 0 或 1。</summary>
    public IReadOnlyCollection<IGameSession> All
    {
        get
        {
            lock (_sync)
                return _current is null ? Array.Empty<IGameSession>() : new[] { _current };
        }
    }

    public IGameSession Create(SessionData config)
    {
        ArgumentNullException.ThrowIfNull(config);
        lock (_sync)
        {
            ThrowIfDisposed();
            if (_current is not null)
                throw new InvalidOperationException("当前版本只支持一个 GameSession，请先 RemoveAsync 当前会话。");

            _current = new GameSession(config);
            _logger.LogFormat("SessionManager: 已挂载 {0}", _current.DisplayName);
            return _current;
        }
    }

    public async Task RemoveAsync(Guid id)
    {
        IGameSession? session;
        lock (_sync)
        {
            if (_current?.SessionId != id) return;
            session = _current;
            _current = null;
        }

        await session.StopAsync().ConfigureAwait(false);
        session.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        IGameSession? session;
        lock (_sync)
        {
            if (_disposed) return;
            _disposed = true;
            session = _current;
            _current = null;
        }

        if (session is not null)
        {
            await session.StopAsync().ConfigureAwait(false);
            session.Dispose();
        }
    }

    public void Dispose() => DisposeAsync().AsTask().GetAwaiter().GetResult();

    private void ThrowIfDisposed()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(SessionManager));
    }
}
