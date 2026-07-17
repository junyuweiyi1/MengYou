namespace iFramework;

/// <summary>
/// 视觉服务实现：封装模板匹配、OCR、像素查询。
/// 内部维护"当前帧"截图缓存，Refresh() 刷新。
/// </summary>
public sealed class VisionService : IVisionServiceMgr, IDisposable
{
    /// <summary>关联窗口句柄。</summary>
    private readonly IntPtr _hWnd;

    /// <summary>模板匹配器。</summary>
    private readonly TemplateMatcher _matcher;

    /// <summary>OCR 引擎。</summary>
    private readonly IOcrEngine _ocr;

    /// <summary>当前帧缓存。</summary>
    private Bitmap? _current;

    /// <summary>缓存同步锁。</summary>
    private readonly object _sync = new();

    /// <summary>构造。</summary>
    public VisionService(IntPtr hWnd, TemplateMatcher matcher, IOcrEngine ocr)
    {
        _hWnd = hWnd;
        _matcher = matcher;
        _ocr = ocr;
    }

    /// <summary>惰性获取当前帧。</summary>
    private Bitmap Current()
    {
        lock (_sync)
        {
            _current ??= WindowCapture.CaptureClient(_hWnd);
            return _current;
        }
    }

    /// <inheritdoc/>
    public void Refresh()
    {
        lock (_sync)
        {
            _current?.Dispose();
            _current = null;
        }
    }

    /// <inheritdoc/>
    public Vector2? FindTemplate(string templateKey, double threshold = 0.85)
        => _matcher.FindOne(Current(), templateKey, threshold);

    /// <inheritdoc/>
    public IReadOnlyList<Vector2> FindTemplateAll(string templateKey, double threshold = 0.85)
        => _matcher.FindAll(Current(), templateKey, threshold);

    /// <inheritdoc/>
    public string? ReadText(Rect region)
    {
        using var crop = CropRegion(region);
        var text = _ocr.Recognize(crop);
        return string.IsNullOrWhiteSpace(text) ? null : text;
    }

    /// <inheritdoc/>
    public int? ReadNumber(Rect region)
    {
        using var crop = CropRegion(region);
        return _ocr.RecognizeNumber(crop);
    }

    /// <inheritdoc/>
    public int GetPixel(Vector2 point)
    {
        var frame = Current();
        if (point.X < 0 || point.Y < 0 || point.X >= frame.Width || point.Y >= frame.Height) return 0;
        return frame.GetPixel(point.X, point.Y).ToArgb();
    }

    /// <summary>按区域裁剪当前帧；返回新的 Bitmap（调用者需 Dispose）。</summary>
    private Bitmap CropRegion(Rect region)
    {
        var frame = Current();
        var rect = new Rectangle(region.X, region.Y, region.Width, region.Height);
        return frame.Clone(rect, frame.PixelFormat);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _current?.Dispose();
    }
}
