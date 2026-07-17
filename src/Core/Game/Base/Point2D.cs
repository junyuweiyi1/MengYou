using System.Runtime.CompilerServices;
/// <summary>
/// 二维整型坐标点：屏幕像素点或地图格子坐标皆可使用。
/// </summary>
/// <param name="X">横坐标。</param>
/// <param name="Y">纵坐标。</param>
public struct Vector2
{
    public int x;
    public int y;

    public Vector2(int x, int y)
    {
        this.x = x;
        this.y = y;
    }


    public static readonly Vector2 Zero = new(0, 0);

    public static implicit operator Vector2((int X, int Y) value) => new(value.X, value.Y);
    public static Vector2 operator +(Vector2 a, Vector2 b)
    {
        return new Vector2(a.x + b.x, a.y + b.y);
    }

    public static Vector2 operator -(Vector2 a, Vector2 b)
    {
        return new Vector2(a.x - b.x, a.y - b.y);
    }

    public static Vector2 operator *(Vector2 a, Vector2 b)
    {
        return new Vector2(a.x * b.x, a.y * b.y);
    }

    public static Vector2 operator /(Vector2 a, Vector2 b)
    {
        return new Vector2(a.x / b.x, a.y / b.y);
    }

    public override string ToString() => $"({x},{y})";
}

/// <summary>
/// 矩形区域：常用于图像裁剪、UI 元素范围。
/// </summary>
/// <param name="X">左上角横坐标。</param>
/// <param name="Y">左上角纵坐标。</param>
/// <param name="Width">宽度。</param>
/// <param name="Height">高度。</param>
public struct Rect
{
    public int X;
    public int Y;
    public int Width;
    public int Height;

    public Rect(int X, int Y, int Width, int Height)
    {
        this.X = X;
        this.Y = Y;
        this.Width = Width;
        this.Height = Height;
    }

    /// <summary>获取中心点。</summary>
    public Vector2 Center => new(X + Width / 2, Y + Height / 2);
}
