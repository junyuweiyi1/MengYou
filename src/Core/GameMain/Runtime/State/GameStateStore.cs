using System.Runtime.CompilerServices;
using System.Collections.ObjectModel;
using MengYou.UI;

namespace MengYou.Runtime;

/// <summary>会话级状态存储；发布时自动分配单调递增的版本号。</summary>
public sealed class GameStateStore : IGameStateStore, IGameStatePublisher
{
    private readonly object _sync = new();
    private GameStateSnapshot _current = GameStateSnapshot.Empty;
    private TaskCompletionSource<bool> _changed = NewSignal();

    public GameStateSnapshot Current => Volatile.Read(ref _current);

    public GameStateSnapshot Publish(GameStateSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        TaskCompletionSource<bool> signal;
        GameStateSnapshot published;
        lock (_sync)
        {
            published = snapshot with
            {
                Version = _current.Version + 1,
                UiStates = Freeze(snapshot.UiStates),
            };
            Volatile.Write(ref _current, published);
            signal = _changed;
            _changed = NewSignal();
        }

        signal.TrySetResult(true);
        return published;
    }

    public async Task<GameStateSnapshot> WaitForChangeAsync(
        long afterVersion,
        CancellationToken ct = default)
    {
        while (true)
        {
            Task waitTask;
            lock (_sync)
            {
                if (_current.Version > afterVersion)
                    return _current;
                waitTask = _changed.Task;
            }

            await waitTask.WaitAsync(ct).ConfigureAwait(false);
        }
    }

    public async IAsyncEnumerable<GameStateSnapshot> WatchAsync(
        long afterVersion = -1,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var version = afterVersion;
        while (!ct.IsCancellationRequested)
        {
            var next = await WaitForChangeAsync(version, ct).ConfigureAwait(false);
            version = next.Version;
            yield return next;
        }
    }

    private static TaskCompletionSource<bool> NewSignal()
        => new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static IReadOnlyDictionary<UiId, UiObservation> Freeze(
        IReadOnlyDictionary<UiId, UiObservation> source)
        => new ReadOnlyDictionary<UiId, UiObservation>(
            new Dictionary<UiId, UiObservation>(source));
}
