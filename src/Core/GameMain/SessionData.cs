using iFramework;

/// <summary>
/// 后端类型：切换识别 + 操作实现。
/// </summary>
public enum BackendType
{
    /// <summary>图像识别 + 模拟输入。</summary>
    ImageAndSimulated,
    /// <summary>内存识别 + 内存操作（未来）。</summary>
    Memory,
    /// <summary>混合：图像识别 + 内存操作，等等。</summary>
    Hybrid,
}

/// <summary>
/// 会话初始化数据
/// </summary>
public sealed class SessionData
{
    /// <summary>关联窗口句柄。</summary>
    public IntPtr WindowHandle { get; init; }

    /// <summary>进程 ID。</summary>
    public int ProcessId { get; init; }

    /// <summary>用户可读显示名。</summary>
    public string DisplayName { get; init; } = "Session";

    /// <summary>输入模式。</summary>
    public InputMode InputMode { get; init; } = InputMode.Foreground;

    /// <summary>后端类型。</summary>
    public BackendType Backend { get; init; } = BackendType.ImageAndSimulated;

    /// <summary>是否启用人性化装饰（随机延迟、轨迹）。</summary>
    public bool EnableHumanized { get; init; } = true;
}
