using System.IO;
using iFramework;

/// <summary>
/// 游戏对象：单个游戏窗口对应一个 Game，聚合背包/状态读取等业务组件。
/// </summary>
public class Game
{
    public static string? OverrideConfigRoot { get; set; }

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

    private static string ResolveConfigRoot()
    {
        if (!string.IsNullOrWhiteSpace(OverrideConfigRoot))
        {
            return OverrideConfigRoot!;
        }

        var localConfig = Path.Combine(AppContext.BaseDirectory, "Config");
        if (Directory.Exists(localConfig))
        {
            return localConfig;
        }

        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "src", "Core", "Config");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        return localConfig;
    }

    public Game(IntPtr hWnd, InputMode inputMode = InputMode.Foreground)
    {
        _hWnd = hWnd;
        var configRoot = ResolveConfigRoot();

        WindowMgr = new WindowMgr();
        InputMgr = CreateInputMgr(inputMode);
        VisionServiceMgr = new VisionService(hWnd, new TemplateMatcher(Path.Combine(configRoot, "Templates")), new NoopOcrEngine());
        UIMgr = new UIMgr();
        UIElementLocateMgr = new UIElementLocateMgr();
        WindowMgr.Initialize(hWnd);
        InputMgr.Initialize(WindowMgr);
        UIElementLocateMgr.Initialize(Path.Combine(configRoot, "UILayout.json"));

        GameReader = new ImageGameReader(this);
        GameControl = new PlayerGameControl(this);

        BagMgr = new BagMgr(this);
        UIMgr.SetProvider(new HumanUIMgrProvider(this));
    }

    /// <summary>
    /// 按输入模式创建对应的 <see cref="IInputMgr"/> 实现。
    /// Driver 模式使用 Interception 内核驱动模拟真实硬件输入，兼容性最强，
    /// 但需要提前安装驱动（见 <see cref="DriverInputMgr.EnsureDriverReady"/>）。
    /// </summary>
    private static IInputMgr CreateInputMgr(InputMode inputMode) => inputMode switch
    {
        InputMode.Driver => new DriverInputMgr(),
        _ => new ForegroundInputMgr(),
    };

    public void Dispose()
    {
        WindowMgr.Dispose();
        InputMgr.Dispose();
        UIElementLocateMgr.Dispose();

        UIMgr.Dispose();
    }
}
