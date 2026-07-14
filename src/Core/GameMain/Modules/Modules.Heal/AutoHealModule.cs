using MengYou.Abstractions;
using MengYou.Abstractions.Events;
using MengYou.Abstractions.Modules;
using Microsoft.Extensions.Logging;

namespace MengYou.Modules.Heal;

/// <summary>
/// 自动加血模块：轮询玩家状态，低于阈值时按快捷键使用血药/蓝药。
/// </summary>
public sealed class AutoHealModule : IHealModule
{
    /// <summary>数据读取。</summary>
    private readonly IGameReader _reader;

    /// <summary>操作发送。</summary>
    private readonly IGameController _controller;

    /// <summary>事件总线。</summary>
    private readonly ISessionEventBus _events;

    /// <summary>日志。</summary>
    private readonly ILogger<AutoHealModule> _logger;

    /// <summary>后台轮询取消源。</summary>
    private CancellationTokenSource? _cts;

    /// <summary>后台轮询任务。</summary>
    private Task? _loop;

    /// <summary>HP 触发阈值百分比。</summary>
    private double _selfHpThreshold = 0.5;

    /// <summary>MP 触发阈值百分比。</summary>
    private double _selfMpThreshold = 0.3;

    /// <summary>血药快捷键虚拟键码：默认 F1。</summary>
    private int _hpItemKey = 0x70;

    /// <summary>蓝药快捷键虚拟键码：默认 F2。</summary>
    private int _mpItemKey = 0x71;

    /// <summary>血药最短冷却毫秒。</summary>
    private const int HpCooldownMs = 1500;

    /// <summary>蓝药最短冷却毫秒。</summary>
    private const int MpCooldownMs = 1500;

    /// <summary>轮询间隔毫秒。</summary>
    private const int PollMs = 500;

    /// <summary>上次血药触发时间。</summary>
    private DateTime _lastHpAt = DateTime.MinValue;

    /// <summary>上次蓝药触发时间。</summary>
    private DateTime _lastMpAt = DateTime.MinValue;

    /// <summary>构造。</summary>
    public AutoHealModule(IGameReader reader, IGameController controller, ISessionEventBus events, ILogger<AutoHealModule> logger)
    {
        _reader = reader;
        _controller = controller;
        _events = events;
        _logger = logger;
    }

    /// <inheritdoc/>
    public string Name => "AutoHeal";

    /// <inheritdoc/>
    public bool IsRunning => _loop is { IsCompleted: false };

    /// <inheritdoc/>
    public void Configure(double selfHpThreshold, double selfMpThreshold)
    {
        _selfHpThreshold = selfHpThreshold;
        _selfMpThreshold = selfMpThreshold;
    }

    /// <summary>配置快捷键（默认 F1=血药 / F2=蓝药）。</summary>
    public void ConfigureKeys(int hpItemKey, int mpItemKey)
    {
        _hpItemKey = hpItemKey;
        _mpItemKey = mpItemKey;
    }

    /// <inheritdoc/>
    public Task StartAsync(CancellationToken ct = default)
    {
        if (IsRunning) return Task.CompletedTask;
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _loop = Task.Run(() => LoopAsync(_cts.Token));
        _logger.LogInformation("AutoHeal 已启动 HP<{Hp:P0} MP<{Mp:P0}", _selfHpThreshold, _selfMpThreshold);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task StopAsync(CancellationToken ct = default)
    {
        _cts?.Cancel();
        if (_loop != null) try { await _loop; } catch { }
        _logger.LogInformation("AutoHeal 已停止");
    }

    /// <summary>轮询主体。</summary>
    private async Task LoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var s = _reader.GetPlayerState();
                if (!s.IsAlive) { await Task.Delay(PollMs, ct); continue; }

                var now = DateTime.UtcNow;
                // 血量低 → 血药
                if (s.HpPercent < _selfHpThreshold && (now - _lastHpAt).TotalMilliseconds > HpCooldownMs)
                {
                    _logger.LogInformation("HP {Pct:P0}<{Th:P0}，使用血药", s.HpPercent, _selfHpThreshold);
                    await _controller.SendKeyAsync(_hpItemKey, ct: ct);
                    _lastHpAt = now;
                    _events.Publish(new HpChangedEvent { OldHp = s.Hp, NewHp = s.Hp, MaxHp = s.MaxHp });
                }
                // 蓝量低 → 蓝药
                if (s.MpPercent < _selfMpThreshold && (now - _lastMpAt).TotalMilliseconds > MpCooldownMs)
                {
                    _logger.LogInformation("MP {Pct:P0}<{Th:P0}，使用蓝药", s.MpPercent, _selfMpThreshold);
                    await _controller.SendKeyAsync(_mpItemKey, ct: ct);
                    _lastMpAt = now;
                }
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "AutoHeal 轮询异常");
            }
            try { await Task.Delay(PollMs, ct); } catch (OperationCanceledException) { break; }
        }
    }
}
