/// <summary>
/// 二维整型坐标点：屏幕像素点或地图格子坐标皆可使用。
/// </summary>
/// <param name="X">横坐标。</param>
/// <param name="Y">纵坐标。</param>
public struct Point2D(int X, int Y)
{
    /// <summary>零点常量。</summary>
    public static readonly Point2D Zero = new(0, 0);

    /// <summary>转为字符串用于日志显示。</summary>
    public override string ToString() => $"({X},{Y})";
}

/// <summary>
/// 矩形区域：常用于图像裁剪、UI 元素范围。
/// </summary>
/// <param name="X">左上角横坐标。</param>
/// <param name="Y">左上角纵坐标。</param>
/// <param name="Width">宽度。</param>
/// <param name="Height">高度。</param>
public struct Rect(int X, int Y, int Width, int Height)
{
    /// <summary>获取中心点。</summary>
    public Point2D Center => new(X + Width / 2, Y + Height / 2);
}
