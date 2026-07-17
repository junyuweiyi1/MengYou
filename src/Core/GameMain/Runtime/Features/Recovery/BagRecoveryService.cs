using MengYou.UI;

namespace MengYou.Runtime;

/// <summary>通过现有背包识别与排队控制门面完成恢复。</summary>
public sealed class BagRecoveryService : IRecoveryService
{
    private readonly Game _game;

    public BagRecoveryService(Game game)
    {
        _game = game ?? throw new ArgumentNullException(nameof(game));
    }

    public async Task<GameActionResult> RecoverAsync(
        RecoveryResource resource,
        string itemName,
        int useCount,
        CancellationToken ct = default)
    {
        var actionName = $"recovery:{resource}:{itemName}:{useCount}";
        if (string.IsNullOrWhiteSpace(itemName) || useCount <= 0)
        {
            return GameActionResult.Create(
                actionName,
                GameActionStatus.NotApplicable,
                "恢复物品名为空或使用数量无效。");
        }

        try
        {
            var bagVisible = await _game.UIMgr
                .ShowUI(GameUiIds.Bag.Value, ct)
                .ConfigureAwait(false);
            if (!bagVisible)
            {
                return GameActionResult.VerificationFailed(
                    actionName,
                    "无法确认道具行囊已打开，未执行恢复。");
            }

            await _game.BagMgr.RefreshData(ct).ConfigureAwait(false);
            var used = await _game.BagMgr
                .UseItem(itemName, useCount, ct)
                .ConfigureAwait(false);
            return used
                ? GameActionResult.Success(actionName, $"已使用 {itemName}。")
                : GameActionResult.VerificationFailed(
                    actionName,
                    $"未找到或未能使用恢复物品：{itemName}。");
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return GameActionResult.Interrupted(actionName, "恢复操作已取消。");
        }
        catch (Exception ex)
        {
            return GameActionResult.Faulted(actionName, ex);
        }
    }
}
