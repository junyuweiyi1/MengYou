using MengYou.Abstractions;
using MengYou.Abstractions.Models;
using MengYou.Modules.Path.Interaction;
using MengYou.Modules.Path.World;
using Microsoft.Extensions.Logging;

namespace MengYou.Modules.Path.Teleport;

/// <summary>
/// 飞行符提供者：打开背包 → 双击飞行符 → 弹窗选择目标 → 确认。
/// </summary>
public sealed class FlyingCharmProvider : ITeleportProvider
{
    /// <summary>Reader。</summary>
    private readonly IGameReader _reader;

    /// <summary>Controller。</summary>
    private readonly IGameController _controller;

    /// <summary>UI 布局。</summary>
    private readonly IUIElementLocator _locator;

    /// <summary>视觉服务：查找飞行符图标与目标地图选项。</summary>
    private readonly IVisionService _vision;

    /// <summary>日志。</summary>
    private readonly ILogger<FlyingCharmProvider> _logger;

    /// <summary>操作间隔毫秒。</summary>
    private const int StepDelayMs = 500;

    /// <summary>构造。</summary>
    public FlyingCharmProvider(IGameReader reader, IGameController controller, IUIElementLocator locator, IVisionService vision, ILogger<FlyingCharmProvider> logger)
    {
        _reader = reader;
        _controller = controller;
        _locator = locator;
        _vision = vision;
        _logger = logger;
    }

    /// <inheritdoc/>
    public bool Supports(ConnectionType type) => type == ConnectionType.FlyCharm;

    /// <inheritdoc/>
    public async Task<bool> TeleportAsync(MapConnection connection, CancellationToken ct = default)
    {
        _logger.LogInformation("使用飞行符前往 {Target}", connection.To);

        // 1) 打开背包
        var bagBtn = _locator.Locate("BagButton");
        if (bagBtn == null) { _logger.LogError("未配置 BagButton"); return false; }
        await _controller.ClickAsync(bagBtn.Value, MouseButton.Left, ct);
        await Task.Delay(StepDelayMs, ct);

        // 2) 查找飞行符图标
        _vision.Refresh();
        var charm = _vision.FindTemplate("Item.飞行符");
        if (charm == null) { _logger.LogError("背包中未找到飞行符"); return false; }

        // 3) 双击使用（右键）
        await _controller.ClickAsync(charm.Value, MouseButton.Right, ct);
        await Task.Delay(StepDelayMs, ct);

        // 4) 在弹窗中选择目标地图（模板名约定 "FlyDest.<地图名>"）
        _vision.Refresh();
        var dest = _vision.FindTemplate($"FlyDest.{connection.To}");
        if (dest == null) { _logger.LogError("飞行符弹窗未找到目标 {To}", connection.To); return false; }
        await _controller.ClickAsync(dest.Value, MouseButton.Left, ct);
        await Task.Delay(StepDelayMs, ct);

        // 5) 关闭背包
        await _controller.ClickAsync(bagBtn.Value, MouseButton.Left, ct);
        return true;
    }
}
