namespace iFramework;
/// <summary>
/// 游戏窗口
/// </summary>
public interface IWindowMgr : IDisposable
{
    IntPtr HWnd { get; }
    bool IsWindowForground { get; }


    void Initialize(IntPtr hWnd);

    /// <summary>
    /// 前台
    /// </summary>
    void ForgroundWindow();

    /// <summary>
    /// 后台
    /// </summary>
    void BackgroundWindow();
}
