namespace MengYou.Abstractions.Models;

/// <summary>
/// 背包中的物品条目。
/// </summary>
public sealed class BagItem
{
    /// <summary>物品名。</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>数量。</summary>
    public int Count { get; init; }

    /// <summary>格子索引（用于点击定位）。</summary>
    public int SlotIndex { get; init; }
}

/// <summary>
/// 背包快照。
/// </summary>
public sealed class BagState
{
    /// <summary>所有物品条目。</summary>
    public IReadOnlyList<BagItem> Items { get; init; } = Array.Empty<BagItem>();

    /// <summary>按名称获取数量，不存在返回 0。</summary>
    public int CountOf(string itemName)
    {
        var total = 0;
        foreach (var it in Items)
        {
            if (it.Name == itemName) total += it.Count;
        }
        return total;
    }
}
