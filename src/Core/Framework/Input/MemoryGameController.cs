using MengYou.Abstractions;
using MengYou.Abstractions.Models;

namespace MengYou.Input.Memory;

/// <summary>
/// 内存操作 Controller 预留骨架：未来通过写内存/调用 CALL 实现。
/// </summary>
public sealed class MemoryGameController : IGameController
{
    /// <summary>目标进程 ID。</summary>
    private readonly int _processId;

    /// <summary>构造。</summary>
    public MemoryGameController(int processId)
    {
        _processId = processId;
    }

    /// <inheritdoc/>
    public InputMode Mode => InputMode.Background;

    /// <inheritdoc/>
    public Task ClickAsync(Point2D point, MouseButton button = MouseButton.Left, CancellationToken ct = default)
        => throw new NotImplementedException("待实现：内存 CALL。");

    /// <inheritdoc/>
    public Task MoveAsync(Point2D point, CancellationToken ct = default)
        => throw new NotImplementedException();

    /// <inheritdoc/>
    public Task DragAsync(Point2D from, Point2D to, CancellationToken ct = default)
        => throw new NotImplementedException();

    /// <inheritdoc/>
    public Task SendKeyAsync(int virtualKeyCode, KeyModifiers modifiers = KeyModifiers.None, CancellationToken ct = default)
        => throw new NotImplementedException();

    /// <inheritdoc/>
    public Task InputTextAsync(string text, CancellationToken ct = default)
        => throw new NotImplementedException();
}
