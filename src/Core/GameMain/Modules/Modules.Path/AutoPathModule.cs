using MengYou.Abstractions;
using MengYou.Abstractions.Models;
using MengYou.Abstractions.Modules;
using MengYou.Modules.Path.Interaction;
using MengYou.Modules.Path.Local;
using MengYou.Modules.Path.Teleport;
using MengYou.Modules.Path.World;
using Microsoft.Extensions.Logging;

namespace MengYou.Modules.Path;

/// <summary>
/// 自动寻路模块实现：跨图规划 → 传送执行 → 图内步行。
/// </summary>
public sealed class AutoPathModule : IPathModule
{
    private readonly IGameReader _reader;
    private readonly IGameController _controller;
    private readonly PathPlanner _planner;
    private readonly LocalPathFinder _local;
    private readonly NpcInteractor _npc;
    private readonly IReadOnlyList<ITeleportProvider> _teleports;
    private readonly ILogger<AutoPathModule> _logger;

    /// <summary>构造。</summary>
    public AutoPathModule(
        IGameReader reader,
        IGameController controller,
        PathPlanner planner,
        LocalPathFinder local,
        NpcInteractor npc,
        IReadOnlyList<ITeleportProvider> teleports,
        ILogger<AutoPathModule> logger)
    {
        _reader = reader;
        _controller = controller;
        _planner = planner;
        _local = local;
        _npc = npc;
        _teleports = teleports;
        _logger = logger;
    }

    /// <inheritdoc/>
    public string Name => "自动寻路";

    /// <inheritdoc/>
    public bool IsRunning { get; private set; }

    /// <inheritdoc/>
    public async Task<bool> NavigateAsync(PathTarget target, CancellationToken ct = default)
    {
        if (IsRunning) return false;
        IsRunning = true;
        try
        {
            var currentMap = _reader.GetCurrentMap();
            var steps = _planner.Plan(currentMap.MapId, target.TargetMapId);
            _logger.LogInformation("寻路规划：{From} → {To}，共 {Count} 步",
                currentMap.MapId, target.TargetMapId, steps.Count);

            foreach (var step in steps)
            {
                _logger.LogInformation("跨图：{From} → {To}（{Type}）",
                    step.FromMapId, step.ToMapId, step.Via.Type);
                var provider = _teleports.FirstOrDefault(p => p.Supports(step.Via.Type));
                if (provider == null)
                {
                    _logger.LogError("无传送提供者支持 {Type}", step.Via.Type);
                    return false;
                }
                var ok = await provider.TeleportAsync(step.Via, ct);
                if (!ok)
                {
                    _logger.LogError("传送失败：{From} → {To}", step.FromMapId, step.ToMapId);
                    return false;
                }
                // 等待地图加载
                await Task.Delay(2000, ct);
            }

            // 最后一段：在目标地图内步行到精确坐标
            var finalOk = await _local.WalkToAsync(target.TargetPosition, TimeSpan.FromSeconds(60), ct);
            _logger.LogInformation("寻路{Result}：目标 {Target}", finalOk ? "成功" : "失败", target);
            return finalOk;
        }
        finally
        {
            IsRunning = false;
        }
    }

    /// <inheritdoc/>
    public Task StartAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("寻路模块已就绪");
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("寻路模块已停止");
        return Task.CompletedTask;
    }
}
