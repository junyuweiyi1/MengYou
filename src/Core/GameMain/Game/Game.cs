using System.IO;
using iFramework;
using MengYou.UI;
using MengYou.Runtime;

/// <summary>
/// 游戏对象：单个游戏窗口对应一个 Game，聚合背包/状态读取等业务组件。
/// </summary>
public class Game : IDisposable
{
    private bool _disposed;

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

    /// <summary>会话级 UI 模块注册表。</summary>
    public IUiRegistry UiRegistry { get; }

    public IGameReader GameReader { get; private set; }
    public IGameControl GameControl { get; private set; }

    /// <summary>会话级动作执行器，所有会改变游戏状态的操作都通过它串行调度。</summary>
    public IGameActionExecutor ActionExecutor { get; }

    /// <summary>面向功能模块的只读游戏状态存储。</summary>
    public IGameStateStore StateStore { get; }

    /// <summary>会话后台感知循环。</summary>
    public IGameObservationLoop ObservationLoop { get; }

    /// <summary>会话级功能生命周期监督器。</summary>
    public IGameFeatureSupervisor FeatureSupervisor { get; }

    public BagMgr BagMgr { get; }

    /// <summary>基于背包的角色资源恢复服务。</summary>
    public IRecoveryService RecoveryService { get; }

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

    public Game(
        IntPtr hWnd,
        InputMode inputMode = InputMode.Foreground,
        Guid sessionId = default,
        string displayName = "Game",
        IForegroundCoordinator? foregroundCoordinator = null)
    {
        var configRoot = ResolveConfigRoot();
        sessionId = sessionId == Guid.Empty ? Guid.NewGuid() : sessionId;

        WindowMgr = new WindowMgr();
        InputMgr = CreateInputMgr(inputMode);
        VisionServiceMgr = new VisionService(hWnd, new TemplateMatcher(Path.Combine(configRoot, "Templates")), new NoopOcrEngine());
        UIElementLocateMgr = new UIElementLocateMgr();
        WindowMgr.Initialize(hWnd);
        InputMgr.Initialize(WindowMgr);
        UIElementLocateMgr.Initialize(Path.Combine(configRoot, "UILayout.json"));

        var uiRegistry = new UiRegistry();
        uiRegistry.RegisterDetector(new BagPannelUIShownReader(VisionServiceMgr, UIElementLocateMgr));
        uiRegistry.RegisterDetector(new PlayerStatePannelUIShownReader(VisionServiceMgr, UIElementLocateMgr));
        uiRegistry.RegisterController(new BagPanelShowUIControl(InputMgr));
        UiRegistry = uiRegistry;

        var directUIMgr = new UIMgr();
        GameReader = new ImageGameReader(VisionServiceMgr, UIElementLocateMgr, UiRegistry);
        var directGameControl = new PlayerGameControl(
            directUIMgr,
            InputMgr,
            UIElementLocateMgr,
            UiRegistry);
        directUIMgr.SetProvider(new HumanUIMgrProvider(UiRegistry, directGameControl, VisionServiceMgr));

        ActionExecutor = new GameActionExecutor(sessionId, displayName, foregroundCoordinator);
        var stateStore = new GameStateStore();
        StateStore = stateStore;
        FeatureSupervisor = new GameFeatureSupervisor(
            new GameFeatureContext(sessionId, displayName, stateStore, ActionExecutor));
        ObservationLoop = new GameObservationLoop(
            new ImageGameStateObserver(GameReader, UiRegistry),
            stateStore,
            stateStore,
            displayName: displayName);

        var inputResources = GameActionResources.Input
            | GameActionResources.Ui
            | GameActionResources.Foreground;

        UIMgr = new QueuedUIMgr(directUIMgr, ActionExecutor, inputResources);
        GameControl = new QueuedGameControl(directGameControl, ActionExecutor, inputResources);

        BagMgr = new BagMgr(this);
        RecoveryService = new BagRecoveryService(this);
    }

    /// <summary>
    /// 按输入模式创建对应的 <see cref="IInputMgr"/> 实现。
    /// Driver 模式使用 Interception 内核驱动模拟真实硬件输入，兼容性最强，
    /// 但需要提前安装驱动（见 <see cref="DriverInputMgr.EnsureDriverReady"/>）。
    /// </summary>
    private static IInputMgr CreateInputMgr(InputMode inputMode) => inputMode switch
    {
        InputMode.Driver => new DriverInputMgr(),
        InputMode.Foreground => new ForegroundInputMgr(),
        InputMode.Background => throw new NotSupportedException(
            "后台输入尚未实现。请选择 Foreground 或 Driver，系统不会静默回落到前台输入。"),
        _ => throw new ArgumentOutOfRangeException(nameof(inputMode), inputMode, "未知输入模式。"),
    };

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        FeatureSupervisor.Dispose();
        ObservationLoop.Dispose();
        ActionExecutor.Dispose();
        UIMgr.Dispose();
        InputMgr.Dispose();
        VisionServiceMgr.Dispose();
        UIElementLocateMgr.Dispose();
        WindowMgr.Dispose();
    }
}
