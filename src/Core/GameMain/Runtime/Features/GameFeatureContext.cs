namespace MengYou.Runtime;

/// <summary>功能运行上下文，只暴露状态读取和受控动作执行入口。</summary>
public sealed class GameFeatureContext
{
    public GameFeatureContext(
        Guid sessionId,
        string displayName,
        IGameStateStore stateStore,
        IGameActionExecutor actions)
    {
        SessionId = sessionId;
        DisplayName = displayName;
        StateStore = stateStore;
        Actions = actions;
    }

    public Guid SessionId { get; }

    public string DisplayName { get; }

    public IGameStateStore StateStore { get; }

    public IGameActionExecutor Actions { get; }

    public Task<GameActionResult> ExecuteAsync(
        IGameAction action,
        CancellationToken ct = default)
        => Actions.ExecuteAsync(action, ct);

    /// <summary>等待满足条件的新状态；会先检查当前快照。</summary>
    public async Task<GameStateSnapshot> WaitForStateAsync(
        Func<GameStateSnapshot, bool> predicate,
        TimeSpan timeout,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        if (timeout <= TimeSpan.Zero && timeout != Timeout.InfiniteTimeSpan)
            throw new ArgumentOutOfRangeException(nameof(timeout));

        var current = StateStore.Current;
        if (predicate(current)) return current;

        using var timeoutCts = new CancellationTokenSource();
        if (timeout != Timeout.InfiniteTimeSpan)
            timeoutCts.CancelAfter(timeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            ct,
            timeoutCts.Token);

        try
        {
            await foreach (var snapshot in StateStore
                .WatchAsync(current.Version, linkedCts.Token)
                .ConfigureAwait(false))
            {
                if (predicate(snapshot)) return snapshot;
            }
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !ct.IsCancellationRequested)
        {
            throw new TimeoutException($"等待游戏状态超时：{timeout}。");
        }

        throw new OperationCanceledException(linkedCts.Token);
    }
}
