namespace MengYou.Modules.Path.World;

/// <summary>
/// 地图节点连接类型：决定使用什么方式跨图。
/// </summary>
public enum ConnectionType
{
    /// <summary>走路（同图内可达或相邻图）。</summary>
    Walk,
    /// <summary>飞行符。</summary>
    FlyCharm,
    /// <summary>飞行旗。</summary>
    FlyFlag,
    /// <summary>NPC 传送。</summary>
    TeleportNpc,
}

/// <summary>地图之间的一条边。</summary>
public sealed class MapConnection
{
    /// <summary>目标地图 ID。</summary>
    public string To { get; set; } = string.Empty;

    /// <summary>连接类型。</summary>
    public ConnectionType Type { get; set; }

    /// <summary>关联 NPC 名（TeleportNpc 用）。</summary>
    public string? Npc { get; set; }

    /// <summary>关联物品名（FlyCharm/FlyFlag 用）。</summary>
    public string? Item { get; set; }

    /// <summary>预估成本（时间秒 / 资源消耗）。</summary>
    public double Cost { get; set; } = 1.0;
}

/// <summary>地图节点。</summary>
public sealed class MapNode
{
    /// <summary>地图 ID。</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>显示名。</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>出边列表。</summary>
    public List<MapConnection> Connections { get; set; } = new();
}
