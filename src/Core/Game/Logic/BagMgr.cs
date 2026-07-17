using iFramework;
using System.Collections.Generic;

public sealed class BagMgr
{
    private readonly Game _game;
    private IReadOnlyList<BagItemSnapshot> _items = Array.Empty<BagItemSnapshot>();

    public BagMgr(Game game)
    {
        _game = game;
    }

    /// <summary>
    /// 使用道具物品。
    /// </summary>
    public async Task RefreshData(CancellationToken ct = default)
    {
        _items = (await _game.Logic.GameReader.GetBagSnapshot(ct).ConfigureAwait(false)).Items;
    }

    public BagItemSnapshot? GetItem(BagType bagType, string itemName)
        => _items.FirstOrDefault(item => item.BagType == bagType
            && string.Equals(item.Name, itemName, StringComparison.OrdinalIgnoreCase));

    public async Task<bool> UseItem(string itemName, int useCount, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(itemName) || useCount <= 0) return false;

        // 物品名识别必须在背包已经打开的画面上进行。
        var opened = await _game.FW.UIMgr.ShowUI("道具行囊", ct).ConfigureAwait(false);
        if (!opened) return false;

        try
        {
            var itemTab = _game.FW.UIElementLocateMgr.LocateRegion("BagTab.Item");
            if (itemTab is not null)
            {
                await _game.FW.InputMgr.ClickAsync(itemTab.Value.Center, MouseButton.Left, ct).ConfigureAwait(false);
                await Task.Delay(300, ct).ConfigureAwait(false);
            }
            await RefreshData(ct).ConfigureAwait(false);
            var item = GetItem(BagType.道具, itemName.Trim());
            if (item is null || item.Count <= 0) return false;

            var actualCount = Math.Min(useCount, item.Count);
            return await _game.Logic.GameControl.UseBagItem(
                BagType.道具,
                item.BagIndex,
                item.SlotIndex,
                actualCount,
                ct).ConfigureAwait(false);
        }
        finally
        {
            await TryCloseBagAsync().ConfigureAwait(false);
        }
    }

    private async Task TryCloseBagAsync()
    {
        try { await _game.FW.UIMgr.CloseUI("道具行囊", CancellationToken.None).ConfigureAwait(false); }
        catch { /* 主操作结果优先，关闭失败由下一次 UI 状态检查修复。 */ }
    }
}
