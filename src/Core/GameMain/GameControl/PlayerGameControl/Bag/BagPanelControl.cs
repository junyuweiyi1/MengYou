using iFramework;
using System.Xml.Linq;

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
        var showUISuc = await _game.UIMgr.ShowUI("道具行囊", ct);
        if (!showUISuc)
            return false;

        var chooseItemBagTypeSuc = await ChooseBagTypeTab(bagType, ct);
        if (!chooseItemBagTypeSuc)
            return false;

        var region = _game.UIElementLocateMgr.LocateRegion($"BagSlot.{slotIndex}.Icon");
        if (region == null)
            return false;

        for (int i = 0; i < useCount; i++)
        {
            await _game.InputMgr.ClickAsync(region.Value.Center, MouseButton.Right, ct);
            await Task.Delay(500, ct);
        }

        await _game.UIMgr.CloseUI("道具行囊", ct);

        await Task.Delay(300, ct);
        return true;
    }

    public async Task<bool> ChooseBagTypeTab(BagType bagType, CancellationToken ct = default)
    {
        var key = bagType == BagType.道具 ? "BagTab.Item" : "BagTab.Package";
        var itemBagBtnRegion = _game.UIElementLocateMgr.LocateRegion(key);
        if (itemBagBtnRegion == null)
            return bagType == BagType.道具;

        await _game.InputMgr.ClickAsync(itemBagBtnRegion.Value.Center, MouseButton.Left, ct);
        await Task.Delay(1000, ct);
        return true;
    }
}
