/// <summary>
/// 会话状态：SessionManager 与 UI 使用。
/// </summary>
public enum SessionState
{
    /// <summary>已创建但未运行。</summary>
    Idle,
    /// <summary>运行中。</summary>
    Running,
    /// <summary>已暂停。</summary>
    Paused,
    /// <summary>发生错误。</summary>
    Error,
    /// <summary>已停止（不可恢复）。</summary>
    Stopped,
}

/// <summary>
/// 游戏会话：一个游戏窗口对应一个 Session。
/// 是所有模块运行的隔离容器。
/// </summary>
public interface IGameSession : IDisposable
{
    /// <summary>会话唯一 ID。</summary>
    Guid SessionId { get; }

    /// <summary>关联的游戏窗口句柄。</summary>
    IntPtr WindowHandle { get; }

    /// <summary>关联的进程 ID。</summary>
    int ProcessId { get; }

    /// <summary>显示名，如"小号1"。</summary>
    string DisplayName { get; }

    /// <summary>当前会话状态。</summary>
    SessionState State { get; }

    /// <summary>游戏对象。</summary>
    Game Game { get; }

    /// <summary>启动会话。</summary>
    Task StartAsync(CancellationToken ct = default);

    /// <summary>停止会话。</summary>
    Task StopAsync(CancellationToken ct = default);
}
