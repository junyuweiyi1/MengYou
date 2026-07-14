using MengYou.Abstractions;
using MengYou.Modules.Path.World;
using Microsoft.Extensions.Logging;

namespace MengYou.Modules.Path.Teleport;

/// <summary>
/// 飞行旗提供者：与飞行符类似，物品名不同；目标地图有限制。
/// </summary>
public sealed class FlyingFlagProvider : ITeleportProvider
{
    /// <summary>Controller。</summary>
    private readonly IGameController _controller;

    /// <summary>UI 布局。</summary>
    private readonly IUIElementLocator _locator;

    /// <summary>视觉服务。</summary>
    private readonly IVisionService _vision;

    /// <summary>日志。</summary>
    private readonly ILogger<FlyingFlagProvider> _logger;

    /// <summary>操作间隔。</summary>
    private const int StepDelayMs = 500;

    /// <summary>构造。</summary>
    public FlyingFlagProvider(IGameController controller, IUIElementLocator locator, IVisionService vision, ILogger<FlyingFlagProvider> logger)
    {
        _controller = controller;
        _locator = locator;
        _vision = vision;
        _logger = logger;
    }

    /// <inheritdoc/>
    public bool Supports(ConnectionType type) => type == ConnectionType.FlyFlag;

    /// <inheritdoc/>
    public async Task<bool> TeleportAsync(MapConnection connection, CancellationToken ct = default)
    {
        _logger.LogInformation("使用飞行旗前往 {Target}", connection.To);
        var bagBtn = _locator.Locate("BagButton");
        if (bagBtn == null) { _logger.LogError("未配置 BagButton"); return false; }

        // 打开背包
        await _controller.ClickAsync(bagBtn.Value, MouseButton.Left, ct);
        await Task.Delay(StepDelayMs, ct);

        // 查找飞行旗
        _vision.Refresh();
        var flag = _vision.FindTemplate("Item.飞行旗");
        if (flag == null) { _logger.LogError("背包中未找到飞行旗"); return false; }
        await _controller.ClickAsync(flag.Value, MouseButton.Right, ct);
        await Task.Delay(StepDelayMs, ct);

        // 选择目标
        _vision.Refresh();
        var dest = _vision.FindTemplate($"FlyDest.{connection.To}");
        if (dest == null) { _logger.LogError("飞行旗弹窗未找到目标 {To}", connection.To); return false; }
        await _controller.ClickAsync(dest.Value, MouseButton.Left, ct);
        await Task.Delay(StepDelayMs, ct);

        await _controller.ClickAsync(bagBtn.Value, MouseButton.Left, ct);
        return true;
    }
}
