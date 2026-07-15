using Core.GameMain.Game.UI;

namespace iFramework;

/// <summary>
/// 前台控制器：使用 SendInput 直接操作系统级鼠标键盘。
/// 优点：几乎所有游戏都响应。缺点：抢占用户操作，无法多开并行。
/// </summary>
public sealed class ForegroundInputMgr : IInputMgr
{
    /// <summary>目标窗口句柄（用于坐标转换与前台切换）。</summary>
    private IUIMgr _windowMgr;

    /// <inheritdoc/>
    public InputMode Mode => InputMode.Foreground;

    public void Initialize(IUIMgr windowMgr)
    {
        _windowMgr = windowMgr;
    }

    /// <inheritdoc/>
    public Task ClickAsync(Vector2 point, MouseButton button = MouseButton.Left, CancellationToken ct = default)
    {
        EnsureForeground();
        var screen = ClientToScreen(point);
        MoveCursorAbsolute(screen);
        var (down, up) = button switch
        {
            MouseButton.Right => (WinMessages.MOUSEEVENTF_RIGHTDOWN, WinMessages.MOUSEEVENTF_RIGHTUP),
            _ => (WinMessages.MOUSEEVENTF_LEFTDOWN, WinMessages.MOUSEEVENTF_LEFTUP),
        };
        SendMouseFlag(down);
        SendMouseFlag(up);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task MoveAsync(Vector2 point, CancellationToken ct = default)
    {
        EnsureForeground();
        var screen = ClientToScreen(point);
        MoveCursorAbsolute(screen);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task DragAsync(Vector2 from, Vector2 to, CancellationToken ct = default)
    {
        EnsureForeground();
        MoveCursorAbsolute(ClientToScreen(from));
        SendMouseFlag(WinMessages.MOUSEEVENTF_LEFTDOWN);
        const int steps = 20;
        for (var i = 1; i <= steps; i++)
        {
            var x = from.X + (to.X - from.X) * i / steps;
            var y = from.Y + (to.Y - from.Y) * i / steps;
            MoveCursorAbsolute(ClientToScreen(new Point2D(x, y)));
            await Task.Delay(10, ct);
        }
        SendMouseFlag(WinMessages.MOUSEEVENTF_LEFTUP);
    }

    /// <inheritdoc/>
    public Task SendKeyAsync(int virtualKeyCode, KeyModifiers modifiers = KeyModifiers.None, CancellationToken ct = default)
    {
        EnsureForeground();
        // 修饰键按下
        if ((modifiers & KeyModifiers.Ctrl) != 0) SendKeyEvent(0x11, false);
        if ((modifiers & KeyModifiers.Shift) != 0) SendKeyEvent(0x10, false);
        if ((modifiers & KeyModifiers.Alt) != 0) SendKeyEvent(0x12, false);
        SendKeyEvent((ushort)virtualKeyCode, false);
        SendKeyEvent((ushort)virtualKeyCode, true);
        if ((modifiers & KeyModifiers.Alt) != 0) SendKeyEvent(0x12, true);
        if ((modifiers & KeyModifiers.Shift) != 0) SendKeyEvent(0x10, true);
        if ((modifiers & KeyModifiers.Ctrl) != 0) SendKeyEvent(0x11, true);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task InputTextAsync(string text, CancellationToken ct = default)
    {
        EnsureForeground();
        // 简化实现：按字符发送 VK
        // 生产版本应改为 SendInput 的 UNICODE 键盘事件
        foreach (var c in text)
        {
            var vk = char.ToUpper(c);
            SendKeyEvent((ushort)vk, false);
            SendKeyEvent((ushort)vk, true);
        }
        return Task.CompletedTask;
    }

    /// <summary>确保目标窗口在前台。</summary>
    private void EnsureForeground()
    {
        if (!_windowMgr.IsWindowForground)
        {
            _windowMgr.ForgroundWindow();
        }
    }

    /// <summary>客户端坐标转屏幕坐标。</summary>
    private Vector2 ClientToScreen(Vector2 p)
    {
        var pt = new User32.Point { X = p.X, Y = p.Y };
        User32.ClientToScreen(_windowMgr.HWnd, ref pt);
        return new Point2D(pt.X, pt.Y);
    }

    /// <summary>移动光标到绝对屏幕坐标（SendInput 的 ABSOLUTE 需 0~65535）。</summary>
    private static void MoveCursorAbsolute(Vector2 screenPoint)
    {
        var screen = System.Windows.Forms.Screen.PrimaryScreen!.Bounds;
        var normX = (int)(screenPoint.X * 65535.0 / screen.Width);
        var normY = (int)(screenPoint.Y * 65535.0 / screen.Height);
        var input = new User32.INPUT
        {
            Type = User32.INPUT_MOUSE,
            Data = new User32.InputUnion
            {
                Mouse = new User32.MOUSEINPUT
                {
                    Dx = normX,
                    Dy = normY,
                    Flags = WinMessages.MOUSEEVENTF_MOVE | WinMessages.MOUSEEVENTF_ABSOLUTE,
                }
            }
        };
        User32.SendInput(1, new[] { input }, System.Runtime.InteropServices.Marshal.SizeOf<User32.INPUT>());
    }

    /// <summary>发送一次鼠标按键标志。</summary>
    private static void SendMouseFlag(uint flag)
    {
        var input = new User32.INPUT
        {
            Type = User32.INPUT_MOUSE,
            Data = new User32.InputUnion
            {
                Mouse = new User32.MOUSEINPUT { Flags = flag }
            }
        };
        User32.SendInput(1, new[] { input }, System.Runtime.InteropServices.Marshal.SizeOf<User32.INPUT>());
    }

    /// <summary>发送键盘事件。</summary>
    /// <param name="vk">虚拟键码。</param>
    /// <param name="keyUp">true=抬起，false=按下。</param>
    private static void SendKeyEvent(ushort vk, bool keyUp)
    {
        var input = new User32.INPUT
        {
            Type = User32.INPUT_KEYBOARD,
            Data = new User32.InputUnion
            {
                Keyboard = new User32.KEYBDINPUT
                {
                    VirtualKey = vk,
                    ScanCode = (ushort)User32.MapVirtualKey(vk, 0),
                    Flags = keyUp ? WinMessages.KEYEVENTF_KEYUP : 0,
                }
            }
        };
        User32.SendInput(1, new[] { input }, System.Runtime.InteropServices.Marshal.SizeOf<User32.INPUT>());
    }
}
