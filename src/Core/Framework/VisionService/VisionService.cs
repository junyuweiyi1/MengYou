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

    private bool _disposed;

    /// <summary>构造。</summary>
    public VisionService(IntPtr hWnd, TemplateMatcher matcher, IOcrEngine ocr)
    {
        _hWnd = hWnd;
        _matcher = matcher;
        _ocr = ocr;
    }

    public bool CanReadText => _ocr.IsAvailable;

    /// <summary>在缓存锁内使用当前帧，避免 Refresh/Dispose 释放正在识别的位图。</summary>
    private T WithCurrent<T>(Func<Bitmap, T> operation)
    {
        lock (_sync)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            _current ??= WindowCapture.CaptureClient(_hWnd);
            return operation(_current);
        }
    }

    /// <inheritdoc/>
    public void Refresh()
    {
        lock (_sync)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            _current?.Dispose();
            _current = null;
        }
    }

    /// <inheritdoc/>
    public Vector2? FindTemplate(string templateKey, double threshold = 0.85)
        => WithCurrent(frame => _matcher.FindOne(frame, templateKey, threshold));

    /// <inheritdoc/>
    public IReadOnlyList<Vector2> FindTemplateAll(string templateKey, double threshold = 0.85)
        => WithCurrent(frame => _matcher.FindAll(frame, templateKey, threshold));

    /// <inheritdoc/>
    public string? ReadText(Rect region)
    {
        return WithCurrent(frame =>
        {
            using var crop = CropRegion(frame, region);
            var text = _ocr.Recognize(crop);
            return string.IsNullOrWhiteSpace(text) ? null : text;
        });
    }

    /// <inheritdoc/>
    public int? ReadNumber(Rect region)
    {
        return WithCurrent(frame =>
        {
            using var crop = CropRegion(frame, region);
            return _ocr.RecognizeNumber(crop);
        });
    }

    /// <inheritdoc/>
    public int GetPixel(Vector2 point)
    {
        return WithCurrent(frame =>
        {
            if (point.X < 0 || point.Y < 0 || point.X >= frame.Width || point.Y >= frame.Height) return 0;
            return frame.GetPixel(point.X, point.Y).ToArgb();
        });
    }

    /// <summary>按区域裁剪当前帧；返回新的 Bitmap（调用者需 Dispose）。</summary>
    private static Bitmap CropRegion(Bitmap frame, Rect region)
    {
        var requested = new Rectangle(region.X, region.Y, region.Width, region.Height);
        var rect = Rectangle.Intersect(requested, new Rectangle(0, 0, frame.Width, frame.Height));
        if (rect.Width <= 0 || rect.Height <= 0)
            throw new ArgumentOutOfRangeException(nameof(region), "识别区域不在当前帧范围内。");
        return frame.Clone(rect, frame.PixelFormat);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        lock (_sync)
        {
            if (_disposed) return;
            _disposed = true;
            _current?.Dispose();
            _current = null;
        }

        _matcher.Dispose();
        if (_ocr is IDisposable disposableOcr)
            disposableOcr.Dispose();
    }
}
