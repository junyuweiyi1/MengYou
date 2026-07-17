using MengYou.Runtime;

namespace iFramework;

/// <summary>
/// 面向外部模块的 UI 门面。所有会改变游戏状态的 UI 操作都进入会话动作队列；
/// 只读检测可以直接执行。
/// </summary>
public sealed class QueuedUIMgr : IUIMgr
{
    private readonly IUIMgr _inner;
    private readonly IGameActionExecutor _executor;
    private readonly GameActionResources _resources;

    public QueuedUIMgr(
        IUIMgr inner,
        IGameActionExecutor executor,
        GameActionResources resources)
    {
        _inner = inner;
        _executor = executor;
        _resources = resources;
    }

    public void SetProvider(IUIMgrProvider provider) => _inner.SetProvider(provider);

    public Task<bool> IsUIShown(string uiName, CancellationToken ct = default)
        => _inner.IsUIShown(uiName, ct);

    public async Task<bool> ShowUI(string uiName, CancellationToken ct = default)
    {
        var actionName = $"ui.show:{uiName}";
        var result = await _executor.ExecuteAsync(
            new DelegateGameAction(actionName, _resources, async actionCt =>
            {
                var shown = await _inner.ShowUI(uiName, actionCt).ConfigureAwait(false);
                return shown
                    ? GameActionResult.Success(actionName)
                    : GameActionResult.VerificationFailed(actionName, $"未能确认 UI 已打开：{uiName}");
            }),
            ct).ConfigureAwait(false);

        return result.IsSuccess;
    }

    public async Task<bool> CloseUI(string uiName, CancellationToken ct = default)
    {
        var actionName = $"ui.close:{uiName}";
        var result = await _executor.ExecuteAsync(
            new DelegateGameAction(actionName, _resources, async actionCt =>
            {
                var closed = await _inner.CloseUI(uiName, actionCt).ConfigureAwait(false);
                return closed
                    ? GameActionResult.Success(actionName)
                    : GameActionResult.VerificationFailed(actionName, $"未能确认 UI 已关闭：{uiName}");
            }),
            ct).ConfigureAwait(false);

        return result.IsSuccess;
    }

    public void Dispose() => _inner.Dispose();
}
