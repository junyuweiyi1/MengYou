using MengYou.Abstractions;
using MengYou.Abstractions.Models;
using Microsoft.Extensions.Logging;

namespace MengYou.Modules.Path.Local;

/// <summary>
/// 地图内寻路器：把玩家从当前坐标带到目标坐标（同一张图内）。
/// MVP 方案：将地图坐标映射到小地图屏幕坐标 → 右键点击触发游戏内自动寻路。
/// </summary>
public sealed class LocalPathFinder
{
    /// <summary>Reader。</summary>
    private readonly IGameReader _reader;

    /// <summary>Controller。</summary>
    private readonly IGameController _controller;

    /// <summary>UI 定位器（小地图区域）。</summary>
    private readonly IUIElementLocator _locator;

    /// <summary>日志。</summary>
    private readonly ILogger<LocalPathFinder> _logger;

    /// <summary>梦幻西游地图坐标系宽度（用于映射到小地图；默认值可覆盖）。</summary>
    private const int MapCoordWidth = 100;

    /// <summary>梦幻西游地图坐标系高度。</summary>
    private const int MapCoordHeight = 100;

    /// <summary>到达判定阈值（曼哈顿距离）。</summary>
    private const int ArriveThreshold = 2;

    /// <summary>轮询间隔毫秒。</summary>
    private const int PollIntervalMs = 500;

    /// <summary>点击后至少走几秒才判定"卡住"。</summary>
    private const int StuckSeconds = 8;

    /// <summary>构造。</summary>
    public LocalPathFinder(IGameReader reader, IGameController controller, IUIElementLocator locator, ILogger<LocalPathFinder> logger)
    {
        _reader = reader;
        _controller = controller;
        _locator = locator;
        _logger = logger;
    }

    /// <summary>在当前地图内寻路到目标坐标。到达或超时返回。</summary>
    /// <param name="target">地图坐标目标。</param>
    /// <param name="timeout">总超时。</param>
    public async Task<bool> WalkToAsync(Point2D target, TimeSpan timeout, CancellationToken ct = default)
    {
        _logger.LogInformation("地图内寻路到 {Target}", target);
        var deadline = DateTime.UtcNow + timeout;
        Point2D lastPos = _reader.GetPlayerState().MapPosition;
        var lastMoveAt = DateTime.UtcNow;

        // 首次触发点击
        await IssueClickAsync(target, ct);

        while (DateTime.UtcNow < deadline && !ct.IsCancellationRequested)
        {
            await Task.Delay(PollIntervalMs, ct);
            var pos = _reader.GetPlayerState().MapPosition;
            // 到达
            if (Manhattan(pos, target) <= ArriveThreshold)
            {
                _logger.LogInformation("已到达 {Target}", target);
                return true;
            }
            // 位置变化则更新时间戳
            if (!pos.Equals(lastPos))
            {
                lastPos = pos;
                lastMoveAt = DateTime.UtcNow;
                continue;
            }
            // 卡住：重新点击一次
            if ((DateTime.UtcNow - lastMoveAt).TotalSeconds > StuckSeconds)
            {
                _logger.LogWarning("寻路疑似卡住，重发点击");
                await IssueClickAsync(target, ct);
                lastMoveAt = DateTime.UtcNow;
            }
        }
        _logger.LogWarning("寻路超时未到达 {Target}", target);
        return false;
    }

    /// <summary>发起一次寻路点击：将地图坐标映射到小地图屏幕坐标并点击。</summary>
    private async Task IssueClickAsync(Point2D mapTarget, CancellationToken ct)
    {
        var mini = _locator.LocateRegion("MiniMap");
        if (mini == null)
        {
            _logger.LogError("UILayout 未配置 MiniMap 区域，无法寻路");
            return;
        }
        var screen = MapToMiniMap(mapTarget, mini.Value);
        _logger.LogDebug("寻路点击：地图 {Map} → 屏幕 {Screen}", mapTarget, screen);
        // 右键点击小地图触发梦幻的自动寻路
        await _controller.ClickAsync(screen, MouseButton.Right, ct);
    }

    /// <summary>把地图坐标线性映射到小地图矩形内。</summary>
    private static Point2D MapToMiniMap(Point2D mapPoint, Rect miniMap)
    {
        var x = miniMap.X + (int)(mapPoint.X * (double)miniMap.Width / MapCoordWidth);
        var y = miniMap.Y + (int)(mapPoint.Y * (double)miniMap.Height / MapCoordHeight);
        // Clamp 防止越界
        x = Math.Clamp(x, miniMap.X, miniMap.X + miniMap.Width - 1);
        y = Math.Clamp(y, miniMap.Y, miniMap.Y + miniMap.Height - 1);
        return new Point2D(x, y);
    }

    /// <summary>曼哈顿距离。</summary>
    private static int Manhattan(Point2D a, Point2D b) => Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
}
