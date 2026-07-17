using iFramework;
public class FW
{
    /// <summary>窗口</summary>
    public IWindowMgr WindowMgr { get; private set; } = null!;
    /// <summary>输入</summary>
    public IInputMgr InputMgr { get; private set; } = null!;
    /// <summary>视觉</summary>
    public IVisionServiceMgr VisionServiceMgr { get; private set; } = null!;
    /// <summary>UI</summary>
    public IUIMgr UIMgr { get; private set; } = null!;
    /// <summary>UI位置</summary>
    public IUIElementLocateMgr UIElementLocateMgr { get; private set; } = null!;

    public void Initialize(IntPtr hWnd, InputMode inputMode)
    {
        WindowMgr = new WindowMgr();
        InputMgr = new DriverInputMgr();
        VisionServiceMgr = new VisionService();
        UIMgr = new UIMgr();
        UIElementLocateMgr = new UIElementLocateMgr();

        WindowMgr.Initialize(hWnd);
        InputMgr.Initialize(WindowMgr);
        var configRoot = ResolveConfigRoot();
        var templateRoot = Path.Combine(configRoot, "Templates");
        VisionServiceMgr.Initialize(
            hWnd,
            new TemplateMatcher(templateRoot),
            new PaddleOcrEngine());
        UIElementLocateMgr.Initialize(Path.Combine(configRoot, "UILayout.json"));
    }

    public void Dispose()
    {
        WindowMgr.Dispose();
        InputMgr.Dispose();
        VisionServiceMgr.Dispose();
        UIElementLocateMgr.Dispose();
        UIMgr.Dispose();
    }

    public static string? OverrideConfigRoot { get; set; }
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
            var candidate = Path.Combine(dir.FullName, "src", "Core", "Game", "Config");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            var legacyCandidate = Path.Combine(dir.FullName, "src", "Core", "Config");
            if (Directory.Exists(legacyCandidate)) return legacyCandidate;

            dir = dir.Parent;
        }

        return localConfig;
    }
}
