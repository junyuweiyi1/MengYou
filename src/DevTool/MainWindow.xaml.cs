using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using iFramework;
using MengYou.DevTool.Services;
// 用别名避免 System.Drawing.Rectangle 与 System.Windows.Shapes.Rectangle 冲突
using DrawingRect = System.Drawing.Rectangle;
using ShapeRect = System.Windows.Shapes.Rectangle;
using WpfLine = System.Windows.Shapes.Line;

namespace MengYou.DevTool;

/// <summary>
/// 开发者工具主窗口：截图 + 矩形标定 + 保存到 UILayout / 模板。
/// </summary>
public partial class MainWindow : Window
{
    private static class WindowDebugNative
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        internal static extern IntPtr GetForegroundWindow();

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        internal static extern int GetWindowTextLength(IntPtr hWnd);

        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        internal static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder lpString, int nMaxCount);

        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        internal static extern int GetClassName(IntPtr hWnd, System.Text.StringBuilder lpClassName, int nMaxCount);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        internal static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);
    }

    /// <summary>当前冻结截图。</summary>
    private Bitmap? _capture;

    /// <summary>UILayout 存储。</summary>
    private UILayoutStore _layout = null!;

    /// <summary>模板存储。</summary>
    private TemplateStore _templates = null!;

    /// <summary>拖拽起始点（画布坐标）。</summary>
    private System.Windows.Point? _dragStart;

    /// <summary>当前拖拽绘制的红色矩形。</summary>
    private ShapeRect? _dragRect;

    /// <summary>当前选中的矩形（客户端像素坐标）。</summary>
    private Int32Rect? _currentSelection;

    /// <summary>是否显示已标定元素叠加层。</summary>
    private bool _overlayVisible = true;

    /// <summary>已标定元素列表数据源。</summary>
    private readonly System.Collections.ObjectModel.ObservableCollection<ElementItem> _elements = new();

    /// <summary>当前连接的游戏实例。</summary>
    private Game? _game;

    /// <summary>构造。</summary>
    public MainWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => Initialize();
    }

    /// <summary>初始化：加载 UILayout / 模板目录 / 绑定列表。</summary>
    private void Initialize()
    {
        // 定位到主应用的 Config 目录（同一份数据）
        var baseDir = AppContext.BaseDirectory;
        // 若通过 dotnet run 运行，位于 bin/Debug/net7.0-windows；向上回溯并定位到仓库内 Config
        var appConfigDir = FindAppConfigDir(baseDir);
        var templateDir = Path.Combine(appConfigDir, "Templates");
        var layoutFile = Path.Combine(appConfigDir, "UILayout.json");

        _layout = new UILayoutStore(layoutFile);
        _templates = new TemplateStore(templateDir);

        TemplateDirText.Text = templateDir;
        LayoutFileText.Text = layoutFile;

        ElementsList.ItemsSource = _elements;
        RefreshElementsList();
        RefreshWindowsList();
        UpdateWindowDebugInfo();
        SetStatus("初始化完成");
    }

    /// <summary>查找当前仓库实际使用的 Config 目录。</summary>
    private static string FindAppConfigDir(string devToolBaseDir)
    {
        var dir = new DirectoryInfo(devToolBaseDir);
        while (dir != null)
        {
            var coreCandidate = Path.Combine(dir.FullName, "src", "Core", "Config");
            if (Directory.Exists(coreCandidate)) return coreCandidate;

            var localCandidate = Path.Combine(dir.FullName, "Config");
            if (Directory.Exists(localCandidate) && File.Exists(Path.Combine(localCandidate, "UILayout.json")))
                return localCandidate;

            dir = dir.Parent;
        }

        return Path.Combine(devToolBaseDir, "Config");
    }

    private static string FindWorkspaceRoot(string startDir)
    {
        var dir = new DirectoryInfo(startDir);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "src", "Core", "Core.csproj");
            if (File.Exists(candidate))
            {
                return Path.Combine(dir.FullName, "src", "Core");
            }

            dir = dir.Parent;
        }

        return AppContext.BaseDirectory;
    }

    private void OnAttachGame(object sender, RoutedEventArgs e)
    {
        if (WindowsCombo.SelectedItem is not GameWindow gw)
        {
            System.Windows.MessageBox.Show("请先选择窗口");
            return;
        }

        try
        {
            _game?.Dispose();

            var inputMode = iFramework.InputMode.Foreground;
            if (DriverModeCheck.IsChecked == true)
            {
                inputMode = iFramework.InputMode.Driver;
            }

            _game = new Game(gw.Handle, inputMode);
            _game.WindowMgr.ForgroundWindow();
            UpdateWindowDebugInfo(gw.Handle);

            if (inputMode == iFramework.InputMode.Driver && _game.InputMgr is DriverInputMgr driverInputMgr)
            {
                var diag = driverInputMgr.GetDiagnostics();
                UpdateDriverStatusText(diag);
                if (!diag.IsFullyReady)
                {
                    var reason = diag.LikelyNeedsReboot
                        ? "\n\n驱动已安装但尚未在内核层生效，这通常是因为刚安装完驱动还没有重启系统。请重启电脑后再试。"
                        : "";
                    System.Windows.MessageBox.Show(
                        "驱动级输入未完全就绪，按键可能无效。\n" + diag.ToFullString() + reason);
                }
            }

            SetStatus($"已连接游戏窗口: {gw.Title}（输入模式: {inputMode}）");
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show("连接游戏失败: " + ex.Message);
        }
    }

    /// <summary>勾选/取消"驱动级输入"时，立刻刷新一次驱动状态展示，避免用户连接后才发现驱动没装好。</summary>
    private async void OnDriverModeCheckChanged(object sender, RoutedEventArgs e)
    {
        if (DriverModeCheck.IsChecked == true)
        {
            await RefreshDriverStatusAsync(promptInstallIfMissing: true);
        }
    }

    private async void OnCheckDriverStatus(object sender, RoutedEventArgs e)
    {
        await RefreshDriverStatusAsync(promptInstallIfMissing: true);
    }

    /// <summary>
    /// 刷新驱动状态文本，并在驱动缺失、用户确认时触发安装流程（带明确的进度与结果反馈）。
    /// 注意：这里只做"安装/DLL加载"层面的预检，不涉及键盘/鼠标设备捕获状态——
    /// 设备捕获状态只有在点击"连接游戏"之后才有意义，此前显示"缺失"是正常现象。
    /// </summary>
    private async Task RefreshDriverStatusAsync(bool promptInstallIfMissing)
    {
        DriverStatusText.Text = "驱动状态: 检查中...";
        DriverStatusText.Foreground = System.Windows.Media.Brushes.Gray;

        var diag = DriverInputMgr.GetStaticDiagnostics();

        if (diag.IsInstallationReady)
        {
            DriverStatusText.Text = $"驱动状态: 已就绪（请点击“连接游戏”以捕获设备） | {diag}";
            DriverStatusText.Foreground = System.Windows.Media.Brushes.Green;
            return;
        }

        if (diag.LikelyNeedsReboot)
        {
            // 驱动已安装、DLL 也能加载，但内核层未生效（CreateContext 拿不到有效句柄）：
            // Interception 是键盘/鼠标的上层过滤驱动，只有系统启动构建设备栈时才会真正挂载，
            // 安装后立即测试必然失败，这是正常现象，只需重启系统一次即可。
            DriverStatusText.Text = $"驱动状态: 已安装但尚未重启，驱动未在内核层生效 | {diag}";
            DriverStatusText.Foreground = System.Windows.Media.Brushes.OrangeRed;
            if (promptInstallIfMissing)
            {
                System.Windows.MessageBox.Show(
                    "驱动已安装成功，但尚未在内核层生效。\n" +
                    "这是正常现象：Interception 作为键盘/鼠标的过滤驱动，只有系统启动时才会被真正加载，\n" +
                    "安装后必须重启电脑一次才能生效。请重启后再次点击“检查驱动状态”确认。");
            }
            return;
        }

        DriverStatusText.Text = $"驱动状态: 未安装 | {diag}";
        DriverStatusText.Foreground = System.Windows.Media.Brushes.OrangeRed;

        if (!promptInstallIfMissing) return;

        if (!diag.HasAdminRights)
        {
            System.Windows.MessageBox.Show(
                "检测到 Interception 驱动未安装，且当前程序没有管理员权限，无法自动安装。\n" +
                "请以管理员身份重新启动本程序后再试。");
            return;
        }

        var confirm = System.Windows.MessageBox.Show(
            "检测到 Interception 驱动未安装。是否现在安装？\n" +
            "安装需要管理员权限（已具备），安装完成后需要重启系统一次才能生效。",
            "安装驱动确认", MessageBoxButton.YesNo);
        if (confirm != MessageBoxResult.Yes) return;

        DriverStatusText.Text = "驱动状态: 正在安装...（请稍候，可能需要几秒钟）";
        DriverStatusText.Foreground = System.Windows.Media.Brushes.Gray;
        SetStatus("正在安装 Interception 驱动...");

        var result = await DriverInputMgr.EnsureDriverReadyAsync();

        switch (result)
        {
            case DriverInputMgr.InstallResult.AlreadyInstalled:
                DriverStatusText.Text = "驱动状态: 已安装";
                DriverStatusText.Foreground = System.Windows.Media.Brushes.Green;
                SetStatus("驱动已安装");
                break;
            case DriverInputMgr.InstallResult.InstalledNeedsReboot:
                DriverStatusText.Text = "驱动状态: 安装成功，需重启系统后才能生效！";
                DriverStatusText.Foreground = System.Windows.Media.Brushes.OrangeRed;
                SetStatus("驱动安装成功，需重启系统");
                System.Windows.MessageBox.Show("驱动安装成功！\n请重启电脑后，驱动级输入才能真正生效。");
                break;
            case DriverInputMgr.InstallResult.Failed:
                DriverStatusText.Text = "驱动状态: 安装失败";
                DriverStatusText.Foreground = System.Windows.Media.Brushes.Red;
                SetStatus("驱动安装失败");
                System.Windows.MessageBox.Show("驱动安装失败，请检查是否已有安全软件拦截驱动安装。");
                break;
            case DriverInputMgr.InstallResult.RequiresAdmin:
                DriverStatusText.Text = "驱动状态: 需要管理员权限";
                DriverStatusText.Foreground = System.Windows.Media.Brushes.Red;
                SetStatus("需要管理员权限");
                System.Windows.MessageBox.Show("需要管理员权限才能安装驱动，请以管理员身份重新运行本程序。");
                break;
            case DriverInputMgr.InstallResult.DllLoadFailed:
                DriverStatusText.Text = "驱动状态: DLL 加载失败";
                DriverStatusText.Foreground = System.Windows.Media.Brushes.Red;
                SetStatus("驱动 DLL 加载失败");
                System.Windows.MessageBox.Show("Interception 驱动 DLL 加载失败，无法继续安装。");
                break;
        }
    }

    /// <summary>连接游戏成功后，用实例级诊断（含设备捕获状态）刷新状态文本。</summary>
    private void UpdateDriverStatusText(DriverInputMgr.Diagnostics diag)
    {
        DriverStatusText.Text = $"驱动状态: {(diag.IsFullyReady ? "就绪" : "未完全就绪")} | {diag.ToFullString()}";
        DriverStatusText.Foreground = diag.IsFullyReady
            ? System.Windows.Media.Brushes.Green
            : System.Windows.Media.Brushes.OrangeRed;
    }

    private async void OnOpenBag(object sender, RoutedEventArgs e)
    {
        if (!EnsureGameAttached()) return;
        _game.WindowMgr.ForgroundWindow();

        try
        {
            await _game!.UIMgr.ShowUI("道具行囊");
            SetStatus("已发送打开背包指令");
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show("打开背包失败: " + ex.Message);
        }
    }

    private async void OnCloseBag(object sender, RoutedEventArgs e)
    {
        if (!EnsureGameAttached()) return;

        try
        {
            await _game!.UIMgr.CloseUI("道具行囊");
            SetStatus("已发送关闭背包指令");
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show("关闭背包失败: " + ex.Message);
        }
    }

    private async void OnUseBagSlot(object sender, RoutedEventArgs e)
    {
        if (!EnsureGameAttached()) return;

        if (!int.TryParse(UseSlotInput.Text, out var slotIndex) || slotIndex <= 0)
        {
            System.Windows.MessageBox.Show("格子编号必须是正整数");
            return;
        }

        if (!int.TryParse(UseCountInput.Text, out var useCount) || useCount <= 0)
        {
            System.Windows.MessageBox.Show("次数必须是正整数");
            return;
        }

        try
        {
            var ok = await _game!.GameControl.UseBagItem(BagType.道具, 1, slotIndex, useCount);
            SetStatus(ok ? $"已尝试使用背包格子 {slotIndex}" : $"使用背包格子 {slotIndex} 失败");
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show("使用背包格子失败: " + ex.Message);
        }
    }

    private bool EnsureGameAttached()
    {
        if (_game != null)
        {
            _game.WindowMgr.ForgroundWindow();
            return true;
        }

        System.Windows.MessageBox.Show("请先连接游戏窗口");
        return false;
    }

    private void OnBringWindowToFront(object sender, RoutedEventArgs e)
    {
        if (WindowsCombo.SelectedItem is not GameWindow gw)
        {
            System.Windows.MessageBox.Show("请先选择窗口");
            return;
        }

        try
        {
            var windowMgr = new WindowMgr();
            windowMgr.Initialize(gw.Handle);
            windowMgr.ForgroundWindow();
            UpdateWindowDebugInfo(gw.Handle);
            SetStatus($"已尝试切换到前台: {gw.Title}");
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show("切换前台失败: " + ex.Message);
        }
    }

    // ================== 窗口选择 ==================

    /// <summary>刷新可用窗口列表。</summary>
    private void OnRefreshWindows(object sender, RoutedEventArgs e) => RefreshWindowsList();

    private void OnWindowSelectionChanged(object sender, SelectionChangedEventArgs e) => UpdateWindowDebugInfo();

    /// <summary>刷新下拉框。</summary>
    private void RefreshWindowsList()
    {
        var list = WindowEnumerator.Enumerate();
        WindowsCombo.ItemsSource = list;
        if (list.Count > 0) WindowsCombo.SelectedIndex = 0;
        UpdateWindowDebugInfo();
    }

    private void UpdateWindowDebugInfo(IntPtr? targetHandle = null)
    {
        var selected = WindowsCombo.SelectedItem as GameWindow;
        var target = targetHandle ?? selected?.Handle ?? IntPtr.Zero;
        var foreground = WindowDebugNative.GetForegroundWindow();
        var targetDesc = DescribeWindow(target);
        var foregroundDesc = DescribeWindow(foreground);
        WindowDebugText.Text = $"目标: {targetDesc} | 前台: {foregroundDesc}";
    }

    private static string DescribeWindow(IntPtr hWnd)
    {
        if (hWnd == IntPtr.Zero) return "无";

        var titleLength = WindowDebugNative.GetWindowTextLength(hWnd);
        var titleBuilder = new System.Text.StringBuilder(Math.Max(titleLength + 1, 64));
        WindowDebugNative.GetWindowText(hWnd, titleBuilder, titleBuilder.Capacity);

        var classBuilder = new System.Text.StringBuilder(256);
        WindowDebugNative.GetClassName(hWnd, classBuilder, classBuilder.Capacity);

        WindowDebugNative.GetWindowThreadProcessId(hWnd, out var pid);
        var procName = string.Empty;
        try
        {
            procName = System.Diagnostics.Process.GetProcessById((int)pid).ProcessName;
        }
        catch
        {
        }

        var title = string.IsNullOrWhiteSpace(titleBuilder.ToString()) ? "<无标题>" : titleBuilder.ToString();
        var className = string.IsNullOrWhiteSpace(classBuilder.ToString()) ? "<无类名>" : classBuilder.ToString();
        return $"0x{hWnd.ToInt64():X} {title} [{className}/{procName}/{pid}]";
    }

    // ================== 截图 ==================

    /// <summary>截图冻结所选窗口。</summary>
    private void OnCapture(object sender, RoutedEventArgs e)
    {
        if (WindowsCombo.SelectedItem is not GameWindow gw)
        {
            System.Windows.MessageBox.Show("请先选择窗口");
            return;
        }
        try
        {
            _capture?.Dispose();
            _capture = WindowCapture.CaptureClient(gw.Handle);
            CaptureImage.Source = BitmapToImageSource(_capture);
            // 让 Canvas 覆盖到与图片一致的尺寸
            OverlayCanvas.Width = _capture.Width;
            OverlayCanvas.Height = _capture.Height;
            RedrawOverlay();
            SetStatus($"截图完成：{_capture.Width} x {_capture.Height}");
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show("截图失败：" + ex.Message);
        }
    }

    /// <summary>切换叠加层可见性。</summary>
    private void OnToggleOverlay(object sender, RoutedEventArgs e)
    {
        _overlayVisible = !_overlayVisible;
        RedrawOverlay();
    }

    /// <summary>Bitmap 转 WPF ImageSource。</summary>
    private static BitmapImage BitmapToImageSource(Bitmap bmp)
    {
        using var ms = new MemoryStream();
        bmp.Save(ms, ImageFormat.Png);
        ms.Position = 0;
        var img = new BitmapImage();
        img.BeginInit();
        img.CacheOption = BitmapCacheOption.OnLoad;
        img.StreamSource = ms;
        img.EndInit();
        img.Freeze();
        return img;
    }

    // ================== 鼠标拖拽选区 ==================

    /// <summary>按下：记录起点，开始画红色矩形。</summary>
    private void OnCanvasMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (_capture == null) return;
        _dragStart = e.GetPosition(OverlayCanvas);
        OverlayCanvas.CaptureMouse();
        RemoveDragRect();
        _dragRect = new ShapeRect
        {
            Stroke = System.Windows.Media.Brushes.Red,
            StrokeThickness = 2,
            StrokeDashArray = new DoubleCollection { 4, 2 },
        };
        Canvas.SetLeft(_dragRect, _dragStart.Value.X);
        Canvas.SetTop(_dragRect, _dragStart.Value.Y);
        OverlayCanvas.Children.Add(_dragRect);
    }

    /// <summary>移动：更新矩形尺寸 + 状态栏坐标。</summary>
    private void OnCanvasMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        var pt = e.GetPosition(OverlayCanvas);
        MousePosText.Text = $"Mouse: ({(int)pt.X}, {(int)pt.Y})";
        if (_dragStart == null || _dragRect == null) return;
        var x = Math.Min(_dragStart.Value.X, pt.X);
        var y = Math.Min(_dragStart.Value.Y, pt.Y);
        var w = Math.Abs(pt.X - _dragStart.Value.X);
        var h = Math.Abs(pt.Y - _dragStart.Value.Y);
        Canvas.SetLeft(_dragRect, x);
        Canvas.SetTop(_dragRect, y);
        _dragRect.Width = w;
        _dragRect.Height = h;
    }

    /// <summary>抬起：结束拖拽，记录选区。</summary>
    private void OnCanvasMouseUp(object sender, MouseButtonEventArgs e)
    {
        OverlayCanvas.ReleaseMouseCapture();
        if (_dragStart == null || _dragRect == null) return;
        var end = e.GetPosition(OverlayCanvas);
        var x = (int)Math.Min(_dragStart.Value.X, end.X);
        var y = (int)Math.Min(_dragStart.Value.Y, end.Y);
        var w = (int)Math.Abs(end.X - _dragStart.Value.X);
        var h = (int)Math.Abs(end.Y - _dragStart.Value.Y);
        _dragStart = null;
        if (w < 2 || h < 2)
        {
            // 视为单击点
            _currentSelection = new Int32Rect(x, y, 1, 1);
            SelectionInfoText.Text = $"点: ({x}, {y})";
            TypePoint.IsChecked = true;
        }
        else
        {
            _currentSelection = new Int32Rect(x, y, w, h);
            SelectionInfoText.Text = $"区域: X={x} Y={y} W={w} H={h}";
            TypeRegion.IsChecked = true;
        }
    }

    /// <summary>清除当前拖拽矩形。</summary>
    private void RemoveDragRect()
    {
        if (_dragRect != null && OverlayCanvas.Children.Contains(_dragRect))
            OverlayCanvas.Children.Remove(_dragRect);
        _dragRect = null;
    }

    // ================== 保存 ==================

    /// <summary>保存到 UILayout.json。</summary>
    private void OnSaveToLayout(object sender, RoutedEventArgs e)
    {
        if (_currentSelection == null) { System.Windows.MessageBox.Show("请先框选一个区域"); return; }
        var key = KeyInput.Text?.Trim();
        if (string.IsNullOrWhiteSpace(key)) { System.Windows.MessageBox.Show("请填写键名"); return; }
        var sel = _currentSelection.Value;
        if (TypePoint.IsChecked == true)
        {
            var cx = sel.X + sel.Width / 2;
            var cy = sel.Y + sel.Height / 2;
            _layout.SetPoint(key, cx, cy);
        }
        else
        {
            _layout.SetRegion(key, sel.X, sel.Y, sel.Width, sel.Height);
        }
        _layout.Save();
        RefreshElementsList();
        RedrawOverlay();
        SetStatus($"已保存 {key} 到 UILayout.json");
    }

    /// <summary>保存为模板 PNG。</summary>
    private void OnSaveAsTemplate(object sender, RoutedEventArgs e)
    {
        if (_capture == null) { System.Windows.MessageBox.Show("请先截图"); return; }
        if (_currentSelection == null) { System.Windows.MessageBox.Show("请先框选区域"); return; }
        var key = KeyInput.Text?.Trim();
        if (string.IsNullOrWhiteSpace(key)) { System.Windows.MessageBox.Show("请填写模板键名"); return; }
        var sel = _currentSelection.Value;
        try
        {
            var path = _templates.SaveTemplate(_capture, new DrawingRect(sel.X, sel.Y, sel.Width, sel.Height), key);
            SetStatus($"模板已保存：{path}");
            RefreshElementsList();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show("保存失败：" + ex.Message);
        }
    }

    /// <summary>删除所选。</summary>
    private void OnDeleteSelected(object sender, RoutedEventArgs e)
    {
        if (ElementsList.SelectedItem is not ElementItem item) return;
        if (item.Kind == "Template")
        {
            _templates.Delete(item.Key);
        }
        else
        {
            _layout.Remove(item.Key);
            _layout.Save();
        }
        RefreshElementsList();
        RedrawOverlay();
        SetStatus($"已删除 {item.Kind}:{item.Key}");
    }

    /// <summary>列表选中：把该元素信息填回选区。</summary>
    private void OnElementSelected(object sender, SelectionChangedEventArgs e)
    {
        if (ElementsList.SelectedItem is not ElementItem item) return;
        KeyInput.Text = item.Key;
        switch (item.Kind)
        {
            case "Region":
                if (_layout.Document.Regions.TryGetValue(item.Key, out var r) && r.Length == 4)
                {
                    _currentSelection = new Int32Rect(r[0], r[1], r[2], r[3]);
                    SelectionInfoText.Text = $"区域: X={r[0]} Y={r[1]} W={r[2]} H={r[3]}";
                    TypeRegion.IsChecked = true;
                }
                break;
            case "Point":
                if (_layout.Document.Points.TryGetValue(item.Key, out var p) && p.Length == 2)
                {
                    _currentSelection = new Int32Rect(p[0], p[1], 1, 1);
                    SelectionInfoText.Text = $"点: ({p[0]}, {p[1]})";
                    TypePoint.IsChecked = true;
                }
                break;
        }
    }

    // ================== 叠加层绘制 ==================

    /// <summary>重绘所有已标定元素到画布。</summary>
    private void RedrawOverlay()
    {
        // 清除除了拖拽矩形之外的所有覆盖
        var toRemove = OverlayCanvas.Children.OfType<UIElement>().Where(el => el != _dragRect).ToList();
        foreach (var el in toRemove) OverlayCanvas.Children.Remove(el);

        if (!_overlayVisible) return;

        // 绘 Region：绿色边框 + 键名标签
        foreach (var kv in _layout.Document.Regions)
        {
            if (kv.Value.Length != 4) continue;
            var rect = new ShapeRect
            {
                Stroke = System.Windows.Media.Brushes.Lime,
                StrokeThickness = 1.5,
                Width = kv.Value[2],
                Height = kv.Value[3],
            };
            Canvas.SetLeft(rect, kv.Value[0]);
            Canvas.SetTop(rect, kv.Value[1]);
            OverlayCanvas.Children.Add(rect);

            var lbl = new TextBlock
            {
                Text = kv.Key,
                Foreground = System.Windows.Media.Brushes.Lime,
                Background = System.Windows.Media.Brushes.Black,
                FontSize = 10,
                Padding = new Thickness(2, 0, 2, 0),
            };
            Canvas.SetLeft(lbl, kv.Value[0]);
            Canvas.SetTop(lbl, Math.Max(0, kv.Value[1] - 14));
            OverlayCanvas.Children.Add(lbl);
        }

        // 绘 Point：黄色十字 + 键名标签
        foreach (var kv in _layout.Document.Points)
        {
            if (kv.Value.Length != 2) continue;
            var cx = kv.Value[0];
            var cy = kv.Value[1];
            OverlayCanvas.Children.Add(MakeLine(cx - 5, cy, cx + 5, cy, System.Windows.Media.Brushes.Yellow));
            OverlayCanvas.Children.Add(MakeLine(cx, cy - 5, cx, cy + 5, System.Windows.Media.Brushes.Yellow));

            var lbl = new TextBlock
            {
                Text = kv.Key,
                Foreground = System.Windows.Media.Brushes.Yellow,
                Background = System.Windows.Media.Brushes.Black,
                FontSize = 10,
                Padding = new Thickness(2, 0, 2, 0),
            };
            Canvas.SetLeft(lbl, cx + 6);
            Canvas.SetTop(lbl, cy + 6);
            OverlayCanvas.Children.Add(lbl);
        }
    }

    /// <summary>构造一根线。</summary>
    private static WpfLine MakeLine(double x1, double y1, double x2, double y2, System.Windows.Media.Brush brush)
        => new()
        {
            X1 = x1, Y1 = y1, X2 = x2, Y2 = y2,
            Stroke = brush,
            StrokeThickness = 1.5,
        };

    // ================== 列表刷新 ==================

    /// <summary>重建左侧标定列表。</summary>
    private void RefreshElementsList()
    {
        _elements.Clear();
        foreach (var k in _layout.Document.Regions.Keys.OrderBy(k => k))
            _elements.Add(new ElementItem("Region", k));
        foreach (var k in _layout.Document.Points.Keys.OrderBy(k => k))
            _elements.Add(new ElementItem("Point", k));
        foreach (var k in _templates.List())
            _elements.Add(new ElementItem("Template", k));
    }

    /// <summary>设置底部状态栏文本。</summary>
    private void SetStatus(string text) => StatusText.Text = text;

    /// <summary>列表项 DTO。</summary>
    public sealed record ElementItem(string Kind, string Key);
}
