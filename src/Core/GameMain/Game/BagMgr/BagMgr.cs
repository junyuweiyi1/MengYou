using iFramework;
using System.Collections.Generic;

public sealed class BagMgr
{
    private readonly Game _game;
    private IReadOnlyList<BagItem> _items = Array.Empty<BagItem>();

    public BagMgr(Game game)
    {
        _game = game;
    }

    public async Task RefreshData(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var items = new List<BagItem>();

        var bagSnapshot = await _game.GameReader
            .GetBagSnapshot(ct)
            .ConfigureAwait(false);
        foreach (var itemSnapshot in bagSnapshot.Items)
        {
            var bagItem = new BagItem()
            {
                BagType = itemSnapshot.BagType,
                Name = itemSnapshot.Name,
                Count = itemSnapshot.Count,
                BagIndex = itemSnapshot.BagIndex,
                SlotIndex = itemSnapshot.SlotIndex,
            };
            items.Add(bagItem);
        }

        Volatile.Write(ref _items, items);
    }

    public BagItem GetItem(BagType bagType, string itemName)
    {
        var items = Volatile.Read(ref _items);
        foreach (var item in items)
        {
            if (item.BagType == bagType && string.Equals(item.Name, itemName, StringComparison.OrdinalIgnoreCase))
                return item;
        }
        return null!;
    }

    /// <summary>
    /// 使用道具物品。
    /// </summary>
    public async Task<bool> UseItem(
        string itemName,
        int useCount,
        CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(itemName)) return false;
        if (useCount <= 0) return false;

        var item = GetItem(BagType.道具, itemName);
        if (item == null || item.Count == 0) return false;

        return await _game.GameControl
            .UseBagItem(BagType.道具, item.BagIndex, item.SlotIndex, useCount, ct)
            .ConfigureAwait(false);
    }
}
