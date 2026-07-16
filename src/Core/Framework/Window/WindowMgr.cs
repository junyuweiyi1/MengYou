using System.Runtime.InteropServices;
using System.Threading;

namespace iFramework;

/// <summary>
/// 游戏窗口
/// </summary>
public class WindowMgr : IWindowMgr
{
    public IntPtr HWnd { get; private set; }
    public bool IsWindowForground
    {
        get
        {
            return User32.GetForegroundWindow() == HWnd;
        }
    }

    public void Initialize(IntPtr hWnd)
    {
        HWnd = hWnd;
    }

    /// <summary>
    /// 前台
    /// </summary>
    public void ForgroundWindow()
    {
        ActivateWindow();
    }

    private void ActivateWindow()
    {
        if (User32.IsWindowVisible(HWnd))
        {
            User32.ShowWindowAsync(HWnd, User32.SW_SHOW);
        }

        Thread.Sleep(80);

        User32.AllowSetForegroundWindow(User32.ASFW_ANY);
        User32.BringWindowToTop(HWnd);

        var foreground = User32.GetForegroundWindow();
        var currentThreadId = User32.GetCurrentThreadId();
        var foregroundThreadId = foreground == IntPtr.Zero ? 0 : User32.GetWindowThreadProcessId(foreground, IntPtr.Zero);
        var targetThreadId = User32.GetWindowThreadProcessId(HWnd, IntPtr.Zero);

        var attachedToForeground = false;
        var attachedToTarget = false;

        try
        {
            if (foregroundThreadId != 0 && foregroundThreadId != currentThreadId)
            {
                attachedToForeground = User32.AttachThreadInput(currentThreadId, foregroundThreadId, true);
            }

            if (targetThreadId != 0 && targetThreadId != currentThreadId)
            {
                attachedToTarget = User32.AttachThreadInput(currentThreadId, targetThreadId, true);
            }

            TapAlt();
            User32.SetActiveWindow(HWnd);
            User32.SetFocus(HWnd);
            User32.SetForegroundWindow(HWnd);
            User32.BringWindowToTop(HWnd);
            Thread.Sleep(80);

            if (User32.GetForegroundWindow() != HWnd)
            {
                ClickToActivate();
                Thread.Sleep(80);
                User32.SetForegroundWindow(HWnd);
                User32.BringWindowToTop(HWnd);
            }
        }
        finally
        {
            if (attachedToTarget)
            {
                User32.AttachThreadInput(currentThreadId, targetThreadId, false);
            }

            if (attachedToForeground)
            {
                User32.AttachThreadInput(currentThreadId, foregroundThreadId, false);
            }
        }
    }

    private void ClickToActivate()
    {
        if (!TryGetClientActivationPoint(out var clientPoint, out var screenPoint)) return;

        User32.GetCursorPos(out var originalCursor);

        try
        {
            User32.SetCursorPos(screenPoint.X, screenPoint.Y);
            Thread.Sleep(30);

            SendMouseFlag(WinMessages.MOUSEEVENTF_LEFTDOWN);
            Thread.Sleep(30);
            SendMouseFlag(WinMessages.MOUSEEVENTF_LEFTUP);

            User32.PostMessage(HWnd, WinMessages.WM_MOUSEMOVE, IntPtr.Zero, WinMessages.MakeLParam(clientPoint.X, clientPoint.Y));
            User32.PostMessage(HWnd, WinMessages.WM_LBUTTONDOWN, (IntPtr)1, WinMessages.MakeLParam(clientPoint.X, clientPoint.Y));
            User32.PostMessage(HWnd, WinMessages.WM_LBUTTONUP, IntPtr.Zero, WinMessages.MakeLParam(clientPoint.X, clientPoint.Y));
        }
        finally
        {
            User32.SetCursorPos(originalCursor.X, originalCursor.Y);
        }
    }

    private bool TryGetClientActivationPoint(out User32.Point clientPoint, out User32.Point screenPoint)
    {
        clientPoint = default;
        screenPoint = default;

        if (!User32.GetClientRect(HWnd, out var clientRect)) return false;

        var width = Math.Max(1, clientRect.Right - clientRect.Left);
        var height = Math.Max(1, clientRect.Bottom - clientRect.Top);

        clientPoint = new User32.Point
        {
            X = Math.Max(8, width / 2),
            Y = Math.Max(8, Math.Min(height / 2, height - 8)),
        };

        screenPoint = clientPoint;
        return User32.ClientToScreen(HWnd, ref screenPoint);
    }

    private static void SendMouseFlag(uint flag)
    {
        var input = new User32.INPUT
        {
            Type = User32.INPUT_MOUSE,
            Data = new User32.InputUnion
            {
                Mouse = new User32.MOUSEINPUT
                {
                    Flags = flag,
                }
            }
        };

        User32.SendInput(1, new[] { input }, Marshal.SizeOf<User32.INPUT>());
    }

    private static void TapAlt()
    {
        // 使用扫描码（Scan Code）+ 硬件模拟方式发送，而非虚拟键码模式，
        // 以规避部分游戏对纯 VirtualKey 模式 SendInput 的过滤/忽略。
        var scanCode = (ushort)User32.MapVirtualKey(WinMessages.VK_MENU, WinMessages.MAPVK_VK_TO_VSC);

        var down = new User32.INPUT
        {
            Type = User32.INPUT_KEYBOARD,
            Data = new User32.InputUnion
            {
                Keyboard = new User32.KEYBDINPUT
                {
                    VirtualKey = 0,
                    ScanCode = scanCode,
                    Flags = WinMessages.KEYEVENTF_SCANCODE,
                }
            }
        };

        var up = new User32.INPUT
        {
            Type = User32.INPUT_KEYBOARD,
            Data = new User32.InputUnion
            {
                Keyboard = new User32.KEYBDINPUT
                {
                    VirtualKey = 0,
                    ScanCode = scanCode,
                    Flags = WinMessages.KEYEVENTF_SCANCODE | WinMessages.KEYEVENTF_KEYUP,
                }
            }
        };

        User32.SendInput(2, new[] { down, up }, Marshal.SizeOf<User32.INPUT>());
    }

    /// <summary>
    /// 后台
    /// </summary>
    public void BackgroundWindow()
    {
        User32.SetBackgroundWindow(HWnd);
    }

    public void Dispose()
    {
    }
}
