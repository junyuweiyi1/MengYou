using iFramework;

/// <summary>
/// 背包使用器
/// </summary>
public sealed class BagPanelControl
{
    private readonly IUIMgr _uiMgr;
    private readonly IInputMgr _input;
    private readonly IUIElementLocateMgr _locator;

    public BagPanelControl(
        IUIMgr uiMgr,
        IInputMgr input,
        IUIElementLocateMgr locator)
    {
        _uiMgr = uiMgr;
        _input = input;
        _locator = locator;
    }

    public async Task<bool> UseItem(BagType bagType, int bagIndex, int slotIndex, int useCount, CancellationToken ct = default)
    {
        var showUISuc = await _uiMgr.ShowUI("道具行囊", ct);
        if (!showUISuc)
            return false;

        var chooseItemBagTypeSuc = await ChooseBagTypeTab(bagType, ct);
        if (!chooseItemBagTypeSuc)
            return false;

        var region = _locator.LocateRegion($"BagSlot.{slotIndex}.Icon");
        if (region == null)
            return false;

        for (int i = 0; i < useCount; i++)
        {
            await _input.ClickAsync(region.Value.Center, MouseButton.Right, ct);
            await Task.Delay(500, ct);
        }

        await _uiMgr.CloseUI("道具行囊", ct);

        await Task.Delay(300, ct);
        return true;
    }

    public async Task<bool> ChooseBagTypeTab(BagType bagType, CancellationToken ct = default)
    {
        var key = bagType == BagType.道具 ? "BagTab.Item" : "BagTab.Package";
        var itemBagBtnRegion = _locator.LocateRegion(key);
        if (itemBagBtnRegion == null)
            return bagType == BagType.道具;

        await _input.ClickAsync(itemBagBtnRegion.Value.Center, MouseButton.Left, ct);
        await Task.Delay(1000, ct);
        return true;
    }
}
