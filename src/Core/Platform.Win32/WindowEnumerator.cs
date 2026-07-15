using System.Text;
using MengYou.Platform.Win32.Native;


/// <summary>
/// 游戏窗口信息。
/// </summary>
public sealed class GameWindow
{
    /// <summary>窗口句柄。</summary>
    public IntPtr Handle { get; init; }

    /// <summary>窗口标题。</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>进程 ID。</summary>
    public int ProcessId { get; init; }

    /// <summary>进程名。</summary>
    public string ProcessName { get; init; } = string.Empty;
}

/// <summary>
/// 窗口枚举器：查找所有可能的梦幻西游窗口。
/// </summary>
public static class WindowEnumerator
{
    /// <summary>枚举所有可见顶级窗口。</summary>
    /// <param name="titleFilter">标题子串过滤，null 则不过滤。</param>
    public static List<GameWindow> Enumerate(string? titleFilter = null)
    {
        var list = new List<GameWindow>();
        User32.EnumWindows((hWnd, _) =>
        {
            if (!User32.IsWindowVisible(hWnd)) return true;
            var len = User32.GetWindowTextLength(hWnd);
            if (len == 0) return true;
            var sb = new StringBuilder(len + 1);
            User32.GetWindowText(hWnd, sb, sb.Capacity);
            var title = sb.ToString();
            if (titleFilter != null && !title.Contains(titleFilter)) return true;
            User32.GetWindowThreadProcessId(hWnd, out var pid);
            var procName = string.Empty;
            try { procName = System.Diagnostics.Process.GetProcessById((int)pid).ProcessName; } catch { }
            list.Add(new GameWindow
            {
                Handle = hWnd,
                Title = title,
                ProcessId = (int)pid,
                ProcessName = procName,
            });
            return true;
        }, IntPtr.Zero);
        return list;
    }
}
