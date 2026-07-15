namespace iFramework;

/// <summary>
/// UI 布局定位器：从 JSON 读取固定坐标（单版本单分辨率方案）。
/// 未来扩展点：可派生 MultiResolutionLayoutProvider 支持多分辨率。
/// </summary>
public sealed class UIElementLocateMgr : IUIElementLocateMgr
{
    /// <summary>元素→坐标映射。</summary>
    private readonly Dictionary<string, Point2D> _points;

    /// <summary>元素→区域映射。</summary>
    private readonly Dictionary<string, Rect> _regions;

    /// <summary>构造。</summary>
    private UIElementLocateMgr(Dictionary<string, Point2D> points, Dictionary<string, Rect> regions)
    {
        _points = points;
        _regions = regions;
    }

    /// <summary>从 JSON 文件加载布局。</summary>
    public static UIElementLocateMgr LoadFromFile(string path)
    {
        var json = File.ReadAllText(path);
        var doc = JsonDocument.Parse(json);
        var points = new Dictionary<string, Point2D>(StringComparer.OrdinalIgnoreCase);
        var regions = new Dictionary<string, Rect>(StringComparer.OrdinalIgnoreCase);

        if (doc.RootElement.TryGetProperty("points", out var pts))
        {
            foreach (var kv in pts.EnumerateObject())
            {
                var arr = kv.Value;
                points[kv.Name] = new Point2D(arr[0].GetInt32(), arr[1].GetInt32());
            }
        }
        if (doc.RootElement.TryGetProperty("regions", out var rgs))
        {
            foreach (var kv in rgs.EnumerateObject())
            {
                var arr = kv.Value;
                regions[kv.Name] = new Rect(arr[0].GetInt32(), arr[1].GetInt32(), arr[2].GetInt32(), arr[3].GetInt32());
            }
        }
        return new UIElementLocateMgr(points, regions);
    }

    /// <inheritdoc/>
    public Point2D? Locate(string elementKey)
        => _points.TryGetValue(elementKey, out var p) ? p : null;

    /// <inheritdoc/>
    public Rect? LocateRegion(string elementKey)
        => _regions.TryGetValue(elementKey, out var r) ? r : null;
}
