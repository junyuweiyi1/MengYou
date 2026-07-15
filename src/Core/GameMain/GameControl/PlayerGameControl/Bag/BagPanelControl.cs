using iFramework;

/// <summary>
/// 背包使用器
/// </summary>
public sealed class BagPanelControl
{
    public async Task<bool> UseItem(BagType bagType, int bagIndex, int slotIndex, int useCount)
    {
        if (bagType == BagType.道具)
        {
            bool chooseItemBagType = await ChooseBagTypeTab(BagType.bagType);
            if (!chooseItemBagType)
                return false;

            for (int i = 0; i < useCount; i++)
            {
                var region = _locator.LocateRegion($"背包.道具背包.道具格子{slotIndex}.图标");
                await _game.InputMgr.ClickAsync(region.Value.Center, MouseButton.Right);
                await Task.Delay(500, ct);
            }

            await Task.Delay(1000, ct);
        }
    }

    public async Task<bool> ChooseBagTypeTab(BagType bagType)
    {
        var itemBagBtnRegion = _locator.LocateRegion($"背包.背包种类.道具");
        await _game.InputMgr.ClickAsync(itemBagBtnRegion.Value.Center, MouseButton.Left);
        await Task.Delay(1000, ct);
        return true;
    }
}
