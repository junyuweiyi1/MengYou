namespace MengYou.Abstractions.Events;

/// <summary>
/// 所有游戏事件的基类：便于统一订阅与日志。
/// </summary>
public abstract class GameEvent
{
    /// <summary>事件产生时刻（UTC）。</summary>
    public DateTime TimestampUtc { get; } = DateTime.UtcNow;
}
