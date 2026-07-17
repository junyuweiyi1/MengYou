using System.Collections.Concurrent;
using System.Drawing;
using System.Drawing.Text;
using MengYou.Core.Application.Runtime;
using MengYou.Core.Business;
using MengYou.Core.Business.Inventory;
using MengYou.Core.Business.Models;
using MengYou.Core.Business.Ports;
using MengYou.Core.Business.Recovery;

var tests = new (string Name, Func<Task> Run)[]
{
    ("低血量触发加血并执行冷却", AutoHealUsesThresholdAndCooldown),
    ("默认连续两帧低血量才加血", AutoHealRequiresConfirmation),
    ("加血优先于同周期定时物品", HealingHasPriority),
    ("定时物品失败后按配置重试", ScheduledItemRetriesAfterFailure),
    ("无效生命值不会误用药", InvalidVitalsDoNotHeal),
    ("UI 和物品动作允许并发执行", GameActionsCanRunConcurrently),
    ("运行循环可取消并正常结束", RunLoopStopsOnCancellation),
    ("业务配置在启动前完整校验", OptionsAreValidated),
    ("生命值数字模板 OCR 可识别多位数", TemplateOcrReadsNumbers),
    ("SessionManager 只允许一个活动会话", SessionManagerAllowsOnlyOneSession),
};

var passed = 0;
foreach (var test in tests)
{
    try
    {
        await test.Run();
        Console.WriteLine($"PASS {test.Name}");
        passed++;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"FAIL {test.Name}: {ex.Message}");
    }
}

Console.WriteLine($"完成：{passed}/{tests.Length} 通过");
return passed == tests.Length ? 0 : 1;

static async Task AutoHealUsesThresholdAndCooldown()
{
    var port = new FakeGamePort { Vitals = new PlayerVitals(30, 100) };
    using var runtime = new SingleGameAutomationRuntime(port, new AutomationOptions
    {
        AutoHeal = HealRule(cooldown: TimeSpan.FromSeconds(10)),
    });
    var now = DateTimeOffset.Parse("2026-01-01T00:00:00Z");

    await runtime.RunCycleAsync(now);
    await runtime.RunCycleAsync(now.AddSeconds(9));
    Assert(port.UsedItems.Count == 1, "冷却期间不应重复加血");

    await runtime.RunCycleAsync(now.AddSeconds(10));
    Assert(port.UsedItems.Count == 2, "冷却结束后应再次加血");
    Assert(runtime.Status.SuccessfulActionCount == 2, "成功计数不正确");
}

static async Task AutoHealRequiresConfirmation()
{
    var port = new FakeGamePort { Vitals = new PlayerVitals(25, 100) };
    using var runtime = new SingleGameAutomationRuntime(port, new AutomationOptions
    {
        AutoHeal = new AutoHealRule
        {
            TriggerHealthRatio = 0.5,
            Item = ItemTarget.BySlot(5),
            Cooldown = TimeSpan.FromSeconds(5),
        },
    });
    var now = DateTimeOffset.Parse("2026-01-01T00:00:00Z");

    await runtime.RunCycleAsync(now);
    Assert(port.UsedItems.Count == 0, "单帧低血量不应立刻触发");
    await runtime.RunCycleAsync(now.AddSeconds(1));
    Assert(port.UsedItems.Count == 1, "连续两帧低血量应触发加血");
}

static async Task HealingHasPriority()
{
    var port = new FakeGamePort { Vitals = new PlayerVitals(20, 100) };
    using var runtime = new SingleGameAutomationRuntime(port, new AutomationOptions
    {
        AutoHeal = HealRule(cooldown: TimeSpan.FromMinutes(1)),
        AutoUseItems = new[]
        {
            new AutoUseItemRule
            {
                Name = "自动吃增益药",
                Item = ItemTarget.BySlot(8),
                Interval = TimeSpan.FromMinutes(5),
            },
        },
    });
    var now = DateTimeOffset.Parse("2026-01-01T00:00:00Z");

    await runtime.RunCycleAsync(now);
    Assert(port.UsedItems.Single().Item.SlotIndex == 5, "首个动作应为加血物品");

    await runtime.RunCycleAsync(now.AddSeconds(1));
    Assert(port.UsedItems.Count == 2 && port.UsedItems[1].Item.SlotIndex == 8, "下一周期应执行到期物品");
}

static async Task ScheduledItemRetriesAfterFailure()
{
    var port = new FakeGamePort();
    port.UseResults.Enqueue(false);
    port.UseResults.Enqueue(true);
    using var runtime = new SingleGameAutomationRuntime(port, new AutomationOptions
    {
        AutoUseItems = new[]
        {
            new AutoUseItemRule
            {
                Name = "自动使用飞行符",
                Item = ItemTarget.BySlot(2),
                Interval = TimeSpan.FromMinutes(1),
                FailureRetryInterval = TimeSpan.FromSeconds(5),
            },
        },
    });
    var now = DateTimeOffset.Parse("2026-01-01T00:00:00Z");

    await runtime.RunCycleAsync(now);
    await runtime.RunCycleAsync(now.AddSeconds(4));
    Assert(port.UsedItems.Count == 1, "失败重试间隔前不应再次执行");

    await runtime.RunCycleAsync(now.AddSeconds(5));
    Assert(port.UsedItems.Count == 2, "失败重试间隔后应再次执行");
    Assert(runtime.Status.FailedActionCount == 1 && runtime.Status.SuccessfulActionCount == 1, "动作结果计数不正确");
}

static async Task InvalidVitalsDoNotHeal()
{
    var port = new FakeGamePort { Vitals = new PlayerVitals(0, 0) };
    using var runtime = new SingleGameAutomationRuntime(port, new AutomationOptions
    {
        AutoHeal = HealRule(TimeSpan.FromSeconds(1)),
    });

    await runtime.RunCycleAsync(DateTimeOffset.UtcNow);
    Assert(port.UsedItems.Count == 0, "无法识别生命上限时不能盲目用药");
    Assert(runtime.Status.LastError?.Contains("生命值数据无效") == true, "应暴露可诊断状态");
}

static async Task GameActionsCanRunConcurrently()
{
    var port = new FakeGamePort { OperationDelay = TimeSpan.FromMilliseconds(40) };
    using var runtime = new SingleGameAutomationRuntime(port, new AutomationOptions
    {
        AutoUseItems = new[]
        {
            new AutoUseItemRule
            {
                Name = "自动物品",
                Item = ItemTarget.BySlot(2),
                Interval = TimeSpan.FromMinutes(1),
            },
        },
    });

    await Task.WhenAll(
        runtime.RunCycleAsync(DateTimeOffset.UtcNow),
        runtime.OpenUiAsync("道具行囊"),
        runtime.UseItemNowAsync(ItemTarget.BySlot(3)),
        runtime.CloseUiAsync("道具行囊"));

    Assert(port.MaximumConcurrentOperations >= 2, "游戏动作仍被串行锁限制");
    Assert(port.OpenedUis.SequenceEqual(new[] { "道具行囊" }), "打开 UI 未执行");
    Assert(port.ClosedUis.SequenceEqual(new[] { "道具行囊" }), "关闭 UI 未执行");
}

static async Task RunLoopStopsOnCancellation()
{
    var port = new FakeGamePort();
    using var runtime = new SingleGameAutomationRuntime(port, new AutomationOptions
    {
        PollInterval = TimeSpan.FromMilliseconds(10),
        AutoUseItems = new[]
        {
            new AutoUseItemRule
            {
                Item = ItemTarget.BySlot(1),
                Interval = TimeSpan.FromHours(1),
            },
        },
    });
    using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(80));

    await runtime.RunAsync(cts.Token);
    Assert(!runtime.Status.IsRunning, "取消后状态仍显示运行中");
    Assert(port.UsedItems.Count == 1, "循环应在第一次周期立即执行物品");
}

static Task OptionsAreValidated()
{
    var threw = false;
    try
    {
        _ = new SingleGameAutomationRuntime(new FakeGamePort(), new AutomationOptions
        {
            AutoHeal = new AutoHealRule
            {
                TriggerHealthRatio = 1.5,
                Item = ItemTarget.BySlot(1),
            },
        });
    }
    catch (ArgumentOutOfRangeException)
    {
        threw = true;
    }
    Assert(threw, "非法阈值应在创建运行器时被拒绝");
    return Task.CompletedTask;
}

static Task TemplateOcrReadsNumbers()
{
    var directory = Path.Combine(Path.GetTempPath(), "mengyou-ocr-" + Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(directory);
    try
    {
        using var font = new Font("Consolas", 24, FontStyle.Bold, GraphicsUnit.Pixel);
        for (var digit = 0; digit <= 9; digit++)
        {
            using var template = DrawDigits(digit.ToString(), font, 32);
            template.Save(Path.Combine(directory, digit + ".png"));
        }

        using var engine = new iFramework.TemplateDigitOcrEngine(directory);
        for (var digit = 0; digit <= 9; digit++)
        {
            using var single = DrawDigits(digit.ToString(), font, 32);
            var singleResult = engine.RecognizeNumber(single);
            Assert(singleResult == digit, $"单数字 {digit} 被识别为 {singleResult?.ToString() ?? "null"}");
        }
        using var input = ComposeDigits("507", font);
        var recognized = engine.RecognizeNumber(input);
        Assert(recognized == 507, $"数字模板 OCR 未能识别 507，实际为 {recognized?.ToString() ?? "null"}");
    }
    finally
    {
        Directory.Delete(directory, recursive: true);
    }
    return Task.CompletedTask;
}

static Bitmap DrawDigits(string text, Font font, int width)
{
    var bitmap = new Bitmap(width, 40);
    using var graphics = Graphics.FromImage(bitmap);
    graphics.Clear(Color.Black);
    graphics.TextRenderingHint = TextRenderingHint.SingleBitPerPixelGridFit;
    graphics.DrawString(text, font, Brushes.White, new PointF(0, 4), StringFormat.GenericTypographic);
    return bitmap;
}

static Bitmap ComposeDigits(string text, Font font)
{
    var bitmap = new Bitmap(text.Length * 32, 40);
    using var graphics = Graphics.FromImage(bitmap);
    graphics.Clear(Color.Black);
    for (var i = 0; i < text.Length; i++)
    {
        using var digit = DrawDigits(text[i].ToString(), font, 32);
        graphics.DrawImageUnscaled(digit, i * 32, 0);
    }
    return bitmap;
}

static async Task SessionManagerAllowsOnlyOneSession()
{
    using var window = new System.Windows.Forms.Form();
    var windowHandle = window.Handle;
    await using var manager = new SessionManager();
    var first = manager.Create(new SessionData
    {
        WindowHandle = windowHandle,
        DisplayName = "测试会话",
        InputMode = iFramework.InputMode.Background,
    });

    await first.StartAsync();
    Assert(first.State == SessionState.Running, "会话未进入 Running");

    var rejected = false;
    try
    {
        manager.Create(new SessionData
        {
            WindowHandle = windowHandle,
            InputMode = iFramework.InputMode.Background,
        });
    }
    catch (InvalidOperationException)
    {
        rejected = true;
    }

    Assert(rejected, "第二个活动 GameSession 应被拒绝");
    Assert(manager.All.Count == 1 && ReferenceEquals(manager.Current, first), "单会话视图不正确");
    await manager.RemoveAsync(first.SessionId);
    Assert(manager.Current is null, "移除后仍残留当前会话");
}

static AutoHealRule HealRule(TimeSpan cooldown) => new()
{
    TriggerHealthRatio = 0.5,
    Item = ItemTarget.BySlot(5),
    Cooldown = cooldown,
    RequiredConsecutiveLowReadings = 1,
};

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

internal sealed class FakeGamePort : IGameAutomationPort
{
    private int _runningOperations;
    private int _maximumConcurrentOperations;

    public PlayerVitals Vitals { get; set; } = new(100, 100);
    public TimeSpan OperationDelay { get; set; }
    public ConcurrentQueue<bool> UseResults { get; } = new();
    public List<(ItemTarget Item, int Count)> UsedItems { get; } = new();
    public List<string> OpenedUis { get; } = new();
    public List<string> ClosedUis { get; } = new();
    public int MaximumConcurrentOperations => Volatile.Read(ref _maximumConcurrentOperations);

    public Task<PlayerVitals> ReadVitalsAsync(CancellationToken ct = default)
        => InOperation(() => Vitals, ct);

    public Task<bool> UseItemAsync(ItemTarget item, int useCount, CancellationToken ct = default)
        => InOperation(() =>
        {
            lock (UsedItems) UsedItems.Add((item, useCount));
            return UseResults.TryDequeue(out var result) ? result : true;
        }, ct);

    public Task<bool> OpenUiAsync(string uiName, CancellationToken ct = default)
        => InOperation(() => { OpenedUis.Add(uiName); return true; }, ct);

    public Task<bool> CloseUiAsync(string uiName, CancellationToken ct = default)
        => InOperation(() => { ClosedUis.Add(uiName); return true; }, ct);

    private async Task<T> InOperation<T>(Func<T> complete, CancellationToken ct)
    {
        var running = Interlocked.Increment(ref _runningOperations);
        UpdateMaximum(running);
        try
        {
            if (OperationDelay > TimeSpan.Zero)
                await Task.Delay(OperationDelay, ct);
            return complete();
        }
        finally
        {
            Interlocked.Decrement(ref _runningOperations);
        }
    }

    private void UpdateMaximum(int value)
    {
        while (true)
        {
            var current = Volatile.Read(ref _maximumConcurrentOperations);
            if (value <= current || Interlocked.CompareExchange(ref _maximumConcurrentOperations, value, current) == current)
                return;
        }
    }
}
