using MengYou.Abstractions.Models;

namespace MengYou.Abstractions.Events;

/// <summary>HP 变化事件。</summary>
public sealed class HpChangedEvent : GameEvent
{
    /// <summary>变化前 HP。</summary>
    public int OldHp { get; init; }

    /// <summary>变化后 HP。</summary>
    public int NewHp { get; init; }

    /// <summary>最大 HP。</summary>
    public int MaxHp { get; init; }
}

/// <summary>MP 变化事件。</summary>
public sealed class MpChangedEvent : GameEvent
{
    /// <summary>变化前 MP。</summary>
    public int OldMp { get; init; }

    /// <summary>变化后 MP。</summary>
    public int NewMp { get; init; }

    /// <summary>最大 MP。</summary>
    public int MaxMp { get; init; }
}

/// <summary>场景类型变化事件。</summary>
public sealed class SceneChangedEvent : GameEvent
{
    /// <summary>旧场景。</summary>
    public SceneType Old { get; init; }

    /// <summary>新场景。</summary>
    public SceneType New { get; init; }
}

/// <summary>地图变化事件。</summary>
public sealed class MapChangedEvent : GameEvent
{
    /// <summary>旧地图 ID。</summary>
    public string OldMapId { get; init; } = string.Empty;

    /// <summary>新地图 ID。</summary>
    public string NewMapId { get; init; } = string.Empty;
}

/// <summary>战斗开始事件。</summary>
public sealed class CombatStartedEvent : GameEvent { }

/// <summary>战斗结束事件。</summary>
public sealed class CombatEndedEvent : GameEvent { }

/// <summary>对话弹出事件。</summary>
public sealed class DialogAppearedEvent : GameEvent
{
    /// <summary>对话内容快照。</summary>
    public DialogInfo Dialog { get; init; } = new();
}
