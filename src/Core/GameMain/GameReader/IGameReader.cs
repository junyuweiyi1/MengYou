using MengYou.Abstractions.Models;

namespace MengYou.Abstractions;

/// <summary>
/// 游戏状态读取抽象：屏蔽图像识别 / 内存读取两种实现。
/// 所有业务模块只依赖此接口，永远不感知底层数据源。
/// </summary>
public interface IGameReader
{
    /// <summary>读取玩家状态。</summary>
    PlayerState GetPlayerState();

    /// <summary>读取队友列表（含玩家自己）。</summary>
    IReadOnlyList<Unit> GetTeamMembers();

    /// <summary>读取战场敌人。</summary>
    IReadOnlyList<Unit> GetEnemies();

    /// <summary>读取当前地图信息。</summary>
    MapInfo GetCurrentMap();

    /// <summary>识别当前场景类型。</summary>
    SceneType GetSceneType();

    /// <summary>读取正在弹出的对话框；无对话返回 null。</summary>
    DialogInfo? GetActiveDialog();

    /// <summary>读取背包状态。</summary>
    BagState GetBagState();
}
