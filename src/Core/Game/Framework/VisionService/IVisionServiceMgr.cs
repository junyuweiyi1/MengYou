
namespace iFramework;
/// <summary>
/// 视觉门面：模板匹配 + OCR + 像素查询统一入口。
/// </summary>
public interface IVisionServiceMgr
{
    void Initialize(IntPtr hWnd, TemplateMatcher matcher, IOcrEngine ocr);

    /// <summary>指定模板文件是否已经配置。</summary>
    bool HasTemplate(string templateKey);

    //Vector2? FindColor(color, Rect region);
    // 找文本
    Vector2? FindText(string text, Rect rect);
    // 找图
    Vector2? FindImage(string templateKey, double threshold = 0.85, Rect? region = null);
    IReadOnlyList<Vector2> FindImageAll(string templateKey, double threshold = 0.85, Rect? region = null);

    /// <summary>OCR 读取指定区域文字。</summary>
    string? ReadText(Rect region);

    /// <summary>OCR 读取指定区域数字（内部对数字做校正）。</summary>
    int? ReadNumber(Rect region);

    /// <summary>读取像素颜色（ARGB）。</summary>
    int ReadPixel(Vector2 point);

    /// <summary>触发一次截图刷新（供上层控制识别频率）。</summary>
    void Refresh();

    void Dispose();
}
