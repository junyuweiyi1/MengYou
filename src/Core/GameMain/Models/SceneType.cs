namespace MengYou.Abstractions.Models;

/// <summary>
/// 当前场景类型：驱动业务模块的决策。
/// </summary>
public enum SceneType
{
    /// <summary>未知。</summary>
    Unknown = 0,

    /// <summary>登录/角色选择等准备阶段。</summary>
    Login,

    /// <summary>普通场景（城镇/野外）。</summary>
    World,

    /// <summary>NPC 对话中。</summary>
    Dialog,

    /// <summary>战斗中。</summary>
    Combat,

    /// <summary>切换地图加载中。</summary>
    Loading,

    /// <summary>死亡等待复活。</summary>
    Dead,
}
