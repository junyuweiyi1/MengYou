/// <summary>
/// 游戏会话默认实现：拥有独立 DI 子容器，是多开隔离的基石。
/// </summary>
public sealed class GameSession : IGameSession
{
    /// <summary>会话内部取消令牌源。</summary>
    private readonly CancellationTokenSource _cts = new();

    /// <summary>日志。</summary>
    private readonly Logger _logger;

    private readonly SemaphoreSlim _lifecycleLock = new(1, 1);

    private int _disposed;

    /// <summary>创建会话。</summary>
    /// <param name="data">会话数据。</param>
    /// <param name="foregroundCoordinator">跨会话前台操作协调器。</param>
    public GameSession(
        SessionData data,
        MengYou.Runtime.IForegroundCoordinator? foregroundCoordinator = null)
    {
        _logger = new Logger(GetType().Name);
        SessionId = Guid.NewGuid();
        WindowHandle = data.WindowHandle;
        ProcessId = data.ProcessId;
        DisplayName = data.DisplayName;
        Game = new Game(
            WindowHandle,
            data.InputMode,
            SessionId,
            DisplayName,
            foregroundCoordinator);

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

    public Game Game { get; }

    public MengYou.Runtime.IGameActionExecutor Actions => Game.ActionExecutor;

    public MengYou.Runtime.IGameStateStore StateStore => Game.StateStore;

    public MengYou.Runtime.IGameFeatureSupervisor Features => Game.FeatureSupervisor;


    /// <inheritdoc/>
    public async Task StartAsync(CancellationToken ct = default)
    {
        await _lifecycleLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
            if (State == SessionState.Running) return;
            if (State == SessionState.Stopped)
                throw new InvalidOperationException("已停止的 Session 不能重新启动。");

            await Actions.StartAsync(ct).ConfigureAwait(false);
            try
            {
                await Game.ObservationLoop.StartAsync(ct).ConfigureAwait(false);
            }
            catch
            {
                await Actions.StopAsync(CancellationToken.None).ConfigureAwait(false);
                throw;
            }
            State = SessionState.Running;
            _logger.LogFormat("Session {0} 已启动", SessionId);
        }
        catch
        {
            State = SessionState.Error;
            throw;
        }
        finally
        {
            _lifecycleLock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task StopAsync(CancellationToken ct = default)
    {
        await _lifecycleLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (State == SessionState.Stopped) return;
            _cts.Cancel();
            await Features.StopAllAsync(ct).ConfigureAwait(false);
            await Game.ObservationLoop.StopAsync(ct).ConfigureAwait(false);
            await Actions.StopAsync(ct).ConfigureAwait(false);
            State = SessionState.Stopped;
            _logger.LogFormat("Session {0} 已停止", SessionId);
        }
        finally
        {
            _lifecycleLock.Release();
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        if (State != SessionState.Stopped)
            StopAsync().GetAwaiter().GetResult();
        _cts.Dispose();
        Game.Dispose();
        _lifecycleLock.Dispose();
    }
}
