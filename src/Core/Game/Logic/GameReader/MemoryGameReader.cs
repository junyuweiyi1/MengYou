/// <summary>
/// 内存读取 GameReader 预留骨架：未来通过 ReadProcessMemory 实现。
/// </summary>
public sealed class MemoryGameReader : IGameReader
{
    /// <summary>目标进程 ID。</summary>
    private readonly int _processId;

    /// <summary>构造。</summary>
    public MemoryGameReader(int processId)
    {
        _processId = processId;
    }

    /// <inheritdoc/>
    public Task<bool> IsUIShown(string uiName, CancellationToken ct = default) => Task.FromResult(false);

    /// <inheritdoc/>
    public Task<UserStateSnapshot> GetUserSnapshot(CancellationToken ct = default) => throw new NotImplementedException("待实现：内存偏移读取。");

    /// <inheritdoc/>
    public Task<BagSnapshot> GetBagSnapshot(CancellationToken ct = default) => throw new NotImplementedException();
}
