namespace MengYou.Runtime;

/// <summary>可由会话监督器托管的长期运行功能。</summary>
public interface IGameFeature
{
    string Id { get; }

    string DisplayName { get; }

    Task RunAsync(GameFeatureContext context, CancellationToken ct = default);
}

/// <summary>功能运行状态。</summary>
public enum GameFeatureRunState
{
    Running,
    Stopping,
    Completed,
    Stopped,
    Faulted,
}

/// <summary>功能状态的不可变查询快照。</summary>
public sealed record GameFeatureStatus
{
    public required string FeatureId { get; init; }

    public required string DisplayName { get; init; }

    public required GameFeatureRunState State { get; init; }

    public required DateTimeOffset StartedAt { get; init; }

    public DateTimeOffset? FinishedAt { get; init; }

    public string? Error { get; init; }
}
