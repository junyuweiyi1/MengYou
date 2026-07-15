using iFramework;

/// <summary>
/// 基于图像识别的 GameReader 实现。
/// 通过 IVisionService + IUIElementLocator 读取所有游戏状态。
/// 战场/背包识别委托给 TeamPanelReader / EnemyPanelReader / BagPanelReader。
/// </summary>
public sealed class ImageGameReader : IGameReader
{
    /// <summary>视觉服务。</summary>
    private readonly IVisionService _vision;

    /// <summary>UI 定位器。</summary>
    private readonly IUIElementLocateMgr _locator;

    /// <summary>背包读取。</summary>
    private readonly BagPanelReader _bag;

    /// <summary>构造。</summary>
    public ImageGameReader(
        IVisionService vision,
        IUIElementLocator locator,
        BagPanelReader bag)
    {
        _vision = vision;
        _locator = locator;
        _bag = bag;
    }

    /// <inheritdoc/>
    public async Task<UserSnapshot> GetUserSnapshot()
    {
        _vision.Refresh();
        var hp = _locator.LocateRegion("HpNumber") is { } hr ? _vision.ReadNumber(hr) ?? 0 : 0;
        var maxHp = _locator.LocateRegion("MaxHpNumber") is { } mhr ? _vision.ReadNumber(mhr) ?? 0 : 0;
        var mp = _locator.LocateRegion("MpNumber") is { } mr ? _vision.ReadNumber(mr) ?? 0 : 0;
        var maxMp = _locator.LocateRegion("MaxMpNumber") is { } mmr ? _vision.ReadNumber(mmr) ?? 0 : 0;
        var lv = _locator.LocateRegion("Level") is { } lr ? _vision.ReadNumber(lr) ?? 0 : 0;
        return new UserSnapshot
        {
            Hp = hp,
            MaxHp = maxHp,
            Mp = mp,
            MaxMp = maxMp,
            Level = lv,
        };
    }

    /// <inheritdoc/>
    public async Task<BagSnapshot> GetBagSnapshot()
    {
        _vision.Refresh();
        return _bag.ReadAll();
    }
}
