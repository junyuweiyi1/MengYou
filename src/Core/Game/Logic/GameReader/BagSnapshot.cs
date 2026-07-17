/// <summary>
/// 背包中的物品条目快照。
/// </summary>
public sealed class BagItemSnapshot
{
    public BagType BagType { get; init; }

    /// <summary>物品名。</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>数量。</summary>
    public int Count { get; init; }

    /// <summary>在第几个背包。</summary>
    public int BagIndex { get; init; }

    /// <summary>格子索引（用于点击定位）。</summary>
    public int SlotIndex { get; init; }
}

/// <summary>
/// 背包快照。
/// </summary>
public sealed class BagSnapshot
{
    /// <summary>所有物品条目。</summary>
    public IReadOnlyList<BagItemSnapshot> Items { get; init; } = Array.Empty<BagItemSnapshot>();
}
public enum BagType
{
    道具,
    行囊,
}
