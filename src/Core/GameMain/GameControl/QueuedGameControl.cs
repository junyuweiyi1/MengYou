using MengYou.Runtime;

/// <summary>
/// 面向业务模块的游戏控制门面。所有写操作进入会话动作队列，底层控制器只在队列内部调用。
/// </summary>
public sealed class QueuedGameControl : IGameControl
{
    private readonly IGameControl _inner;
    private readonly IGameActionExecutor _executor;
    private readonly GameActionResources _resources;

    public QueuedGameControl(
        IGameControl inner,
        IGameActionExecutor executor,
        GameActionResources resources)
    {
        _inner = inner;
        _executor = executor;
        _resources = resources;
    }

    public async Task<bool> UseBagItem(
        BagType bagType,
        int bagIndex,
        int slotIndex,
        int useCount,
        CancellationToken ct = default)
    {
        var actionName = $"bag.use:{bagType}:{bagIndex}:{slotIndex}:{useCount}";
        var result = await _executor.ExecuteAsync(
            new DelegateGameAction(actionName, _resources, async actionCt =>
            {
                var used = await _inner
                    .UseBagItem(bagType, bagIndex, slotIndex, useCount, actionCt)
                    .ConfigureAwait(false);
                return used
                    ? GameActionResult.Success(actionName)
                    : GameActionResult.VerificationFailed(actionName, "使用背包物品失败。");
            }),
            ct).ConfigureAwait(false);

        return result.IsSuccess;
    }

    public async Task ShowUI(string uiName, CancellationToken ct = default)
    {
        var actionName = $"control.ui.show:{uiName}";
        var result = await _executor.ExecuteAsync(
            new DelegateGameAction(actionName, _resources, async actionCt =>
            {
                await _inner.ShowUI(uiName, actionCt).ConfigureAwait(false);
                return GameActionResult.Success(actionName);
            }),
            ct).ConfigureAwait(false);

        ThrowIfFailed(result, ct);
    }

    public async Task CloseUI(string uiName, CancellationToken ct = default)
    {
        var actionName = $"control.ui.close:{uiName}";
        var result = await _executor.ExecuteAsync(
            new DelegateGameAction(actionName, _resources, async actionCt =>
            {
                await _inner.CloseUI(uiName, actionCt).ConfigureAwait(false);
                return GameActionResult.Success(actionName);
            }),
            ct).ConfigureAwait(false);

        ThrowIfFailed(result, ct);
    }

    private static void ThrowIfFailed(GameActionResult result, CancellationToken ct)
    {
        if (result.IsSuccess) return;
        if (ct.IsCancellationRequested)
            throw new OperationCanceledException(ct);
        if (result.Exception != null)
            throw new InvalidOperationException(result.Message, result.Exception);

        throw new InvalidOperationException(result.Message ?? $"动作失败：{result.ActionName}");
    }
}
