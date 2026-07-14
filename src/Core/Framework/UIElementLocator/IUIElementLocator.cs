namespace MengYou.Abstractions;

/// <summary>
/// UI 元素定位抽象：根据元素键名返回屏幕坐标 / 区域。
/// 图像识别方案：从 UILayout.json 读取固定坐标或用模板匹配动态定位。
/// 内存方案：可直接从游戏结构算出。
/// </summary>
public interface IUIElementLocator
{
    /// <summary>定位元素中心点。</summary>
    Models.Point2D? Locate(string elementKey);

    /// <summary>定位元素矩形区域。</summary>
    Models.Rect? LocateRegion(string elementKey);
}
