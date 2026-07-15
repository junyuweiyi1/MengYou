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
    public UserSnapshot GetUserSnapshot() => throw new NotImplementedException("待实现：内存偏移读取。");

    /// <inheritdoc/>
    public BagSnapshot GetBagSnapshot() => throw new NotImplementedException();
}
