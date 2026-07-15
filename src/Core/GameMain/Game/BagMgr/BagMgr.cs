using iFramework;
using System.Collections.Generic;

public sealed class BagMgr
{
    private readonly Game _game;
    private List<BagItem> _items = new List<BagItem>();

    public BagMgr(Game game)
    {
        _game = game;
    }

    public async void RefreshData()
    {
        _items.Clear();

        var bagSnapshot = await _game.GameReader.GetBagSnapshot();
        foreach (var itemSnapshot in bagSnapshot.Items)
        {
            var bagItem = new BagItem()
            {
                BagType = itemSnapshot.Type,
                Name = itemSnapshot.Name,
                Count = itemSnapshot.Count,
                BagIndex = itemSnapshot.BagIndex,
                SlotIndex = itemSnapshot.SlotIndex,
            };
            _items.Add(bagItem);
        }
    }

    public BagItem GetItem(BagType bagType, string itemName)
    {
        foreach (var item in _items)
        {
            if (item.BagType == bagType && item.Name = itemName)
                return item;
        }
        return null;
    }

    /// <summary>
    /// 使用道具物品。
    /// </summary>
    public async Task<bool> UseItem(string itemName, int useCount)
    {
        if (string.IsNullOrEmpty(itemName)) return false;

        var item = GetItem(itemName);
        if (item == null || item.Count == 0) return false;

        return await _game.GameControl.UseBagItem(BagType.道具, item.BagIndex, item.SlotIndex, useCount);
    }
}
