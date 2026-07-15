namespace iFramework;

/// <summary>
/// 人性化装饰器：在原始 Controller 外包一层，加入随机延迟与轻微轨迹抖动。
/// 通过组合而非继承，符合"装饰器模式"。
/// </summary>
public sealed class HumanizedInputMgr : IInputMgr
{
    /// <summary>被包裹的内部 Controller。</summary>
    private readonly IInputMgr _inner;

    /// <summary>随机数源。</summary>
    private readonly Random _rng = new();

    /// <summary>点击前最小延迟(ms)。</summary>
    private readonly int _preDelayMinMs;

    /// <summary>点击前最大延迟(ms)。</summary>
    private readonly int _preDelayMaxMs;

    /// <summary>构造。</summary>
    /// <param name="inner">内部实际执行的 Controller。</param>
    /// <param name="preDelayMinMs">操作前延迟下限（毫秒）。</param>
    /// <param name="preDelayMaxMs">操作前延迟上限（毫秒）。</param>
    public HumanizedInputMgr(IInputMgr inner, int preDelayMinMs = 30, int preDelayMaxMs = 120)
    {
        _inner = inner;
        _preDelayMinMs = preDelayMinMs;
        _preDelayMaxMs = preDelayMaxMs;
    }

    /// <inheritdoc/>
    public InputMode Mode => _inner.Mode;

    /// <inheritdoc/>
    public async Task ClickAsync(Vector2 point, MouseButton button = MouseButton.Left, CancellationToken ct = default)
    {
        await HumanDelay(ct);
        // 坐标微抖动：±2 像素
        var jittered = new Point2D(point.X + _rng.Next(-2, 3), point.Y + _rng.Next(-2, 3));
        await _inner.ClickAsync(jittered, button, ct);
    }

    /// <inheritdoc/>
    public async Task MoveAsync(Vector2 point, CancellationToken ct = default)
    {
        await HumanDelay(ct);
        await _inner.MoveAsync(point, ct);
    }

    /// <inheritdoc/>
    public async Task DragAsync(Vector2 from, Vector2 to, CancellationToken ct = default)
    {
        await HumanDelay(ct);
        await _inner.DragAsync(from, to, ct);
    }

    /// <inheritdoc/>
    public async Task SendKeyAsync(int virtualKeyCode, KeyModifiers modifiers = KeyModifiers.None, CancellationToken ct = default)
    {
        await HumanDelay(ct);
        await _inner.SendKeyAsync(virtualKeyCode, modifiers, ct);
    }

    /// <inheritdoc/>
    public async Task InputTextAsync(string text, CancellationToken ct = default)
    {
        foreach (var c in text)
        {
            await HumanDelay(ct);
            await _inner.InputTextAsync(c.ToString(), ct);
        }
    }

    /// <summary>随机延迟：模拟人类反应时间。</summary>
    private async Task HumanDelay(CancellationToken ct)
    {
        var ms = _rng.Next(_preDelayMinMs, _preDelayMaxMs);
        await Task.Delay(ms, ct);
    }
}
