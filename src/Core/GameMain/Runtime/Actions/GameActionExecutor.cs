using System.Threading.Channels;

namespace MengYou.Runtime;

/// <summary>
/// 基于单读者 Channel 的会话动作执行器。动作严格串行，前台动作还会获取进程级协调锁。
/// </summary>
public sealed class GameActionExecutor : IGameActionExecutor
{
    private static readonly AsyncLocal<GameActionExecutor?> ExecutingExecutor = new();

    private readonly Guid _sessionId;
    private readonly string _displayName;
    private readonly IForegroundCoordinator _foregroundCoordinator;
    private readonly Channel<WorkItem> _queue;
    private readonly CancellationTokenSource _shutdown = new();
    private readonly object _lifecycleSync = new();
    private readonly Logger _logger;

    private Task? _worker;
    private bool _stopped;
    private bool _disposed;

    public GameActionExecutor(
        Guid sessionId,
        string displayName,
        IForegroundCoordinator? foregroundCoordinator = null,
        int queueCapacity = 256)
    {
        if (queueCapacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(queueCapacity), "动作队列容量必须大于零。");

        _sessionId = sessionId;
        _displayName = displayName;
        _foregroundCoordinator = foregroundCoordinator ?? ForegroundCoordinator.Shared;
        _logger = new Logger($"ActionExecutor:{displayName}");
        _queue = Channel.CreateBounded<WorkItem>(new BoundedChannelOptions(queueCapacity)
        {
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false,
            FullMode = BoundedChannelFullMode.Wait,
        });
    }

    public bool IsRunning
    {
        get
        {
            lock (_lifecycleSync)
            {
                return _worker is { IsCompleted: false } && !_stopped;
            }
        }
    }

    public Task StartAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        lock (_lifecycleSync)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_stopped)
                throw new InvalidOperationException("动作执行器已经停止，不能重新启动。");

            _worker ??= Task.Run(ProcessQueueAsync, CancellationToken.None);
        }

        return Task.CompletedTask;
    }

    public async Task<GameActionResult> ExecuteAsync(IGameAction action, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (ReferenceEquals(ExecutingExecutor.Value, this))
        {
            return GameActionResult.Faulted(
                action.Name,
                new InvalidOperationException(
                    "同一会话的动作内部不能再次提交排队动作，请注入底层直接操作端口。"));
        }

        if (ct.IsCancellationRequested)
            return GameActionResult.Interrupted(action.Name, "动作在入队前已取消。");

        try
        {
            await StartAsync(ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return GameActionResult.Interrupted(action.Name, "动作在入队前已取消。");
        }
        catch (InvalidOperationException)
        {
            return GameActionResult.Interrupted(action.Name, "会话动作执行器已经停止。");
        }

        var completion = new TaskCompletionSource<GameActionResult>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var item = new WorkItem(action, ct, completion);

        try
        {
            await _queue.Writer.WriteAsync(item, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return GameActionResult.Interrupted(action.Name, "动作在等待队列空间时被取消。");
        }
        catch (ChannelClosedException)
        {
            return GameActionResult.Interrupted(action.Name, "动作队列已经关闭。");
        }

        try
        {
            return await completion.Task.WaitAsync(ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return GameActionResult.Interrupted(action.Name, "调用方取消了动作。");
        }
    }

    public async Task StopAsync(CancellationToken ct = default)
    {
        Task? worker;
        lock (_lifecycleSync)
        {
            if (_stopped)
            {
                worker = _worker;
            }
            else
            {
                _stopped = true;
                _queue.Writer.TryComplete();
                _shutdown.Cancel();
                worker = _worker;
            }
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
    }

    private async Task ProcessQueueAsync()
    {
        try
        {
            await foreach (var item in _queue.Reader.ReadAllAsync(_shutdown.Token).ConfigureAwait(false))
            {
                var result = await ExecuteOneAsync(item).ConfigureAwait(false);
                item.Completion.TrySetResult(result);
            }
        }
        catch (OperationCanceledException) when (_shutdown.IsCancellationRequested)
        {
            // StopAsync 发起的正常取消。
        }
        finally
        {
            while (_queue.Reader.TryRead(out var pending))
            {
                pending.Completion.TrySetResult(
                    GameActionResult.Interrupted(pending.Action.Name, "会话已停止。"));
            }
        }
    }

    private async Task<GameActionResult> ExecuteOneAsync(WorkItem item)
    {
        var startedAt = DateTimeOffset.UtcNow;
        if (item.CancellationToken.IsCancellationRequested)
            return Complete(GameActionResult.Interrupted(item.Action.Name, "动作在执行前已取消。"), startedAt);

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            _shutdown.Token,
            item.CancellationToken);
        IDisposable? foregroundLease = null;
        var previousExecutor = ExecutingExecutor.Value;

        try
        {
            ExecutingExecutor.Value = this;
            if (item.Action.RequiredResources.HasFlag(GameActionResources.Foreground))
            {
                foregroundLease = await _foregroundCoordinator
                    .AcquireAsync(_sessionId, linkedCts.Token)
                    .ConfigureAwait(false);
            }

            _logger.LogFormat("开始动作 {0}，Session={1}", item.Action.Name, _displayName);
            var result = await item.Action.ExecuteAsync(linkedCts.Token).ConfigureAwait(false);
            return Complete(result with { ActionName = item.Action.Name }, startedAt);
        }
        catch (OperationCanceledException)
        {
            return Complete(GameActionResult.Interrupted(item.Action.Name, "动作执行被取消。"), startedAt);
        }
        catch (Exception ex)
        {
            _logger.LogFormat("动作 {0} 失败：{1}", item.Action.Name, ex.Message);
            return Complete(GameActionResult.Faulted(item.Action.Name, ex), startedAt);
        }
        finally
        {
            ExecutingExecutor.Value = previousExecutor;
            foregroundLease?.Dispose();
        }
    }

    private static GameActionResult Complete(GameActionResult result, DateTimeOffset startedAt)
        => result with
        {
            StartedAt = startedAt,
            CompletedAt = DateTimeOffset.UtcNow,
        };

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

    private sealed record WorkItem(
        IGameAction Action,
        CancellationToken CancellationToken,
        TaskCompletionSource<GameActionResult> Completion);
}
