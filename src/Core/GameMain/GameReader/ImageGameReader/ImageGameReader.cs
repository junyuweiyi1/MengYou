using MengYou.Abstractions;
using MengYou.Abstractions.Models;
using MengYou.Recognition.Image.Bag;
using MengYou.Recognition.Image.Combat;

namespace MengYou.Recognition.Image;

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
    private readonly IUIElementLocator _locator;

    /// <summary>队伍面板读取。</summary>
    private readonly TeamPanelReader _team;

    /// <summary>敌方面板读取。</summary>
    private readonly EnemyPanelReader _enemy;

    /// <summary>背包读取。</summary>
    private readonly BagPanelReader _bag;

    /// <summary>构造。</summary>
    public ImageGameReader(
        IVisionService vision,
        IUIElementLocator locator,
        TeamPanelReader team,
        EnemyPanelReader enemy,
        BagPanelReader bag)
    {
        _vision = vision;
        _locator = locator;
        _team = team;
        _enemy = enemy;
        _bag = bag;
    }

    /// <inheritdoc/>
    public PlayerState GetPlayerState()
    {
        _vision.Refresh();
        var hp = _locator.LocateRegion("HpNumber") is { } hr ? _vision.ReadNumber(hr) ?? 0 : 0;
        var maxHp = _locator.LocateRegion("MaxHpNumber") is { } mhr ? _vision.ReadNumber(mhr) ?? 0 : 0;
        var mp = _locator.LocateRegion("MpNumber") is { } mr ? _vision.ReadNumber(mr) ?? 0 : 0;
        var maxMp = _locator.LocateRegion("MaxMpNumber") is { } mmr ? _vision.ReadNumber(mmr) ?? 0 : 0;
        var lvl = _locator.LocateRegion("Level") is { } lr ? _vision.ReadNumber(lr) ?? 0 : 0;
        var map = GetCurrentMap();
        return new PlayerState
        {
            Hp = hp,
            MaxHp = maxHp,
            Mp = mp,
            MaxMp = maxMp,
            Level = lvl,
            MapName = map.DisplayName,
            MapPosition = map.Position,
        };
    }

    /// <inheritdoc/>
    public IReadOnlyList<Unit> GetTeamMembers()
    {
        _vision.Refresh();
        return _team.ReadAll();
    }

    /// <inheritdoc/>
    public IReadOnlyList<Unit> GetEnemies()
    {
        // 仅战斗场景返回敌人；其他场景返回空
        if (GetSceneType() != SceneType.Combat) return Array.Empty<Unit>();
        _vision.Refresh();
        return _enemy.ReadAll();
    }

    /// <inheritdoc/>
    public MapInfo GetCurrentMap()
    {
        var name = _locator.LocateRegion("MapName") is { } nr ? _vision.ReadText(nr) ?? string.Empty : string.Empty;
        var x = _locator.LocateRegion("CoordX") is { } xr ? _vision.ReadNumber(xr) ?? 0 : 0;
        var y = _locator.LocateRegion("CoordY") is { } yr ? _vision.ReadNumber(yr) ?? 0 : 0;
        return new MapInfo { MapId = name, DisplayName = name, Position = new Point2D(x, y) };
    }

    /// <inheritdoc/>
    public SceneType GetSceneType()
    {
        // 用几张标志性模板判断当前场景
        if (_vision.FindTemplate("Scene.CombatMark") != null) return SceneType.Combat;
        if (_vision.FindTemplate("Scene.DialogMark") != null) return SceneType.Dialog;
        if (_vision.FindTemplate("Scene.LoadingMark") != null) return SceneType.Loading;
        if (_vision.FindTemplate("Scene.DeadMark") != null) return SceneType.Dead;
        return SceneType.World;
    }

    /// <inheritdoc/>
    public DialogInfo? GetActiveDialog()
    {
        if (GetSceneType() != SceneType.Dialog) return null;
        var content = _locator.LocateRegion("DialogContent") is { } cr ? _vision.ReadText(cr) : null;
        return new DialogInfo { Content = content ?? string.Empty };
    }

    /// <inheritdoc/>
    public BagState GetBagState()
    {
        _vision.Refresh();
        return _bag.ReadAll();
    }
}
