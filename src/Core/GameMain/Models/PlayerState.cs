namespace MengYou.Abstractions.Models;

/// <summary>
/// 玩家状态快照：Reader 每次读取的结果聚合体。
/// </summary>
public sealed class PlayerState
{
    /// <summary>当前生命值。</summary>
    public int Hp { get; init; }

    /// <summary>最大生命值。</summary>
    public int MaxHp { get; init; }

    /// <summary>当前法力值。</summary>
    public int Mp { get; init; }

    /// <summary>最大法力值。</summary>
    public int MaxMp { get; init; }

    /// <summary>等级。</summary>
    public int Level { get; init; }

    /// <summary>所在地图名。</summary>
    public string MapName { get; init; } = string.Empty;

    /// <summary>当前地图内坐标。</summary>
    public Point2D MapPosition { get; init; }

    /// <summary>是否存活。</summary>
    public bool IsAlive => Hp > 0;

    /// <summary>血量百分比（0~1）。</summary>
    public double HpPercent => MaxHp <= 0 ? 0 : (double)Hp / MaxHp;

    /// <summary>蓝量百分比（0~1）。</summary>
    public double MpPercent => MaxMp <= 0 ? 0 : (double)Mp / MaxMp;
}
