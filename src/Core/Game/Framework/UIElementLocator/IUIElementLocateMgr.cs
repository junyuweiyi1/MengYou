
namespace iFramework;
/// <summary>
/// UI 元素定位抽象：根据元素键名返回屏幕坐标 / 区域。
/// 图像识别方案：从 UILayout.json 读取固定坐标或用模板匹配动态定位。
/// 内存方案：可直接从游戏结构算出。
/// </summary>
public interface IUIElementLocateMgr
{
    /// <summary>定位元素中心点。</summary>
    Vector2? Locate(string elementKey);

    /// <summary>定位元素矩形区域。</summary>
    Rect? LocateRegion(string elementKey);

    void Initialize(string path);

    void Dispose();
}
