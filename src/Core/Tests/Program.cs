using System.Collections.Concurrent;
using iFramework;
using MengYou.UI;
using MengYou.Runtime;

var tests = new (string Name, Func<Task> Run)[]
{
    ("单会话动作严格串行", SessionActionsAreSerialized),
    ("动作异常不会终止队列", FaultDoesNotStopQueue),
    ("多会话前台动作全局互斥", ForegroundActionsAreGloballySerialized),
    ("拒绝同会话嵌套动作", NestedActionIsRejected),
    ("停止会话会取消在途动作", StopInterruptsRunningAction),
    ("会话管理器释放后拒绝新会话", SessionManagerRejectsCreateAfterDispose),
    ("UI 注册表支持检测与控制扩展", UiRegistryRoutesModules),
    ("UI 未知状态不会触发切换操作", UnknownUiDoesNotToggle),
    ("状态存储版本单调并可等待变化", StateStorePublishesVersions),
    ("感知循环可从单次失败恢复", ObservationLoopRecoversAfterFailure),
    ("功能监督器隔离异常并拒绝重复运行", FeatureSupervisorIsolatesFaults),
    ("工作流支持顺序执行与重试", WorkflowSupportsSequenceAndRetry),
    ("工作流支持状态等待与超时", WorkflowSupportsWaitAndTimeout),
    ("自动恢复按阈值触发并执行冷却限流", AutoRecoveryUsesThresholdAndCooldown),
};

var failed = 0;
foreach (var test in tests)
{
    try
    {
        await test.Run();
        Console.WriteLine($"PASS {test.Name}");
    }
    catch (Exception ex)
    {
        failed++;
        Console.Error.WriteLine($"FAIL {test.Name}: {ex.Message}");
    }
}

Console.WriteLine($"完成：{tests.Length - failed}/{tests.Length} 通过");
return failed == 0 ? 0 : 1;

static async Task SessionActionsAreSerialized()
{
    await using var executor = new GameActionExecutor(Guid.NewGuid(), "serial-test");
    var running = 0;
    var maxRunning = 0;
    var completionOrder = new ConcurrentQueue<int>();

    var tasks = Enumerable.Range(0, 5)
        .Select(index => executor.ExecuteAsync(Action(
            $"serial:{index}",
            GameActionResources.None,
            async ct =>
            {
                var current = Interlocked.Increment(ref running);
                UpdateMaximum(ref maxRunning, current);
                await Task.Delay(20, ct);
                completionOrder.Enqueue(index);
                Interlocked.Decrement(ref running);
                return GameActionResult.Success($"serial:{index}");
            })))
        .ToArray();

    var results = await Task.WhenAll(tasks);
    Assert(results.All(result => result.IsSuccess), "存在未成功的动作。");
    Assert(maxRunning == 1, $"同一时刻执行了 {maxRunning} 个动作。");
    Assert(completionOrder.SequenceEqual(Enumerable.Range(0, 5)), "动作执行顺序与入队顺序不一致。");
}

static async Task FaultDoesNotStopQueue()
{
    await using var executor = new GameActionExecutor(Guid.NewGuid(), "fault-test");

    var failed = await executor.ExecuteAsync(Action(
        "throw",
        GameActionResources.None,
        _ => throw new InvalidOperationException("expected")));
    var succeeded = await executor.ExecuteAsync(Action(
        "after-throw",
        GameActionResources.None,
        _ => Task.FromResult(GameActionResult.Success("after-throw"))));

    Assert(failed.Status == GameActionStatus.Faulted, "异常动作没有转换为 Faulted 结果。");
    Assert(succeeded.IsSuccess, "异常后的动作未继续执行。");
}

static async Task ForegroundActionsAreGloballySerialized()
{
    var coordinator = new ForegroundCoordinator();
    await using var first = new GameActionExecutor(Guid.NewGuid(), "session-1", coordinator);
    await using var second = new GameActionExecutor(Guid.NewGuid(), "session-2", coordinator);
    var running = 0;
    var maxRunning = 0;

    Task<GameActionResult> Run(GameActionExecutor executor, string name)
        => executor.ExecuteAsync(Action(
            name,
            GameActionResources.Foreground,
            async ct =>
            {
                var current = Interlocked.Increment(ref running);
                UpdateMaximum(ref maxRunning, current);
                await Task.Delay(40, ct);
                Interlocked.Decrement(ref running);
                return GameActionResult.Success(name);
            }));

    var results = await Task.WhenAll(Run(first, "first"), Run(second, "second"));
    Assert(results.All(result => result.IsSuccess), "前台动作执行失败。");
    Assert(maxRunning == 1, $"检测到 {maxRunning} 个会话同时占用前台。");
}

static async Task StopInterruptsRunningAction()
{
    await using var executor = new GameActionExecutor(Guid.NewGuid(), "stop-test");
    var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

    var actionTask = executor.ExecuteAsync(Action(
        "long-running",
        GameActionResources.None,
        async ct =>
        {
            started.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, ct);
            return GameActionResult.Success("long-running");
        }));

    await started.Task.WaitAsync(TimeSpan.FromSeconds(2));
    await executor.StopAsync();
    var result = await actionTask;
    var afterStop = await executor.ExecuteAsync(Action(
        "after-stop",
        GameActionResources.None,
        _ => Task.FromResult(GameActionResult.Success("after-stop"))));

    Assert(result.Status == GameActionStatus.Interrupted, "在途动作没有被停止操作取消。");
    Assert(afterStop.Status == GameActionStatus.Interrupted, "停止后仍接受了新动作。");
}

static async Task NestedActionIsRejected()
{
    await using var executor = new GameActionExecutor(Guid.NewGuid(), "nested-test");
    GameActionResult? nestedResult = null;

    var outerResult = await executor.ExecuteAsync(Action(
        "outer",
        GameActionResources.None,
        async ct =>
        {
            nestedResult = await executor.ExecuteAsync(Action(
                "inner",
                GameActionResources.None,
                _ => Task.FromResult(GameActionResult.Success("inner"))), ct);
            return GameActionResult.Success("outer");
        }));

    Assert(outerResult.IsSuccess, "外层动作执行失败。");
    Assert(nestedResult?.Status == GameActionStatus.Faulted, "嵌套动作没有被明确拒绝。");
}

static Task SessionManagerRejectsCreateAfterDispose()
{
    var manager = new SessionManager(new ForegroundCoordinator());
    manager.Dispose();

    try
    {
        manager.Create(new SessionData());
        throw new InvalidOperationException("已释放的 SessionManager 仍创建了会话。");
    }
    catch (ObjectDisposedException)
    {
        return Task.CompletedTask;
    }
}

static async Task UiRegistryRoutesModules()
{
    var registry = new UiRegistry();
    var detector = new VisibleUiDetector(GameUiIds.Bag);
    var controller = new CountingUiController(GameUiIds.Bag);
    registry.RegisterDetector(detector);
    registry.RegisterController(controller);

    var observed = await registry.ObserveAsync(GameUiIds.Bag);
    var unknown = await registry.ObserveAsync(new UiId("未知面板"));
    await registry.OpenAsync(GameUiIds.Bag);
    await registry.CloseAsync(GameUiIds.Bag);

    Assert(observed.Visibility == UiVisibility.Visible, "注册的 UI 检测器未被调用。");
    Assert(unknown.Visibility == UiVisibility.Unknown, "未注册 UI 没有返回 Unknown。");
    Assert(controller.OpenCount == 1 && controller.CloseCount == 1, "UI 控制器路由次数不正确。");

    try
    {
        registry.RegisterDetector(detector);
        throw new InvalidOperationException("重复 UI 检测器注册未被拒绝。");
    }
    catch (InvalidOperationException ex) when (ex.Message.Contains("重复注册"))
    {
    }
}

static async Task StateStorePublishesVersions()
{
    var store = new GameStateStore();
    var initialVersion = store.Current.Version;
    var waitTask = store.WaitForChangeAsync(initialVersion);
    var mutableUiStates = new Dictionary<UiId, UiObservation>
    {
        [GameUiIds.Bag] = UiObservation.Unknown(GameUiIds.Bag),
    };

    var published = store.Publish(new GameStateSnapshot
    {
        CapturedAt = DateTimeOffset.UtcNow,
        Health = ObservationHealth.Healthy,
        UiStates = mutableUiStates,
    });
    mutableUiStates.Clear();
    var observed = await waitTask.WaitAsync(TimeSpan.FromSeconds(2));
    var next = store.Publish(published with { CapturedAt = DateTimeOffset.UtcNow });

    Assert(published.Version == initialVersion + 1, "首次发布的版本号不连续。");
    Assert(observed.Version == published.Version, "等待者没有收到已发布状态。");
    Assert(next.Version == published.Version + 1, "状态版本号没有单调递增。");
    Assert(published.UiStates.Count == 1, "状态存储没有冻结外部传入的 UI 字典。");
}

static async Task UnknownUiDoesNotToggle()
{
    var registry = new UiRegistry();
    registry.RegisterDetector(new UnknownUiDetector(GameUiIds.Bag));
    var control = new CountingGameControl();
    using var vision = new FakeVisionService();
    var provider = new HumanUIMgrProvider(registry, control, vision);

    var opened = await provider.ShowUI(GameUiIds.Bag.Value);
    var closed = await provider.CloseUI(GameUiIds.Bag.Value);

    Assert(!opened && !closed, "Unknown 状态被错误报告为操作成功。");
    Assert(control.ShowCount == 0 && control.CloseCount == 0, "Unknown 状态仍触发了切换操作。");
}

static async Task ObservationLoopRecoversAfterFailure()
{
    var calls = 0;
    var store = new GameStateStore();
    var observer = new DelegateGameStateObserver(_ =>
    {
        if (Interlocked.Increment(ref calls) == 1)
            return Task.FromException<GameStateSnapshot>(new InvalidOperationException("expected"));

        return Task.FromResult(new GameStateSnapshot
        {
            CapturedAt = DateTimeOffset.UtcNow,
            Health = ObservationHealth.Healthy,
        });
    });

    await using var loop = new GameObservationLoop(
        observer,
        store,
        store,
        interval: TimeSpan.FromMilliseconds(10),
        maxBackoff: TimeSpan.FromMilliseconds(40),
        displayName: "observation-test");

    await loop.StartAsync();
    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
    var state = store.Current;
    while (state.Health != ObservationHealth.Healthy)
        state = await store.WaitForChangeAsync(state.Version, timeout.Token);

    Assert(calls >= 2, "感知循环在失败后没有重试。");
    Assert(state.ConsecutiveFailures == 0 && state.Error == null, "恢复后未清除失败状态。");

    await loop.StopAsync();
    Assert(store.Current.Health == ObservationHealth.Stopped, "停止后状态未标记为 Stopped。");
}

static async Task FeatureSupervisorIsolatesFaults()
{
    var store = new GameStateStore();
    await using var executor = new GameActionExecutor(Guid.NewGuid(), "feature-test");
    await using var supervisor = new GameFeatureSupervisor(
        new GameFeatureContext(Guid.NewGuid(), "feature-test", store, executor));
    var longRunningStarted = new TaskCompletionSource(
        TaskCreationOptions.RunContinuationsAsynchronously);
    var longRunning = new DelegateFeature("long-running", async (context, ct) =>
    {
        using var registration = ct.Register(() => _ = supervisor.Statuses.Count);
        longRunningStarted.TrySetResult();
        await Task.Delay(Timeout.InfiniteTimeSpan, ct);
    });

    await supervisor.StartAsync(longRunning);
    await longRunningStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));
    await supervisor.StartAsync(new DelegateFeature(
        "faulting",
        (_, _) => Task.FromException(new InvalidOperationException("expected"))));

    var faulted = await WaitForFeatureStateAsync(
        supervisor,
        "faulting",
        GameFeatureRunState.Faulted);
    var stillRunning = supervisor.Statuses.Single(status => status.FeatureId == "long-running");
    Assert(faulted.Error?.Contains("expected") == true, "功能异常没有记录到监督状态。");
    Assert(stillRunning.State == GameFeatureRunState.Running, "单个功能异常停止了其他功能。");

    try
    {
        await supervisor.StartAsync(longRunning);
        throw new InvalidOperationException("监督器接受了重复运行的功能 Id。");
    }
    catch (InvalidOperationException ex) when (ex.Message.Contains("已经在运行"))
    {
    }

    var stopped = await supervisor.StopAsync("long-running");
    Assert(stopped?.State == GameFeatureRunState.Stopped, "功能停止后状态不正确。");
}

static async Task WorkflowSupportsSequenceAndRetry()
{
    var store = new GameStateStore();
    await using var executor = new GameActionExecutor(Guid.NewGuid(), "workflow-retry-test");
    var context = new GameFeatureContext(Guid.NewGuid(), "workflow-retry-test", store, executor);
    var attempts = 0;
    var finalStepCalls = 0;
    var retry = new RetryWorkflowStep(
        "retry",
        new ActionWorkflowStep(Action(
            "unstable",
            GameActionResources.None,
            _ =>
            {
                var current = Interlocked.Increment(ref attempts);
                return Task.FromResult(current < 3
                    ? GameActionResult.VerificationFailed("unstable", "retry")
                    : GameActionResult.Success("unstable"));
            })),
        maxAttempts: 3,
        delay: TimeSpan.Zero);
    var sequence = new SequenceWorkflowStep(
        "sequence",
        retry,
        new ActionWorkflowStep(Action(
            "final",
            GameActionResources.None,
            _ =>
            {
                Interlocked.Increment(ref finalStepCalls);
                return Task.FromResult(GameActionResult.Success("final"));
            })));

    var result = await sequence.ExecuteAsync(context);

    Assert(result.IsSuccess, "重试成功后工作流仍报告失败。");
    Assert(attempts == 3, $"预期重试 3 次，实际执行 {attempts} 次。");
    Assert(finalStepCalls == 1, "重试成功后没有继续执行后续步骤。");
    Assert(result.Children.Count == 2, "顺序工作流没有保留子步骤结果。");
}

static async Task WorkflowSupportsWaitAndTimeout()
{
    var store = new GameStateStore();
    await using var executor = new GameActionExecutor(Guid.NewGuid(), "workflow-timeout-test");
    var context = new GameFeatureContext(Guid.NewGuid(), "workflow-timeout-test", store, executor);
    var waitStep = new WaitUntilWorkflowStep(
        "wait-healthy",
        state => state.Health == ObservationHealth.Healthy,
        TimeSpan.FromSeconds(1));

    var waitTask = waitStep.ExecuteAsync(context);
    store.Publish(new GameStateSnapshot
    {
        CapturedAt = DateTimeOffset.UtcNow,
        Health = ObservationHealth.Healthy,
    });
    var waitResult = await waitTask;
    var timeoutStep = new TimeoutWorkflowStep(
        "timeout",
        new DelegateWorkflowStep("never", async (_, ct) =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, ct);
            return WorkflowStepResult.Success("never");
        }),
        TimeSpan.FromMilliseconds(30));
    var timeoutResult = await timeoutStep.ExecuteAsync(context);

    Assert(waitResult.IsSuccess, "状态满足后等待步骤没有成功。");
    Assert(timeoutResult.Status == WorkflowStepStatus.TimedOut, "超时边界没有返回 TimedOut。");
}

static async Task AutoRecoveryUsesThresholdAndCooldown()
{
    var store = new GameStateStore();
    await using var executor = new GameActionExecutor(Guid.NewGuid(), "recovery-test");
    await using var supervisor = new GameFeatureSupervisor(
        new GameFeatureContext(Guid.NewGuid(), "recovery-test", store, executor));
    var recovery = new FakeRecoveryService();
    var feature = new AutoRecoveryFeature(recovery, new AutoRecoveryOptions
    {
        HealthThreshold = 0.5,
        HealthItemName = "test-health-item",
        EnableMana = false,
        Cooldown = TimeSpan.FromMinutes(1),
    });

    await supervisor.StartAsync(feature);
    var lowHealth = new GameStateSnapshot
    {
        CapturedAt = DateTimeOffset.UtcNow,
        Health = ObservationHealth.Healthy,
        User = new UserStateSnapshot
        {
            Hp = 40,
            MaxHp = 100,
            Mp = 100,
            MaxMp = 100,
        },
    };
    store.Publish(lowHealth);
    var call = await recovery.FirstCall.Task.WaitAsync(TimeSpan.FromSeconds(2));
    store.Publish(lowHealth with { CapturedAt = DateTimeOffset.UtcNow });
    await Task.Delay(50);

    Assert(call.Resource == RecoveryResource.Health, "低血量没有选择生命恢复。");
    Assert(call.ItemName == "test-health-item", "恢复功能没有使用配置的物品。");
    Assert(recovery.CallCount == 1, "冷却期间重复触发了恢复。");
    Assert(feature.LastResult?.IsSuccess == true, "恢复结果没有保存到功能状态。");

    await supervisor.StopAsync(feature.Id);
}

static async Task<GameFeatureStatus> WaitForFeatureStateAsync(
    IGameFeatureSupervisor supervisor,
    string featureId,
    GameFeatureRunState expected)
{
    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
    while (true)
    {
        timeout.Token.ThrowIfCancellationRequested();
        var status = supervisor.Statuses.Single(item => item.FeatureId == featureId);
        if (status.State == expected) return status;
        await Task.Delay(10, timeout.Token);
    }
}

static DelegateGameAction Action(
    string name,
    GameActionResources resources,
    Func<CancellationToken, Task<GameActionResult>> execute)
    => new(name, resources, execute);

static void UpdateMaximum(ref int maximum, int value)
{
    while (true)
    {
        var current = Volatile.Read(ref maximum);
        if (value <= current) return;
        if (Interlocked.CompareExchange(ref maximum, value, current) == current) return;
    }
}

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

sealed class VisibleUiDetector : IUiStateDetector
{
    public VisibleUiDetector(UiId uiId) => UiId = uiId;

    public UiId UiId { get; }

    public Task<UiObservation> ObserveAsync(CancellationToken ct = default)
        => Task.FromResult(new UiObservation
        {
            UiId = UiId,
            Visibility = UiVisibility.Visible,
            Confidence = 1,
            ObservedAt = DateTimeOffset.UtcNow,
        });
}

sealed class CountingUiController : IUiController
{
    public CountingUiController(UiId uiId) => UiId = uiId;

    public UiId UiId { get; }

    public int OpenCount { get; private set; }

    public int CloseCount { get; private set; }

    public Task OpenAsync(CancellationToken ct = default)
    {
        OpenCount++;
        return Task.CompletedTask;
    }

    public Task CloseAsync(CancellationToken ct = default)
    {
        CloseCount++;
        return Task.CompletedTask;
    }
}

sealed class UnknownUiDetector : IUiStateDetector
{
    public UnknownUiDetector(UiId uiId) => UiId = uiId;

    public UiId UiId { get; }

    public Task<UiObservation> ObserveAsync(CancellationToken ct = default)
        => Task.FromResult(UiObservation.Unknown(UiId, "test"));
}

sealed class CountingGameControl : IGameControl
{
    public int ShowCount { get; private set; }

    public int CloseCount { get; private set; }

    public Task<bool> UseBagItem(
        BagType bagType,
        int bagIndex,
        int slotIndex,
        int useCount,
        CancellationToken ct = default)
        => Task.FromResult(true);

    public Task ShowUI(string uiName, CancellationToken ct = default)
    {
        ShowCount++;
        return Task.CompletedTask;
    }

    public Task CloseUI(string uiName, CancellationToken ct = default)
    {
        CloseCount++;
        return Task.CompletedTask;
    }
}

sealed class FakeVisionService : IVisionServiceMgr
{
    public bool CanReadText => false;

    public Vector2? FindTemplate(string templateKey, double threshold = 0.85) => null;

    public IReadOnlyList<Vector2> FindTemplateAll(string templateKey, double threshold = 0.85)
        => Array.Empty<Vector2>();

    public string? ReadText(Rect region) => null;

    public int? ReadNumber(Rect region) => null;

    public int GetPixel(Vector2 point) => 0;

    public void Refresh()
    {
    }

    public void Dispose()
    {
    }
}

sealed class DelegateFeature : IGameFeature
{
    private readonly Func<GameFeatureContext, CancellationToken, Task> _run;

    public DelegateFeature(
        string id,
        Func<GameFeatureContext, CancellationToken, Task> run)
    {
        Id = id;
        DisplayName = id;
        _run = run;
    }

    public string Id { get; }

    public string DisplayName { get; }

    public Task RunAsync(GameFeatureContext context, CancellationToken ct = default)
        => _run(context, ct);
}

sealed class DelegateWorkflowStep : IWorkflowStep
{
    private readonly Func<GameFeatureContext, CancellationToken, Task<WorkflowStepResult>> _execute;

    public DelegateWorkflowStep(
        string name,
        Func<GameFeatureContext, CancellationToken, Task<WorkflowStepResult>> execute)
    {
        Name = name;
        _execute = execute;
    }

    public string Name { get; }

    public Task<WorkflowStepResult> ExecuteAsync(
        GameFeatureContext context,
        CancellationToken ct = default)
        => _execute(context, ct);
}

sealed class FakeRecoveryService : IRecoveryService
{
    public TaskCompletionSource<RecoveryCall> FirstCall { get; }
        = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public int CallCount { get; private set; }

    public Task<GameActionResult> RecoverAsync(
        RecoveryResource resource,
        string itemName,
        int useCount,
        CancellationToken ct = default)
    {
        CallCount++;
        FirstCall.TrySetResult(new RecoveryCall(resource, itemName, useCount));
        return Task.FromResult(GameActionResult.Success($"fake-recovery:{resource}"));
    }
}

sealed record RecoveryCall(
    RecoveryResource Resource,
    string ItemName,
    int UseCount);
