using System.Collections.ObjectModel;
using MengYou.UI;

namespace MengYou.Runtime;

/// <summary>会话感知链路的健康状态。</summary>
public enum ObservationHealth
{
    Starting,
    Healthy,
    Degraded,
    Stopped,
}

/// <summary>某一时刻的不可变游戏状态快照。</summary>
public sealed record GameStateSnapshot
{
    public long Version { get; init; }

    public required DateTimeOffset CapturedAt { get; init; }

    public required ObservationHealth Health { get; init; }

    public UserStateSnapshot? User { get; init; }

    public IReadOnlyDictionary<UiId, UiObservation> UiStates { get; init; }
        = EmptyUiStates;

    public int ConsecutiveFailures { get; init; }

    public string? Error { get; init; }

    private static IReadOnlyDictionary<UiId, UiObservation> EmptyUiStates { get; }
        = new ReadOnlyDictionary<UiId, UiObservation>(
            new Dictionary<UiId, UiObservation>());

    public static GameStateSnapshot Empty { get; } = new()
    {
        CapturedAt = DateTimeOffset.UtcNow,
        Health = ObservationHealth.Starting,
    };
}
