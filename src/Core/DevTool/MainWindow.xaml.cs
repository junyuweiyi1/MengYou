using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MengYou.DevTool.Services;
using MengYou.Platform.Win32;
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
        // 若通过 dotnet run 运行，位于 bin/Debug/net8.0-windows；向上找 3 级到 src/MengYou.DevTool
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
        SetStatus("初始化完成");
    }

    /// <summary>查找主应用 App 的 Config 目录：优先使用同 solution 下的 MengYou.App/Config。</summary>
    private static string FindAppConfigDir(string devToolBaseDir)
    {
        // 尝试：向上找到 src 目录，再定位到 MengYou.App/Config
        var dir = new DirectoryInfo(devToolBaseDir);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "src", "MengYou.App", "Config");
            if (Directory.Exists(candidate)) return candidate;
            dir = dir.Parent;
        }
        // 回落到 DevTool 自己的 Config
        return Path.Combine(devToolBaseDir, "Config");
    }

    // ================== 窗口选择 ==================

    /// <summary>刷新可用窗口列表。</summary>
    private void OnRefreshWindows(object sender, RoutedEventArgs e) => RefreshWindowsList();

    /// <summary>刷新下拉框。</summary>
    private void RefreshWindowsList()
    {
        var list = WindowEnumerator.Enumerate();
        WindowsCombo.ItemsSource = list;
        if (list.Count > 0) WindowsCombo.SelectedIndex = 0;
    }

    // ================== 截图 ==================

    /// <summary>截图冻结所选窗口。</summary>
    private void OnCapture(object sender, RoutedEventArgs e)
    {
        if (WindowsCombo.SelectedItem is not GameWindow gw)
        {
            MessageBox.Show("请先选择窗口");
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
            MessageBox.Show("截图失败：" + ex.Message);
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
    private void OnCanvasMouseMove(object sender, MouseEventArgs e)
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
        if (_currentSelection == null) { MessageBox.Show("请先框选一个区域"); return; }
        var key = KeyInput.Text?.Trim();
        if (string.IsNullOrWhiteSpace(key)) { MessageBox.Show("请填写键名"); return; }
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
        if (_capture == null) { MessageBox.Show("请先截图"); return; }
        if (_currentSelection == null) { MessageBox.Show("请先框选区域"); return; }
        var key = KeyInput.Text?.Trim();
        if (string.IsNullOrWhiteSpace(key)) { MessageBox.Show("请填写模板键名"); return; }
        var sel = _currentSelection.Value;
        try
        {
            var path = _templates.SaveTemplate(_capture, new DrawingRect(sel.X, sel.Y, sel.Width, sel.Height), key);
            SetStatus($"模板已保存：{path}");
            RefreshElementsList();
        }
        catch (Exception ex)
        {
            MessageBox.Show("保存失败：" + ex.Message);
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
