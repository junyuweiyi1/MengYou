namespace MengYou.Runtime;

/// <summary>生成单次游戏状态快照的感知端口。</summary>
public interface IGameStateObserver
{
    Task<GameStateSnapshot> ObserveAsync(CancellationToken ct = default);
}

/// <summary>便于组合和测试的委托型状态观察器。</summary>
public sealed class DelegateGameStateObserver : IGameStateObserver
{
    private readonly Func<CancellationToken, Task<GameStateSnapshot>> _observe;

    public DelegateGameStateObserver(
        Func<CancellationToken, Task<GameStateSnapshot>> observe)
    {
        _observe = observe ?? throw new ArgumentNullException(nameof(observe));
    }

    public Task<GameStateSnapshot> ObserveAsync(CancellationToken ct = default)
        => _observe(ct);
}
