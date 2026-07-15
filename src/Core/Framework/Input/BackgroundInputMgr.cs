namespace iFramework;

/// <summary>
/// 后台控制器：通过 PostMessage 投递消息实现静默点击/按键。
/// 优点：可后台、可多开。缺点：部分游戏（含反外挂）不响应。
/// </summary>
public sealed class BackgroundInputMgr : IInputMgr
{
    /// <summary>目标窗口句柄。</summary>
    private readonly IntPtr _hWnd;

    /// <summary>构造。</summary>
    public BackgroundInputMgr(IntPtr hWnd)
    {
        _hWnd = hWnd;
    }

    /// <inheritdoc/>
    public InputMode Mode => InputMode.Background;

    /// <inheritdoc/>
    public Task ClickAsync(Point2D point, MouseButton button = MouseButton.Left, CancellationToken ct = default)
    {
        var lParam = WinMessages.MakeLParam(point.X, point.Y);
        (uint down, uint up) = button switch
        {
            MouseButton.Right => (WinMessages.WM_RBUTTONDOWN, WinMessages.WM_RBUTTONUP),
            _ => (WinMessages.WM_LBUTTONDOWN, WinMessages.WM_LBUTTONUP),
        };
        // 先移动，再点击
        User32.PostMessage(_hWnd, WinMessages.WM_MOUSEMOVE, IntPtr.Zero, lParam);
        User32.PostMessage(_hWnd, down, (IntPtr)0x0001, lParam);
        User32.PostMessage(_hWnd, up, IntPtr.Zero, lParam);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task MoveAsync(Point2D point, CancellationToken ct = default)
    {
        User32.PostMessage(_hWnd, WinMessages.WM_MOUSEMOVE, IntPtr.Zero, WinMessages.MakeLParam(point.X, point.Y));
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task DragAsync(Point2D from, Point2D to, CancellationToken ct = default)
    {
        // 后台拖拽：按下 → 中间移动多次 → 抬起
        User32.PostMessage(_hWnd, WinMessages.WM_LBUTTONDOWN, (IntPtr)0x0001, WinMessages.MakeLParam(from.X, from.Y));
        const int steps = 10;
        for (var i = 1; i <= steps; i++)
        {
            var x = from.X + (to.X - from.X) * i / steps;
            var y = from.Y + (to.Y - from.Y) * i / steps;
            User32.PostMessage(_hWnd, WinMessages.WM_MOUSEMOVE, (IntPtr)0x0001, WinMessages.MakeLParam(x, y));
            await Task.Delay(15, ct);
        }
        User32.PostMessage(_hWnd, WinMessages.WM_LBUTTONUP, IntPtr.Zero, WinMessages.MakeLParam(to.X, to.Y));
    }

    /// <inheritdoc/>
    public Task SendKeyAsync(int virtualKeyCode, KeyModifiers modifiers = KeyModifiers.None, CancellationToken ct = default)
    {
        // 简化：忽略修饰键（后台修饰键需额外处理，后期完善）
        User32.PostMessage(_hWnd, WinMessages.WM_KEYDOWN, (IntPtr)virtualKeyCode, IntPtr.Zero);
        User32.PostMessage(_hWnd, WinMessages.WM_KEYUP, (IntPtr)virtualKeyCode, IntPtr.Zero);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task InputTextAsync(string text, CancellationToken ct = default)
    {
        foreach (var c in text)
        {
            User32.PostMessage(_hWnd, WinMessages.WM_CHAR, (IntPtr)c, IntPtr.Zero);
        }
        return Task.CompletedTask;
    }
}
