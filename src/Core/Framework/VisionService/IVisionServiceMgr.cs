namespace iFramework;

/// <summary>
/// 视觉门面：模板匹配 + OCR + 像素查询统一入口。
/// 仅在图像识别后端使用；内存后端可不实现。
/// </summary>
public interface IVisionServiceMgr : IDisposable
{
    /// <summary>当前视觉后端是否具备文字 OCR 能力。</summary>
    bool CanReadText { get; }

    /// <summary>在当前画面查找模板；未找到返回 null。</summary>
    Vector2? FindTemplate(string templateKey, double threshold = 0.85);

    /// <summary>查找模板所有出现位置。</summary>
    IReadOnlyList<Vector2> FindTemplateAll(string templateKey, double threshold = 0.85);

    /// <summary>OCR 读取指定区域文字。</summary>
    string? ReadText(Rect region);

    /// <summary>OCR 读取指定区域数字（内部对数字做校正）。</summary>
    int? ReadNumber(Rect region);

    /// <summary>读取像素颜色（ARGB）。</summary>
    int GetPixel(Vector2 point);

    /// <summary>触发一次截图刷新（供上层控制识别频率）。</summary>
    void Refresh();
}
