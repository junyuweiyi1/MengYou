/// <summary>
/// 游戏会话默认实现：拥有独立 DI 子容器，是多开隔离的基石。
/// </summary>
public sealed class GameSession : IGameSession
{
    /// <summary>会话内部取消令牌源。</summary>
    private readonly CancellationTokenSource _cts = new();

    /// <summary>日志。</summary>
    private readonly Logger _logger;

    /// <summary>创建会话。</summary>
    /// <param name="data">会话数据。</param>
    /// <param name="rootProvider">根 DI 容器，用于创建 Scope。</param>
    public GameSession(SessionData data, IServiceProvider rootProvider)
    {
        _logger = new Logger(GetType().Name);
        SessionId = Guid.NewGuid();
        WindowHandle = data.WindowHandle;
        ProcessId = data.ProcessId;
        DisplayName = data.DisplayName;

        _logger.LogFormat("Session {0} 已创建：{1} (HWND={2})", SessionId, DisplayName, WindowHandle);
    }

    /// <inheritdoc/>
    public Guid SessionId { get; }

    /// <inheritdoc/>
    public IntPtr WindowHandle { get; }

    /// <inheritdoc/>
    public int ProcessId { get; }

    /// <inheritdoc/>
    public string DisplayName { get; }

    /// <inheritdoc/>
    public SessionState State { get; private set; } = SessionState.Idle;

    /// <inheritdoc/>
    public IServiceProvider Services => _scope.ServiceProvider;

    /// <inheritdoc/>
    public Task StartAsync(CancellationToken ct = default)
    {
        if (State == SessionState.Running) return Task.CompletedTask;
        State = SessionState.Running;
        _logger.LogInformation("Session {Id} 已启动", SessionId);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken ct = default)
    {
        if (State == SessionState.Stopped) return Task.CompletedTask;
        _cts.Cancel();
        State = SessionState.Stopped;
        _logger.LogInformation("Session {Id} 已停止", SessionId);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _cts.Dispose();
        _scope.Dispose();
    }
}
