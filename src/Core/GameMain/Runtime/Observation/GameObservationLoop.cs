using System.Diagnostics;

namespace MengYou.Runtime;

/// <summary>
/// 周期性生成游戏快照。单次感知异常只会将状态标为 Degraded，循环会按退避策略继续恢复。
/// </summary>
public sealed class GameObservationLoop : IGameObservationLoop
{
    private readonly IGameStateObserver _observer;
    private readonly IGameStateStore _stateStore;
    private readonly IGameStatePublisher _publisher;
    private readonly TimeSpan _interval;
    private readonly TimeSpan _maxBackoff;
    private readonly CancellationTokenSource _shutdown = new();
    private readonly object _lifecycleSync = new();
    private readonly Logger _logger;

    private Task? _worker;
    private bool _stopped;
    private bool _disposed;

    public GameObservationLoop(
        IGameStateObserver observer,
        IGameStateStore stateStore,
        IGameStatePublisher publisher,
        TimeSpan? interval = null,
        TimeSpan? maxBackoff = null,
        string displayName = "Game")
    {
        _observer = observer;
        _stateStore = stateStore;
        _publisher = publisher;
        _interval = interval ?? TimeSpan.FromMilliseconds(250);
        _maxBackoff = maxBackoff ?? TimeSpan.FromSeconds(2);
        if (_interval <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(interval));
        if (_maxBackoff < _interval)
            throw new ArgumentOutOfRangeException(nameof(maxBackoff));
        _logger = new Logger($"ObservationLoop:{displayName}");
    }

    public bool IsRunning
    {
        get
        {
            lock (_lifecycleSync)
                return _worker is { IsCompleted: false } && !_stopped;
        }
    }

    public Task StartAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        lock (_lifecycleSync)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_stopped)
                throw new InvalidOperationException("感知循环已经停止，不能重新启动。");
            _worker ??= Task.Run(RunAsync, CancellationToken.None);
        }
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken ct = default)
    {
        Task? worker;
        lock (_lifecycleSync)
        {
            if (!_stopped)
            {
                _stopped = true;
                _shutdown.Cancel();
            }
            worker = _worker;
        }

        if (worker != null)
        {
            try
            {
                await worker.WaitAsync(ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (_shutdown.IsCancellationRequested && !ct.IsCancellationRequested)
            {
                // 正常停止。
            }
        }

        PublishStopped();
    }

    private async Task RunAsync()
    {
        var failures = 0;
        while (!_shutdown.IsCancellationRequested)
        {
            var stopwatch = Stopwatch.StartNew();
            TimeSpan delay;

            try
            {
                var snapshot = await _observer
                    .ObserveAsync(_shutdown.Token)
                    .ConfigureAwait(false);
                failures = 0;
                _publisher.Publish(snapshot with
                {
                    CapturedAt = snapshot.CapturedAt,
                    Health = ObservationHealth.Healthy,
                    ConsecutiveFailures = 0,
                    Error = null,
                });
                delay = _interval - stopwatch.Elapsed;
            }
            catch (OperationCanceledException) when (_shutdown.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                failures++;
                _logger.LogFormat("第 {0} 次感知失败：{1}", failures, ex.Message);
                var current = _stateStore.Current;
                _publisher.Publish(current with
                {
                    CapturedAt = DateTimeOffset.UtcNow,
                    Health = ObservationHealth.Degraded,
                    ConsecutiveFailures = failures,
                    Error = ex.Message,
                });
                delay = CalculateBackoff(failures);
            }

            if (delay <= TimeSpan.Zero) continue;
            try
            {
                await Task.Delay(delay, _shutdown.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (_shutdown.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private TimeSpan CalculateBackoff(int failures)
    {
        var multiplier = Math.Pow(2, Math.Min(failures - 1, 6));
        var milliseconds = Math.Min(
            _maxBackoff.TotalMilliseconds,
            _interval.TotalMilliseconds * multiplier);
        return TimeSpan.FromMilliseconds(milliseconds);
    }

    private void PublishStopped()
    {
        var current = _stateStore.Current;
        if (current.Health == ObservationHealth.Stopped) return;
        _publisher.Publish(current with
        {
            CapturedAt = DateTimeOffset.UtcNow,
            Health = ObservationHealth.Stopped,
        });
    }

    public void Dispose()
    {
        if (_disposed) return;
        StopAsync().GetAwaiter().GetResult();
        _shutdown.Dispose();
        _disposed = true;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        await StopAsync().ConfigureAwait(false);
        _shutdown.Dispose();
        _disposed = true;
    }
}
