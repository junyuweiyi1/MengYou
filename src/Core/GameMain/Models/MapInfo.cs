namespace MengYou.Abstractions.Models;

/// <summary>
/// 地图信息：当前所在地图 + 坐标。
/// </summary>
public sealed class MapInfo
{
    /// <summary>地图 ID（图数据的稳定键）。</summary>
    public string MapId { get; init; } = string.Empty;

    /// <summary>地图显示名（如"长安"）。</summary>
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>玩家当前所在坐标。</summary>
    public Point2D Position { get; init; }
}
