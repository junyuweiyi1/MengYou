using iFramework;


/// <summary>
/// 人类游戏操作
/// </summary>
public class HumanGameControl: IGameControl
{
    private Game _game;

    /// <summary>使用背包道具。</summary>
    public async Task<BagSnapshot> UseBagItem(string itemName, int bagIndex, int slotIndex, int useCount)
    {
        // 1.检查背包是否打开

        // 2.鼠标挪到对应道具格

        // 3.检查这格道具名是否是itemName

        // 4.右键点击useCount次
        _game.InputMgr.

                    var region = _locator.LocateRegion($"BagSlot.{slotIndex}.Icon");
        if (region == null) return false;

        await _controller.ClickAsync(region.Value.Center, MouseButton.Right, ct);
        return true;
    }
}
