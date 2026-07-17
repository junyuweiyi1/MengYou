using Core.GameMain.Game.UI;
using InputInterceptorNS;

namespace iFramework;

/// <summary>
/// 驱动级输入控制器：基于 Interception（http://www.oblita.com/interception.html）内核驱动模拟键盘鼠标输入。
/// <para>
/// 原理：驱动作为键盘/鼠标的上层过滤驱动（Upper Filter Driver）安装在内核态，应用层通过驱动发出的
/// 按键/鼠标数据在系统看来与真实物理硬件产生的输入完全一致（携带真实设备句柄与硬件 ID），
/// 因此可以绕过依赖 SendInput 来源标记（<c>LLKHF_INJECTED</c>）或 GetRawInputData/DirectInput 设备句柄校验
/// 来过滤"软件模拟输入"的游戏保护机制。ForegroundInputMgr（SendInput）与 BackgroundInputMgr（PostMessage）
/// 均是纯用户态注入，容易被这类校验识别并丢弃；驱动级方案是当前几种方案中兼容性最强的一种。
/// </para>
/// <para>
/// 缺点：需要额外安装内核驱动（安装/卸载需要管理员权限，安装后通常需要重启一次才能生效）。
/// </para>
/// </summary>
public sealed class DriverInputMgr : IInputMgr
{
    /// <summary>目标窗口句柄（用于坐标转换与前台切换）。</summary>
    private IWindowMgr _windowMgr = null!;

    private KeyboardHook _keyboardHook = null!;
    private MouseHook _mouseHook = null!;

    /// <inheritdoc/>
    public InputMode Mode => InputMode.Driver;

    /// <summary>驱动是否已安装（不代表 DLL 已加载/已初始化）。</summary>
    public static bool IsDriverInstalled => InputInterceptor.CheckDriverInstalled();

    /// <summary>当前进程是否具备管理员权限（安装/卸载驱动需要）。</summary>
    public static bool HasAdministratorRights => InputInterceptor.CheckAdministratorRights();

    /// <summary>
    /// 驱动/运行环境诊断结果，用于在 UI 上展示清晰的安装、加载、设备捕获状态，
    /// 避免用户在"点了安装但不知道有没有装上"的黑盒状态。
    /// </summary>
    /// <param name="DriverInstalled">注册表中过滤驱动服务项是否存在（仅代表"安装动作"已执行）。</param>
    /// <param name="HasAdminRights">当前进程是否具备管理员权限。</param>
    /// <param name="DllLoaded">InputInterceptor.dll 本体是否加载成功（用户态动态库，与驱动是否生效无关）。</param>
    /// <param name="KernelActive">
    /// 驱动是否已真正在内核层生效（通过尝试创建 Context 判断）。
    /// 若为 false 但 DriverInstalled 为 true，几乎总是意味着"刚安装完驱动但还没有重启系统"——
    /// Interception 是键盘/鼠标的上层过滤驱动，只有系统启动时构建设备栈的那一刻才会被真正挂载，
    /// 安装后立即测试是无法生效的，必须重启。
    /// </param>
    /// <param name="KeyboardReady">
    /// 键盘设备是否已被本实例捕获，可用于发送模拟输入。
    /// 注意：该字段只有在调用 <see cref="Initialize"/>（即"连接游戏"）之后才有意义，
    /// 未连接游戏前该字段恒为 false，这是正常现象，不代表驱动异常。
    /// </param>
    /// <param name="MouseReady">
    /// 鼠标设备是否已被本实例捕获。含义与 <paramref name="KeyboardReady"/> 相同：
    /// 未连接游戏前恒为 false，是正常现象。
    /// </param>
    public readonly record struct Diagnostics(
        bool DriverInstalled,
        bool HasAdminRights,
        bool DllLoaded,
        bool KernelActive,
        bool KeyboardReady,
        bool MouseReady)
    {
        /// <summary>
        /// 是否已具备发送模拟输入的完整条件。
        /// 只有在"连接游戏"之后（即存在 <see cref="DriverInputMgr"/> 实例）才可能为 true。
        /// </summary>
        public bool IsFullyReady => DriverInstalled && DllLoaded && KernelActive && KeyboardReady && MouseReady;

        /// <summary>是否已完成"安装"层面的准备（不含设备捕获，用于连接游戏前的预检）。</summary>
        public bool IsInstallationReady => DriverInstalled && DllLoaded && KernelActive;

        /// <summary>驱动注册表项存在，但内核层未生效——几乎总是意味着需要重启系统。</summary>
        public bool LikelyNeedsReboot => DriverInstalled && DllLoaded && !KernelActive;

        public override string ToString()
        {
            string Flag(bool b) => b ? "OK" : "缺失";
            return $"驱动已安装:{Flag(DriverInstalled)} | 管理员权限:{Flag(HasAdminRights)} | " +
                   $"DLL已加载:{Flag(DllLoaded)} | 驱动内核已生效:{Flag(KernelActive)}";
        }

        /// <summary>包含设备捕获状态的完整描述，仅在"连接游戏"之后调用才有意义。</summary>
        public string ToFullString()
        {
            string Flag(bool b) => b ? "OK" : "缺失";
            return $"{ToString()} | 键盘设备就绪:{Flag(KeyboardReady)} | 鼠标设备就绪:{Flag(MouseReady)}";
        }
    }

    /// <summary>
    /// 通过实际创建一次 Context 来判断驱动是否真正在内核层生效。
    /// 注：不能依赖 InputInterceptorNS.InputInterceptor.Initialized 字段——该字段是第三方库的实现缺陷，
    /// 它在静态构造函数执行前就已经求值，永远为 false，无法反映真实的 DLL 加载状态。
    /// </summary>
    private static bool CheckKernelActive()
    {
        try
        {
            var context = InputInterceptor.CreateContext();
            if (context == IntPtr.Zero) return false;
            InputInterceptor.DestroyContext(context);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 采集当前诊断信息。若本实例尚未 Initialize，KeyboardReady/MouseReady 恒为 false（正常现象）。
    /// 注意：驱动"已安装"仅代表注册表中的过滤驱动服务项存在；若刚完成安装，仍需重启系统才能真正生效，
    /// 此时 KernelActive 会是 false，可通过 <see cref="Diagnostics.LikelyNeedsReboot"/> 识别这种情况。
    /// </summary>
    public Diagnostics GetDiagnostics()
    {
        var dllLoaded = InputInterceptor.Initialize();
        return new(
            DriverInstalled: IsDriverInstalled,
            HasAdminRights: HasAdministratorRights,
            DllLoaded: dllLoaded,
            KernelActive: dllLoaded && CheckKernelActive(),
            KeyboardReady: _keyboardHook?.CanSimulateInput ?? false,
            MouseReady: _mouseHook?.CanSimulateInput ?? false);
    }

    /// <summary>
    /// 不依赖实例、仅用于连接前的预检：驱动安装状态 + 管理员权限 + DLL 加载状态 + 内核是否真正生效。
    /// KeyboardReady/MouseReady 在连接游戏前恒为 false，属正常现象。
    /// </summary>
    public static Diagnostics GetStaticDiagnostics()
    {
        var dllLoaded = InputInterceptor.Initialize();
        return new(
            DriverInstalled: IsDriverInstalled,
            HasAdminRights: HasAdministratorRights,
            DllLoaded: dllLoaded,
            KernelActive: dllLoaded && CheckKernelActive(),
            KeyboardReady: false,
            MouseReady: false);
    }

    /// <summary>
    /// 确保驱动已安装并加载 DLL。
    /// 若驱动未安装，会在具备管理员权限时尝试静默安装（安装后通常需要重启系统才能生效）。
    /// </summary>
    /// <returns>DLL 是否已成功加载（不代表驱动已就绪，若刚完成安装，需重启后才生效）。</returns>
    public static bool EnsureDriverReady()
    {
        if (!InputInterceptor.Initialize())
            return false;

        if (InputInterceptor.CheckDriverInstalled())
            return true;

        if (!InputInterceptor.CheckAdministratorRights())
            return false;

        return InputInterceptor.InstallDriver();
    }

    /// <summary>
    /// 安装结果，用于向 UI 层反馈清晰的状态（而非仅一个 bool）。
    /// </summary>
    public enum InstallResult
    {
        /// <summary>驱动此前已安装，无需操作。</summary>
        AlreadyInstalled,
        /// <summary>本次安装成功，需要重启系统才能生效。</summary>
        InstalledNeedsReboot,
        /// <summary>安装失败（安装程序返回非 0 退出码，或抛出异常）。</summary>
        Failed,
        /// <summary>当前进程无管理员权限，无法安装。</summary>
        RequiresAdmin,
        /// <summary>驱动 DLL 本身加载失败（缺少运行时资源等），未进行安装。</summary>
        DllLoadFailed,
    }

    /// <summary>
    /// 异步执行"确保驱动就绪"的完整流程，并返回明确的 <see cref="InstallResult"/>，
    /// 供 UI 层显示"正在安装/安装成功需重启/安装失败/需要管理员权限"等具体状态，
    /// 而不是让用户在点击后完全不知道发生了什么。
    /// </summary>
    public static Task<InstallResult> EnsureDriverReadyAsync(CancellationToken ct = default)
    {
        return Task.Run(() =>
        {
            if (!InputInterceptor.Initialize())
                return InstallResult.DllLoadFailed;

            if (InputInterceptor.CheckDriverInstalled())
                return InstallResult.AlreadyInstalled;

            if (!InputInterceptor.CheckAdministratorRights())
                return InstallResult.RequiresAdmin;

            return InputInterceptor.InstallDriver() ? InstallResult.InstalledNeedsReboot : InstallResult.Failed;
        }, ct);
    }

    /// <inheritdoc/>
    public void Initialize(IWindowMgr windowMgr)
    {
        _windowMgr = windowMgr;

        if (!InputInterceptor.Initialize())
        {
            throw new InvalidOperationException(
                "Interception 驱动 DLL 加载失败，请确认驱动已安装（可调用 DriverInputMgr.EnsureDriverReady() 尝试自动安装，安装后需重启系统）。");
        }

        // filter 使用 None、callback 使用 null：只借助 Hook 定位一个真实的键盘/鼠标设备用于发送模拟数据，
        // 不拦截、不篡改用户真实的物理输入，物理按键仍会原样传递给系统。
        _keyboardHook = new KeyboardHook();
        _mouseHook = new MouseHook();
    }

    /// <inheritdoc/>
    public Task ClickAsync(Vector2 point, MouseButton button = MouseButton.Left, CancellationToken ct = default)
    {
        EnsureForeground();
        var screen = ClientToScreen(point);
        _mouseHook.SetCursorPosition(screen.X, screen.Y);
        switch (button)
        {
            case MouseButton.Right:
                _mouseHook.SimulateRightButtonClick();
                break;
            case MouseButton.Middle:
                _mouseHook.SimulateMiddleButtonClick();
                break;
            default:
                _mouseHook.SimulateLeftButtonClick();
                break;
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task MoveAsync(Vector2 point, CancellationToken ct = default)
    {
        EnsureForeground();
        var screen = ClientToScreen(point);
        _mouseHook.SetCursorPosition(screen.X, screen.Y);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task DragAsync(Vector2 from, Vector2 to, CancellationToken ct = default)
    {
        EnsureForeground();
        var fromScreen = ClientToScreen(from);
        _mouseHook.SetCursorPosition(fromScreen.X, fromScreen.Y);
        _mouseHook.SimulateLeftButtonDown();
        const int steps = 20;
        for (var i = 1; i <= steps; i++)
        {
            var x = from.X + (to.X - from.X) * i / steps;
            var y = from.Y + (to.Y - from.Y) * i / steps;
            var screen = ClientToScreen(new Vector2(x, y));
            _mouseHook.SetCursorPosition(screen.X, screen.Y);
            await Task.Delay(10, ct);
        }
        _mouseHook.SimulateLeftButtonUp();
    }

    /// <inheritdoc/>
    public Task SendKeyAsync(CancellationToken ct, params KeyCode[] keys)
    {
        EnsureForeground();
        if (keys == null || keys.Length == 0)
            return Task.CompletedTask;

        // 按传入顺序依次按下
        foreach (var key in keys)
            SendKeyEvent(key, false);

        // 按相反顺序依次释放
        for (var i = keys.Length - 1; i >= 0; i--)
            SendKeyEvent(keys[i], true);

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task InputTextAsync(string text, CancellationToken ct = default)
    {
        EnsureForeground();
        foreach (var c in text)
        {
            var vk = (KeyCode)char.ToUpper(c);
            SendKeyEvent(vk, false);
            SendKeyEvent(vk, true);
        }
        return Task.CompletedTask;
    }

    /// <summary>确保目标窗口在前台。驱动模拟的输入同真实硬件一样，只会被当前拥有输入焦点的窗口接收。</summary>
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
        return new Vector2(pt.X, pt.Y);
    }

    /// <summary>
    /// 发送键盘事件：将项目内的虚拟键码（VK）换算为扫描码（Scan Code），
    /// 通过 Interception 驱动以"真实硬件设备"的身份下发，而不是普通用户态注入。
    /// </summary>
    /// <param name="key">虚拟键码。</param>
    /// <param name="keyUp">true=抬起，false=按下。</param>
    private void SendKeyEvent(KeyCode key, bool keyUp)
    {
        var scanCode = (ushort)User32.MapVirtualKey((uint)key, WinMessages.MAPVK_VK_TO_VSC);
        var code = (InputInterceptorNS.KeyCode)scanCode;
        var state = keyUp ? KeyState.Up : KeyState.Down;
        if (key.IsExtendedKey())
        {
            state |= KeyState.E0;
        }
        _keyboardHook.SetKeyState(code, state);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _keyboardHook?.Dispose();
        _mouseHook?.Dispose();
    }
}
