using iFramework;

/// <summary>
/// 游戏对象：单个游戏窗口对应一个 Game，聚合背包/状态读取等业务组件。
/// </summary>
public class Game
{
    /// <summary>窗口</summary>
    public IWindowMgr WindowMgr { get; private set; }
    /// <summary>输入</summary>
    public IInputMgr InputMgr { get; private set; }
    /// <summary>视觉</summary>
    public IVisionServiceMgr VisionServiceMgr { get; private set; }
    /// <summary>UI</summary>
    public IUIMgr UIMgr { get; private set; }
    /// <summary>UI位置</summary>
    public IUIElementLocateMgr UIElementLocateMgr { get; private set; }

    public IGameReader GameReader { get; private set; }
    public IGameControl GameControl { get; private set; }

    public BagMgr BagMgr { get; }


    // 目标窗口句柄
    private IntPtr _hWnd;

    public Game(IntPtr hWnd)
    {
        _hWnd = hWnd;

        WindowMgr = new WindowMgr();
        InputMgr = new ForegroundInputMgr();
        UIMgr = new UIMgr();
        UIElementLocateMgr = new UIElementLocateMgr();
        WindowMgr.Initialize(hWnd);
        InputMgr.Initialize(WindowMgr);
        UIElementLocateMgr.Initialize(@"config/UIElementLocateCfg");

        GameReader = new ImageGameReader(this);
        GameControl = new PlayerGameControl(this);

        BagMgr = new BagMgr(this);
        UIMgr.SetProvider(new HumanUIMgrProvider(this));
        BagMgr.Initialize();
    }

    public void Dispose()
    {
        WindowMgr.Dispose();
        InputMgr.Dispose();
        UIElementLocateMgr.Dispose();

        UIMgr.Dispose();
        BagMgr.Dispose();
    }
}