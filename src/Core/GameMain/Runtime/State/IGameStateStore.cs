using System.Runtime.CompilerServices;

namespace MengYou.Runtime;

/// <summary>面向功能模块的只读游戏状态存储。</summary>
public interface IGameStateStore
{
    GameStateSnapshot Current { get; }

    Task<GameStateSnapshot> WaitForChangeAsync(
        long afterVersion,
        CancellationToken ct = default);

    IAsyncEnumerable<GameStateSnapshot> WatchAsync(
        long afterVersion = -1,
        CancellationToken ct = default);
}

/// <summary>仅供感知运行时发布状态的写入端口。</summary>
public interface IGameStatePublisher
{
    GameStateSnapshot Publish(GameStateSnapshot snapshot);
}
