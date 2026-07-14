using MengYou.Abstractions;
using MengYou.Modules.Path.Interaction;
using MengYou.Modules.Path.World;
using Microsoft.Extensions.Logging;

namespace MengYou.Modules.Path.Teleport;

/// <summary>
/// NPC 传送提供者：走到 NPC 附近 → 对话 → 选择目的地。
/// 依赖 NpcInteractor 复用对话推进逻辑。
/// </summary>
public sealed class NpcTeleportProvider : ITeleportProvider
{
    /// <summary>NPC 交互器。</summary>
    private readonly NpcInteractor _npc;

    /// <summary>日志。</summary>
    private readonly ILogger<NpcTeleportProvider> _logger;

    /// <summary>构造。</summary>
    public NpcTeleportProvider(NpcInteractor npc, ILogger<NpcTeleportProvider> logger)
    {
        _npc = npc;
        _logger = logger;
    }

    /// <inheritdoc/>
    public bool Supports(ConnectionType type) => type == ConnectionType.TeleportNpc;

    /// <inheritdoc/>
    public async Task<bool> TeleportAsync(MapConnection connection, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(connection.Npc))
        {
            _logger.LogError("MapConnection.Npc 未配置，无法使用 NPC 传送");
            return false;
        }

        // 1) 点击 NPC 开启对话（模板名约定 "NPC.<NPC名>"）
        var ok = await _npc.ClickNpcAsync($"NPC.{connection.Npc}", ct);
        if (!ok) return false;

        // 2) 推进对话：默认选项序列 = [下一页 × N, 目标地图选项]
        //    这里只演示一步：直接点击目标地图选项模板
        var options = new List<string> { $"NpcTeleport.{connection.To}" };
        return await _npc.AdvanceDialogAsync(options, ct);
    }
}
