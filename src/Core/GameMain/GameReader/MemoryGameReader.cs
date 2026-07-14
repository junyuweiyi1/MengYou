using MengYou.Abstractions;
using MengYou.Abstractions.Models;

namespace MengYou.Recognition.Memory;

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
    public PlayerState GetPlayerState() => throw new NotImplementedException("待实现：内存偏移读取。");

    /// <inheritdoc/>
    public IReadOnlyList<Unit> GetTeamMembers() => throw new NotImplementedException();

    /// <inheritdoc/>
    public IReadOnlyList<Unit> GetEnemies() => throw new NotImplementedException();

    /// <inheritdoc/>
    public MapInfo GetCurrentMap() => throw new NotImplementedException();

    /// <inheritdoc/>
    public SceneType GetSceneType() => throw new NotImplementedException();

    /// <inheritdoc/>
    public DialogInfo? GetActiveDialog() => throw new NotImplementedException();

    /// <inheritdoc/>
    public BagState GetBagState() => throw new NotImplementedException();
}
