namespace iFramework;

/// <summary>鼠标按钮枚举。</summary>
public enum MouseButton
{
    /// <summary>左键。</summary>
    Left,
    /// <summary>右键。</summary>
    Right,
    /// <summary>中键。</summary>
    Middle,
}

/// <summary>键盘修饰键。命名 KeyModifiers 以避免与 WPF 的 ModifierKeys 冲突。</summary>
[Flags]
public enum KeyModifiers
{
    /// <summary>无。</summary>
    None = 0,
    /// <summary>Ctrl。</summary>
    Ctrl = 1,
    /// <summary>Shift。</summary>
    Shift = 2,
    /// <summary>Alt。</summary>
    Alt = 4,
}

/// <summary>操作模式：前台或后台。</summary>
public enum InputMode
{
    /// <summary>前台模式：使用 SendInput 抢占用户输入。</summary>
    Foreground,
    /// <summary>后台模式：使用 PostMessage 静默投递。</summary>
    Background,
}

/// <summary>
/// 游戏操作发送抽象：屏蔽模拟输入 / 内存操作两种实现。
/// 所有点击、按键都通过此接口发出。
/// </summary>
public interface IInputMgr
{
    /// <summary>当前操作模式。</summary>
    InputMode Mode { get; }

    /// <summary>点击屏幕坐标（客户端坐标系）。</summary>
    Task ClickAsync(Point2D point, MouseButton button = MouseButton.Left, CancellationToken ct = default);

    /// <summary>鼠标移动到屏幕坐标。</summary>
    Task MoveAsync(Point2D point, CancellationToken ct = default);

    /// <summary>拖拽。</summary>
    Task DragAsync(Point2D from, Point2D to, CancellationToken ct = default);

    /// <summary>按下并释放某个键。</summary>
    Task SendKeyAsync(int virtualKeyCode, KeyModifiers modifiers = KeyModifiers.None, CancellationToken ct = default);

    /// <summary>输入一段文本。</summary>
    Task InputTextAsync(string text, CancellationToken ct = default);
}
