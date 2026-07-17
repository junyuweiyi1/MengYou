/// <summary>
/// 背包使用器
/// </summary>
public sealed class BagPanelControl
{
    private readonly Game _game;

    public BagPanelControl(Game game)
    {
        _game = game;
    }

    public async Task<bool> UseItem(BagType bagType, int bagIndex, int slotIndex, int useCount, CancellationToken ct = default)
    {
        if (bagIndex != 1 || slotIndex <= 0 || useCount <= 0)
            return false;

        var showUISuc = await _game.FW.UIMgr.ShowUI("道具行囊", ct);
        if (!showUISuc)
            return false;

        try
        {
            var chooseItemBagTypeSuc = await ChooseBagTypeTab(bagType, ct);
            if (!chooseItemBagTypeSuc)
                return false;

            var region = _game.FW.UIElementLocateMgr.LocateRegion($"BagSlot.{slotIndex}.Icon");
            if (region == null)
                return false;

            for (var i = 0; i < useCount; i++)
            {
                await _game.FW.InputMgr.ClickAsync(region.Value.Center, MouseButton.Right, ct);
                await Task.Delay(500, ct);
            }

            await Task.Delay(300, ct);
            return true;
        }
        finally
        {
            try { await _game.FW.UIMgr.CloseUI("道具行囊", CancellationToken.None); }
            catch { /* 不掩盖物品使用结果。 */ }
        }
    }

    public async Task<bool> ChooseBagTypeTab(BagType bagType, CancellationToken ct = default)
    {
        var key = bagType == BagType.道具 ? "BagTab.Item" : "BagTab.Package";
        var itemBagBtnRegion = _game.FW.UIElementLocateMgr.LocateRegion(key);
        if (itemBagBtnRegion == null)
            return bagType == BagType.道具;

        await _game.FW.InputMgr.ClickAsync(itemBagBtnRegion.Value.Center, MouseButton.Left, ct);
        await Task.Delay(1000, ct);
        return true;
    }
}
