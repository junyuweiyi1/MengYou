using iFramework;

/// <summary>
/// 正常玩家 游戏操作
/// </summary>
public class PlayerGameControl : IGameControl
{
    private readonly Game _game;
    private readonly BagPanelControl _bag;
    private readonly IReadOnlyDictionary<string, KeyCode[]> _uiHotkeys;

    public PlayerGameControl(Game game, IReadOnlyDictionary<string, KeyCode[]>? uiHotkeys = null)
    {
        _game = game;
        _bag = new BagPanelControl(game);
        _uiHotkeys = uiHotkeys is null
            ? new Dictionary<string, KeyCode[]>(StringComparer.OrdinalIgnoreCase)
            {
                ["道具行囊"] = new[] { KeyCode.Alt, KeyCode.E },
                ["人物状态"] = new[] { KeyCode.Alt, KeyCode.W },
            }
            : new Dictionary<string, KeyCode[]>(uiHotkeys, StringComparer.OrdinalIgnoreCase);
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
        await ToggleUiAsync(uiName, ct);
    }

    /// <summary>
    /// 关闭指定 UI。
    /// </summary>
    public async Task CloseUI(string uiName, CancellationToken ct = default)
    {
        await ToggleUiAsync(uiName, ct);
    }

    private async Task ToggleUiAsync(string uiName, CancellationToken ct)
    {
        if (!_uiHotkeys.TryGetValue(uiName, out var keys) || keys.Length == 0)
            throw new NotSupportedException($"未配置 UI“{uiName}”的快捷键。");
        await _game.FW.InputMgr.SendKeyAsync(ct, keys);
    }
}
