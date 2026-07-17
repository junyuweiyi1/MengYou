using iFramework;

/// <summary>
/// 正常玩家 游戏操作
/// </summary>
public class PlayerGameControl: IGameControl
{
    private readonly Game _game;
    private readonly BagPanelControl _bag;
    private readonly BagPanelShowUIControl _bagShowUI;

    public PlayerGameControl(Game game)
    {
        _game = game;
        _bag = new BagPanelControl(game);
        _bagShowUI = new BagPanelShowUIControl(game.InputMgr);
    }

    /// <summary>使用背包道具。</summary>
    public async Task<bool> UseBagItem(BagType bagType, int bagIndex, int slotIndex, int useCount, CancellationToken ct = default)
    {
        return await _bag.UseItem(bagType, bagIndex, slotIndex, useCount, ct);
    }

    /// <summary>
    /// 显示指定 UI。暂时只支持通过快捷键（组合键）打开，具体快捷键由各面板自身的 ShowUI 类定义。
    /// </summary>
    public async Task ShowUI(string uiName, CancellationToken ct = default)
    {
        switch (uiName)
        {
            case "道具行囊":
                await _bagShowUI.ShowUI(ct);
                break;
            default:
                break;
        }
    }

    /// <summary>
    /// 关闭指定 UI。
    /// </summary>
    public async Task CloseUI(string uiName, CancellationToken ct = default)
    {
        switch (uiName)
        {
            case "道具行囊":
                await _bagShowUI.CloseUI(ct);
                break;
            default:
                break;
        }
    }
}
