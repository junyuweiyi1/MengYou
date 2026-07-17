using iFramework;
using MengYou.UI;

/// <summary>
/// 正常玩家 游戏操作
/// </summary>
public class PlayerGameControl: IGameControl
{
    private readonly BagPanelControl _bag;
    private readonly IUiRegistry _uiRegistry;

    public PlayerGameControl(
        IUIMgr uiMgr,
        IInputMgr input,
        IUIElementLocateMgr locator,
        IUiRegistry uiRegistry)
    {
        _bag = new BagPanelControl(uiMgr, input, locator);
        _uiRegistry = uiRegistry;
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
        await _uiRegistry
            .OpenAsync(GameUiIds.FromName(uiName), ct)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// 关闭指定 UI。
    /// </summary>
    public async Task CloseUI(string uiName, CancellationToken ct = default)
    {
        await _uiRegistry
            .CloseAsync(GameUiIds.FromName(uiName), ct)
            .ConfigureAwait(false);
    }
}
