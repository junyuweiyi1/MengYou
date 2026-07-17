# 单 GameSession 自动化

当前版本只挂载一个游戏窗口，完整链路是：`SessionManager` → `GameSession` →
`SingleGameAutomationRuntime` → `GameAutomationPort` → 现有截图/输入/背包/UI 实现。
自动动作与手动动作相互独立，可以同时调用游戏端口。

## 最小接入

```csharp
using MengYou.Core.Business;
using MengYou.Core.Business.Inventory;
using MengYou.Core.Business.Recovery;
using iFramework;

await using var sessions = new SessionManager();
var session = sessions.Create(new SessionData
{
    WindowHandle = gameWindowHandle,
    ProcessId = gameProcessId,
    DisplayName = "主号",
    InputMode = InputMode.Background,
    Automation = new AutomationOptions
    {
        PollInterval = TimeSpan.FromSeconds(1),
        AutoHeal = new AutoHealRule
        {
            TriggerHealthRatio = 0.45,
            Item = ItemTarget.BySlot(5),
            UseCount = 1,
            RequiredConsecutiveLowReadings = 2,
            Cooldown = TimeSpan.FromSeconds(8),
        },
        AutoUseItems = new[]
        {
            new AutoUseItemRule
            {
                Name = "每十分钟使用增益物品",
                Item = ItemTarget.BySlot(8),
                InitialDelay = TimeSpan.FromMinutes(10),
                Interval = TimeSpan.FromMinutes(10),
                FailureRetryInterval = TimeSpan.FromSeconds(15),
            },
        },
    },
});

session.ActionCompleted += (_, result) =>
    Console.WriteLine($"{result.CompletedAt:t} {result.Name}: {result.Message}");

await session.StartAsync();

var opened = await session.OpenUiAsync("道具行囊");
var used = await session.UseItemAsync(ItemTarget.BySlot(3));
var closed = await session.CloseUiAsync("道具行囊");

// 应用退出或解绑窗口时：
await sessions.RemoveAsync(session.SessionId);
```

固定格子不依赖物品名识别，是当前最稳妥的生产配置。也可以使用
`ItemTarget.ByName("金创药")`，但必须先配置同名物品图标模板。

## 上线前标定

1. 用 DevTool 按游戏实际分辨率标定 `Game/Config/UILayout.json` 中的
   `HpNumber`、`MaxHpNumber`、背包区域和物品格子。
2. 从游戏相同字体各截取一个数字，保存为
   `Game/Config/Templates/Digits/0.png` 到 `9.png`。自动加血读取不到有效
   `MaxHp` 时会停止用药，并在 `AutomationStatus.LastError` 给出原因。
3. 保存 UI 标题模板，例如
   `Game/Config/Templates/UI.道具行囊.标题.png`。UI 快捷键是 toggle；没有状态模板时
   系统会拒绝操作，避免误把已打开的面板关闭。
4. 按名称使用物品时保存
   `Game/Config/Templates/ItemIcon.物品名.png`。固定格子方式不需要这一步。
5. 后台消息被游戏过滤时，将 `InputMode` 改为 `Foreground`；需要驱动模式时改为
   `Driver` 并先通过 DevTool 完成驱动检查。

内置 UI 快捷键是“道具行囊 = Alt+E、人物状态 = Alt+W”。其他 UI 可通过
`SessionData.UiHotkeys` 增加快捷键映射，同时必须在 `ImageGameReader` 注册对应状态检测器。

## 运行语义

- 自动加血优先于同一周期到期的普通物品，每周期最多执行一次物品动作。
- 手动 UI/物品命令不会等待自动任务；如果业务上需要顺序，由调用方按顺序 `await`。
- 默认连续两次识别为低血量才会加血，用于过滤单帧 OCR 抖动。
- 加血按“尝试时间”冷却，即使该次使用失败也不会高频连点。
- 定时物品成功后按 `Interval` 调度，失败后按 `FailureRetryInterval` 重试。
- `StopAsync` 会取消轮询、等待在途动作退出并进入不可恢复的 `Stopped` 状态。
- `AutomationStatus` 可直接用于状态栏；每个真实动作也会发布 `ActionCompleted`。

验证命令：

```powershell
dotnet build Core.sln
dotnet run --project Tests/Core.RuntimeTests.csproj --no-build
```
