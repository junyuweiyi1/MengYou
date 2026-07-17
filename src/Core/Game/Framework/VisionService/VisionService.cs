
namespace iFramework;
/// <summary>
/// 视觉服务实现：封装模板匹配、OCR、像素查询。
/// 内部维护"当前帧"截图缓存，Refresh() 刷新。
/// </summary>
public sealed class VisionService : IVisionServiceMgr, IDisposable
{
    /// <summary>关联窗口句柄。</summary>
    private IntPtr _hWnd;

    /// <summary>模板匹配器。</summary>
    private TemplateMatcher _matcher = null!;

    /// <summary>OCR 引擎。</summary>
    private IOcrEngine _ocr = null!;



    public void Initialize(IntPtr hWnd, TemplateMatcher matcher, IOcrEngine ocr)
    {
        _hWnd = hWnd;
        _matcher = matcher;
        _ocr = ocr;
    }

    public bool HasTemplate(string templateKey) => _matcher.HasTemplate(templateKey);

    public Bitmap NewBitmap()
    {
        return WindowCapture.CaptureClient(_hWnd);
    }

    /// <inheritdoc/>
    public void Refresh()
    {
    }

    //// 找色
    //public Vector2? FindColor(color, Rect region) 
    //{
    //}

    // 找文本
    public Vector2? FindText(string text, Rect rect)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;

        using var bitmap = NewBitmap();
        using var crop = CropRegion(bitmap, rect);
        var match = _ocr.RecognizeAll(crop)
            .FirstOrDefault(result => result.Text.Contains(text, StringComparison.OrdinalIgnoreCase));

        return string.IsNullOrEmpty(match.Text)
            ? null
            : new Vector2(
                rect.X + match.Bounds.X + match.Bounds.Width / 2,
                rect.Y + match.Bounds.Y + match.Bounds.Height / 2);
    }

    // 找图
    public Vector2? FindImage(string templateKey, double threshold = 0.85, Rect? region = null)
    {
        using var frame = NewBitmap();
        using var crop = region is { } requestedRegion ? CropRegion(frame, requestedRegion) : null;
        var result = _matcher.FindOne(crop ?? frame, templateKey, threshold);
        return result is { } point && region is { } offset
            ? new Vector2(point.x + offset.X, point.y + offset.Y)
            : result;
    }

    /// <inheritdoc/>
    public IReadOnlyList<Vector2> FindImageAll(string templateKey, double threshold = 0.85, Rect? region = null)
    {
        using var frame = NewBitmap();
        using var crop = region is { } requestedRegion ? CropRegion(frame, requestedRegion) : null;
        var result = _matcher.FindAll(crop ?? frame, templateKey, threshold);
        return region is not { } offset
            ? result
            : result.Select(point => new Vector2(point.x + offset.X, point.y + offset.Y)).ToArray();
    }

    /// <inheritdoc/>
    public string? ReadText(Rect region)
    {
        using var bitmap = NewBitmap();
        using var crop = CropRegion(bitmap, region);
        var results = _ocr.RecognizeAll(crop);
        var text = results.Count == 0
            ? _ocr.Recognize(crop)
            : string.Join(Environment.NewLine, results.Select(result => result.Text));
        return string.IsNullOrWhiteSpace(text) ? null : text;
    }

    /// <inheritdoc/>
    public int? ReadNumber(Rect region)
    {
        using var bitmap = NewBitmap();
        using var crop = CropRegion(bitmap, region);
        return _ocr.RecognizeNumber(crop);
    }

    /// <inheritdoc/>
    public int ReadPixel(Vector2 point)
    {
        using var bitmap = NewBitmap();
        if (point.x < 0 || point.y < 0 || point.x >= bitmap.Width || point.y >= bitmap.Height) return 0;
        return bitmap.GetPixel(point.x, point.y).ToArgb();
    }

    /// <summary>按区域裁剪；返回新的 Bitmap（调用者需 Dispose）。</summary>
    private Bitmap CropRegion(Bitmap frame, Rect region)
    {
        ArgumentNullException.ThrowIfNull(frame);
        if (region.Width <= 0 || region.Height <= 0)
            throw new ArgumentOutOfRangeException(nameof(region), "裁剪区域的宽度和高度必须大于 0。");

        var requested = new Rectangle(region.X, region.Y, region.Width, region.Height);
        var frameBounds = new Rectangle(0, 0, frame.Width, frame.Height);
        var clipped = Rectangle.Intersect(requested, frameBounds);
        if (clipped.Width <= 0 || clipped.Height <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(region),
                $"裁剪区域 {requested} 不在截图范围 {frameBounds} 内。");

        // Bitmap.Clone 对越界或部分像素格式会抛出误导性的 OutOfMemoryException。
        // 显式绘制既支持区域裁剪，也避免对截图像素格式的依赖。
        var crop = new Bitmap(clipped.Width, clipped.Height);
        using var graphics = Graphics.FromImage(crop);
        graphics.DrawImage(frame, new Rectangle(0, 0, crop.Width, crop.Height), clipped, GraphicsUnit.Pixel);
        return crop;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _matcher.Dispose();
        if (_ocr is IDisposable disposableOcr) disposableOcr.Dispose();
    }
}
