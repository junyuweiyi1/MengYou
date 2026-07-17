using System.IO;
using iFramework;

/// <summary>
/// 游戏
/// </summary>
public class Game
{
    public FW FW { get; private set; }
    public Logic Logic { get; private set; }


    // 目标窗口句柄
    private readonly IntPtr _hWnd;


    public Game(
        IntPtr hWnd,
        InputMode inputMode = InputMode.Foreground,
        IReadOnlyDictionary<string, KeyCode[]>? uiHotkeys = null)
    {
        _hWnd = hWnd;

        FW = new FW();
        Logic = new Logic();

        FW.Initialize(hWnd, inputMode);
        Logic.Initialize(this, uiHotkeys);
    }


    public void Dispose()
    {
        Logic.Dispose();
        FW.Dispose();
    }
}
