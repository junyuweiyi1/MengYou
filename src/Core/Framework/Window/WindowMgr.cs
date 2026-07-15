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
            return User32.GetForegroundWindow() != HWnd;
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
        if (IsWindowForground) return;

        User32.SetForegroundWindow(HWnd);
    }

    /// <summary>
    /// 后台
    /// </summary>
    public void BackgroundWindow()
    {
        if (!IsWindowForground) return;

        User32.SetBackgroundWindow(HWnd);
    }
}