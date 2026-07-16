/// <summary>
/// 游戏操作抽象：图像识别 / 内存读取两种实现。
/// 所有业务模块只依赖此接口，永远不感知底层数据源。
/// </summary>
public interface IGameControl
{
    /// <summary>使用背包道具。</summary>
    Task<bool> UseBagItem(BagType bagType, int bagIndex, int slotIndex, int useCount, CancellationToken ct = default);

    Task ShowUI(string uiName, CancellationToken ct = default);

    Task CloseUI(string uiName, CancellationToken ct = default);
}
