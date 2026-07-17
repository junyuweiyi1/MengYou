namespace MengYou.Runtime;

/// <summary>可由自动恢复功能处理的角色资源。</summary>
public enum RecoveryResource
{
    Health,
    Mana,
}

/// <summary>
/// 执行一次资源恢复。实现负责定位恢复物品并把实际输入操作送入动作队列。
/// </summary>
public interface IRecoveryService
{
    Task<GameActionResult> RecoverAsync(
        RecoveryResource resource,
        string itemName,
        int useCount,
        CancellationToken ct = default);
}
