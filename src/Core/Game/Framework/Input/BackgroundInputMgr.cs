namespace iFramework;

/// <summary>
/// 使用 PostMessage 向目标窗口投递输入，不抢占前台。部分游戏会过滤后台消息，
/// 遇到这种情况应切换到 Foreground 或 Driver 模式。
/// </summary>
public sealed class BackgroundInputMgr : IInputMgr
{
    private IWindowMgr _windowMgr = null!;

    public InputMode Mode => InputMode.Background;

    public void Initialize(IWindowMgr windowMgr)
        => _windowMgr = windowMgr ?? throw new ArgumentNullException(nameof(windowMgr));

    public Task ClickAsync(Vector2 point, MouseButton button = MouseButton.Left, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var (down, up, downWParam) = button switch
        {
            MouseButton.Right => (WinMessages.WM_RBUTTONDOWN, WinMessages.WM_RBUTTONUP, new IntPtr(2)),
            MouseButton.Middle => throw new NotSupportedException("后台模式暂不支持鼠标中键。"),
            _ => (WinMessages.WM_LBUTTONDOWN, WinMessages.WM_LBUTTONUP, new IntPtr(1)),
        };
        var position = WinMessages.MakeLParam(point.x, point.y);
        User32.PostMessage(_windowMgr.HWnd, WinMessages.WM_MOUSEMOVE, IntPtr.Zero, position);
        User32.PostMessage(_windowMgr.HWnd, down, downWParam, position);
        User32.PostMessage(_windowMgr.HWnd, up, IntPtr.Zero, position);
        return Task.CompletedTask;
    }

    public Task MoveAsync(Vector2 point, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        User32.PostMessage(_windowMgr.HWnd, WinMessages.WM_MOUSEMOVE, IntPtr.Zero, WinMessages.MakeLParam(point.x, point.y));
        return Task.CompletedTask;
    }

    public async Task DragAsync(Vector2 from, Vector2 to, CancellationToken ct = default)
    {
        var start = WinMessages.MakeLParam(from.x, from.y);
        User32.PostMessage(_windowMgr.HWnd, WinMessages.WM_LBUTTONDOWN, new IntPtr(1), start);
        const int steps = 20;
        for (var i = 1; i <= steps; i++)
        {
            ct.ThrowIfCancellationRequested();
            var x = from.x + (to.x - from.x) * i / steps;
            var y = from.y + (to.y - from.y) * i / steps;
            User32.PostMessage(_windowMgr.HWnd, WinMessages.WM_MOUSEMOVE, new IntPtr(1), WinMessages.MakeLParam(x, y));
            await Task.Delay(10, ct).ConfigureAwait(false);
        }
        User32.PostMessage(_windowMgr.HWnd, WinMessages.WM_LBUTTONUP, IntPtr.Zero, WinMessages.MakeLParam(to.x, to.y));
    }

    public Task SendKeyAsync(CancellationToken ct, params KeyCode[] keys)
    {
        ct.ThrowIfCancellationRequested();
        if (keys is null || keys.Length == 0) return Task.CompletedTask;

        var hasAlt = keys.Any(key => key is KeyCode.Alt or KeyCode.LAlt or KeyCode.RAlt);
        var downMessage = hasAlt ? WinMessages.WM_SYSKEYDOWN : WinMessages.WM_KEYDOWN;
        var upMessage = hasAlt ? WinMessages.WM_SYSKEYUP : WinMessages.WM_KEYUP;
        var altContext = hasAlt ? new IntPtr(1 << 29) : IntPtr.Zero;
        var keyUpContext = hasAlt ? new IntPtr(unchecked((int)0xE0000000)) : new IntPtr(unchecked((int)0xC0000000));
        foreach (var key in keys)
            User32.PostMessage(_windowMgr.HWnd, downMessage, new IntPtr((int)key), altContext);
        for (var i = keys.Length - 1; i >= 0; i--)
            User32.PostMessage(_windowMgr.HWnd, upMessage, new IntPtr((int)keys[i]), keyUpContext);
        return Task.CompletedTask;
    }

    public Task InputTextAsync(string text, CancellationToken ct = default)
    {
        foreach (var character in text ?? string.Empty)
        {
            ct.ThrowIfCancellationRequested();
            User32.PostMessage(_windowMgr.HWnd, WinMessages.WM_CHAR, new IntPtr(character), IntPtr.Zero);
        }
        return Task.CompletedTask;
    }

    public void Dispose()
    {
    }
}
