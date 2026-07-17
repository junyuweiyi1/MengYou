namespace MengYou.Runtime;

/// <summary>
/// 功能生命周期监督器。每个功能拥有独立取消源；单个功能异常不会影响动作循环或其他功能。
/// </summary>
public sealed class GameFeatureSupervisor : IGameFeatureSupervisor
{
    private readonly GameFeatureContext _context;
    private readonly Dictionary<string, Entry> _entries = new(StringComparer.OrdinalIgnoreCase);
    private readonly CancellationTokenSource _shutdown = new();
    private readonly object _sync = new();
    private readonly Logger _logger;

    private bool _disposed;

    public GameFeatureSupervisor(GameFeatureContext context)
    {
        _context = context;
        _logger = new Logger($"FeatureSupervisor:{context.DisplayName}");
    }

    public IReadOnlyCollection<GameFeatureStatus> Statuses
    {
        get
        {
            lock (_sync)
                return _entries.Values.Select(ToStatus).ToArray();
        }
    }

    public Task<GameFeatureStatus> StartAsync(
        IGameFeature feature,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(feature);
        ct.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(feature.Id))
            throw new ArgumentException("功能 Id 不能为空。", nameof(feature));

        Entry entry;
        lock (_sync)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_entries.TryGetValue(feature.Id, out var existing))
            {
                if (existing.State is GameFeatureRunState.Running or GameFeatureRunState.Stopping)
                    throw new InvalidOperationException($"功能已经在运行：{feature.Id}");
                existing.Cancellation.Dispose();
                _entries.Remove(feature.Id);
            }

            var cancellation = CancellationTokenSource.CreateLinkedTokenSource(_shutdown.Token);
            entry = new Entry(feature, cancellation)
            {
                State = GameFeatureRunState.Running,
                StartedAt = DateTimeOffset.UtcNow,
            };
            _entries.Add(feature.Id, entry);
            entry.Task = Task.Run(() => RunFeatureAsync(entry), CancellationToken.None);
        }

        _logger.LogFormat("功能已启动：{0}", feature.Id);
        return Task.FromResult(ToStatus(entry));
    }

    public async Task<GameFeatureStatus?> StopAsync(
        string featureId,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(featureId))
            throw new ArgumentException("功能 Id 不能为空。", nameof(featureId));

        Entry? entry;
        Task? task;
        CancellationTokenSource? cancellation = null;
        lock (_sync)
        {
            if (!_entries.TryGetValue(featureId, out entry))
                return null;

            if (entry.State is GameFeatureRunState.Running)
            {
                entry.State = GameFeatureRunState.Stopping;
                cancellation = entry.Cancellation;
            }
            task = entry.Task;
        }

        // CancellationToken 回调可能查询监督器状态，不能在持有内部锁时执行回调。
        cancellation?.Cancel();

        if (task != null)
            await task.WaitAsync(ct).ConfigureAwait(false);

        lock (_sync)
            return ToStatus(entry);
    }

    public async Task StopAllAsync(CancellationToken ct = default)
    {
        string[] ids;
        lock (_sync)
            ids = _entries.Keys.ToArray();

        foreach (var id in ids)
        {
            ct.ThrowIfCancellationRequested();
            await StopAsync(id, ct).ConfigureAwait(false);
        }
    }

    private async Task RunFeatureAsync(Entry entry)
    {
        try
        {
            await entry.Feature
                .RunAsync(_context, entry.Cancellation.Token)
                .ConfigureAwait(false);
            lock (_sync)
            {
                entry.State = entry.Cancellation.IsCancellationRequested
                    ? GameFeatureRunState.Stopped
                    : GameFeatureRunState.Completed;
                entry.FinishedAt = DateTimeOffset.UtcNow;
            }
        }
        catch (OperationCanceledException) when (entry.Cancellation.IsCancellationRequested)
        {
            lock (_sync)
            {
                entry.State = GameFeatureRunState.Stopped;
                entry.FinishedAt = DateTimeOffset.UtcNow;
            }
        }
        catch (Exception ex)
        {
            _logger.LogFormat("功能 {0} 异常：{1}", entry.Feature.Id, ex.Message);
            lock (_sync)
            {
                entry.State = GameFeatureRunState.Faulted;
                entry.FinishedAt = DateTimeOffset.UtcNow;
                entry.Error = ex.Message;
            }
        }
    }

    private static GameFeatureStatus ToStatus(Entry entry)
        => new()
        {
            FeatureId = entry.Feature.Id,
            DisplayName = entry.Feature.DisplayName,
            State = entry.State,
            StartedAt = entry.StartedAt,
            FinishedAt = entry.FinishedAt,
            Error = entry.Error,
        };

    public void Dispose()
    {
        if (!BeginDispose()) return;
        _shutdown.Cancel();
        StopAllAsync().GetAwaiter().GetResult();
        DisposeResources();
    }

    public async ValueTask DisposeAsync()
    {
        if (!BeginDispose()) return;
        _shutdown.Cancel();
        await StopAllAsync().ConfigureAwait(false);
        DisposeResources();
    }

    private bool BeginDispose()
    {
        lock (_sync)
        {
            if (_disposed) return false;
            _disposed = true;
            return true;
        }
    }

    private void DisposeResources()
    {
        lock (_sync)
        {
            foreach (var entry in _entries.Values)
                entry.Cancellation.Dispose();
        }
        _shutdown.Dispose();
    }

    private sealed class Entry
    {
        public Entry(IGameFeature feature, CancellationTokenSource cancellation)
        {
            Feature = feature;
            Cancellation = cancellation;
        }

        public IGameFeature Feature { get; }

        public CancellationTokenSource Cancellation { get; }

        public Task? Task { get; set; }

        public GameFeatureRunState State { get; set; }

        public DateTimeOffset StartedAt { get; set; }

        public DateTimeOffset? FinishedAt { get; set; }

        public string? Error { get; set; }
    }
}
