using iFramework;
using MengYou.UI;

/// <summary>
/// 基于图像识别的 GameReader 实现。
/// 通过 IVisionService + IUIElementLocator 读取所有游戏状态。
/// 战场/背包识别委托给 TeamPanelReader / EnemyPanelReader / BagPanelReader。
///
/// UI 打开检测采用策略模式（见 UIDetectors 目录）：
/// 每种 UI 的打开方式不一（文字/图标/颜色/组合条件），
/// 各自实现 IUIStateDetector 并在构造函数中以 UI 名称注册到 _uiDetectors，
/// 新增 UI 检测无需改动 IsUIShown 本身。
/// </summary>
public sealed class ImageGameReader : IGameReader
{
    /// <summary>视觉服务。</summary>
    private readonly IVisionServiceMgr _vision;

    /// <summary>UI 定位器。</summary>
    private readonly IUIElementLocateMgr _locator;

    /// <summary>背包读取。</summary>
    private readonly BagPanelReader _bag;

    private readonly IUiRegistry _uiRegistry;

    public ImageGameReader(
        IVisionServiceMgr vision,
        IUIElementLocateMgr locator,
        IUiRegistry uiRegistry)
    {
        _vision = vision;
        _locator = locator;
        _uiRegistry = uiRegistry;

        _bag = new BagPanelReader(_vision, _locator);
    }

    public async Task<bool> IsUIShown(string uiName, CancellationToken ct = default)
    {
        var observation = await _uiRegistry
            .ObserveAsync(GameUiIds.FromName(uiName), ct)
            .ConfigureAwait(false);
        return observation.Visibility == UiVisibility.Visible;
    }

    /// <inheritdoc/>
    public Task<UserStateSnapshot> GetUserSnapshot(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        _vision.Refresh();
        var hp = _locator.LocateRegion("HpNumber") is { } hr ? _vision.ReadNumber(hr) ?? 0 : 0;
        var maxHp = _locator.LocateRegion("MaxHpNumber") is { } mhr ? _vision.ReadNumber(mhr) ?? 0 : 0;
        var mp = _locator.LocateRegion("MpNumber") is { } mr ? _vision.ReadNumber(mr) ?? 0 : 0;
        var maxMp = _locator.LocateRegion("MaxMpNumber") is { } mmr ? _vision.ReadNumber(mmr) ?? 0 : 0;
        var lv = _locator.LocateRegion("Level") is { } lr ? _vision.ReadNumber(lr) ?? 0 : 0;
        return Task.FromResult(new UserStateSnapshot
        {
            Hp = hp,
            MaxHp = maxHp,
            Mp = mp,
            MaxMp = maxMp,
            Level = lv,
        });
    }

    /// <inheritdoc/>
    public Task<BagSnapshot> GetBagSnapshot(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        _vision.Refresh();
        return Task.FromResult(_bag.ReadAll());
    }
}
