using iFramework;
using System.Collections.Generic;
public enum BagType
{
    道具,
    行囊,
}

public class BagItem
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