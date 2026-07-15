using iFramework;


/// <summary>
/// 正常玩家 游戏操作
/// </summary>
public class PlayerGameControl: IGameControl
{
    private Game _game;
    private BagPanelControl _bag;

    public PlayerGameControl(Game game)
    {
        _game = game;
        _bag = new BagPanelControl(game.InputMgr);
    }

    /// <summary>使用背包道具。</summary>
    public async void UseBagItem(BagType bagType, int bagIndex, int slotIndex, int useCount)
    {
        await _game.UIMgr.Show("背包");

        await _bag.UseItem(bagType, bagIndex, slotIndex, useCount);

        await _game.UIMgr.Close("背包");
    }
}