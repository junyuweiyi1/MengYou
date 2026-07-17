using Core.GameMain.Game.UI;

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

/// <summary>操作模式：前台或后台。</summary>
public enum InputMode
{
    /// <summary>前台模式：使用 SendInput 抢占用户输入。</summary>
    Foreground,
    /// <summary>后台模式预留：计划使用 PostMessage 静默投递，当前版本尚未实现。</summary>
    Background,
    /// <summary>驱动级模式：使用 Interception 内核驱动模拟真实硬件设备输入。</summary>
    Driver,
}

/// <summary>
/// 游戏操作发送抽象：屏蔽模拟输入 / 内存操作两种实现。
/// 所有点击、按键都通过此接口发出。
/// </summary>
public interface IInputMgr
{
    /// <summary>当前操作模式。</summary>
    InputMode Mode { get; }

    void Initialize(IWindowMgr windowMgr);

    /// <summary>点击屏幕坐标（客户端坐标系）。</summary>
    Task ClickAsync(Vector2 point, MouseButton button = MouseButton.Left, CancellationToken ct = default);

    /// <summary>鼠标移动到屏幕坐标。</summary>
    Task MoveAsync(Vector2 point, CancellationToken ct = default);

    /// <summary>拖拽。</summary>
    Task DragAsync(Vector2 from, Vector2 to, CancellationToken ct = default);

    /// <summary>
    /// 按下并释放一个或多个键（组合键），并支持取消。
    /// 多个键按传入顺序依次按下，再按相反顺序释放，例如 SendKeyAsync(ct, KeyCode.Alt, KeyCode.E) 表示 Alt+E。
    /// </summary>
    Task SendKeyAsync(CancellationToken ct, params KeyCode[] keys);

    /// <summary>输入一段文本。</summary>
    Task InputTextAsync(string text, CancellationToken ct = default);

    void Dispose();
}
