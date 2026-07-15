/// <summary>
/// 游戏状态读取抽象：图像识别 / 内存读取两种实现。
/// 所有业务模块只依赖此接口，永远不感知底层数据源。
/// </summary>
public interface IGameReader
{
    /// <summary>读取玩家状态快照。</summary>
    Task<UserSnapshot> GetUserSnapshot();

    /// <summary>读取背包快照。</summary>
    Task<BagSnapshot> GetBagSnapshot();
}
