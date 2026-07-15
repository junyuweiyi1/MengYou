# AGENTS.md — 梦游 (MengYou)

> AI 交互与代码注释一律使用**简体中文**。

## 1. 项目概览

梦游是基于 .NET 8 的桌面游戏辅助框架，目标为《梦幻西游》。仓库分层：

- `src/Core/` — 核心库（`Core.csproj`, `net8.0`），主解决方案 `Core.sln`
  - `GameMain/` — 领域模型、`IGameReader`、`Sessions`、`Game` 业务对象
  - `Framework/` — 基础设施：`VisionService`（图像识别/OCR）、`Input`（模拟输入 + 装饰器）、`UIElementLocator`、`Events`（会话事件总线）
  - `Platform.Win32/` — Win32 P/Invoke：`WindowCapture`、`WindowEnumerator`、`WinMessages`
  - `DevTool/` — WPF 标定工具（`net8.0-windows`, `WinExe`, `UseWPF`, `UseWindowsForms`），用于生成 `UILayout.json` 与模板 PNG
  - `Log/Logger.cs` — 极简日志封装
- `src/App/` — 未接入 sln 的 WPF 宿主壳（`App.xaml` / `MainWindow.xaml`）

## 2. 构建 / 运行 / 测试

工作目录以 `src/Core/` 为主。所有命令使用 `dotnet` CLI。

```powershell
# 还原依赖
dotnet restore src/Core/Core.sln

# 构建整个解决方案
dotnet build src/Core/Core.sln -c Debug

# 只构建单个项目
dotnet build src/Core/Core.csproj
dotnet build src/Core/DevTool/MengYou.DevTool.csproj

# 运行 DevTool 标定工具（WPF）
dotnet run --project src/Core/DevTool/MengYou.DevTool.csproj

# 发布 Release
dotnet build src/Core/Core.sln -c Release
```

**测试**：仓库当前**没有测试项目**。若新增，遵循：
- 命名 `MengYou.<Module>.Tests`，使用 xUnit
- 运行全部：`dotnet test src/Core/Core.sln`
- 运行单个测试：`dotnet test --filter "FullyQualifiedName~<类型.方法名>"` 或 `--filter "Name=<方法名>"`
- 只跑单文件：`dotnet test --filter "FullyQualifiedName~<命名空间.类型>"`

**Lint / 格式化**：使用内置 `dotnet format`：

```powershell
dotnet format src/Core/Core.sln          # 修复格式
dotnet format --verify-no-changes         # CI 校验
```

## 3. 语言与项目约定

- **目标框架**：`net8.0`（`DevTool` 为 `net8.0-windows`）
- **`ImplicitUsings=enable`**：`System`、`System.Linq`、`System.Threading` 等已隐式导入，**不要重复 `using`**
- **`Nullable=enable`**：全项目启用可空引用类型；接收外部输入的引用类型必须显式标注 `?`，返回 null 的方法签名亦然
- 编码：UTF-8（带或不带 BOM 均可，现有部分文件带 `\uFEFF`，保持一致即可）

## 4. 代码风格

### 4.1 命名
- **类型 / 方法 / 属性 / 枚举成员**：`PascalCase`（`SessionManager`、`GetPlayerState`）
- **私有字段**：`_camelCase`（`_logger`、`_sessions`、`_foregroundLock`）
- **参数 / 局部变量**：`camelCase`
- **接口**：`I` 前缀（`IGameReader`、`IVisionService`、`IUIElementLocator`）
- **抽象/装饰器**：装饰器以功能名 + `Controller`/`Reader` 结尾（`HumanizedController`）
- **异步方法**：一律 `XxxAsync` 后缀，返回 `Task` / `Task<T>`，接收 `CancellationToken ct = default`
- **文件名**：与主类型同名；一个文件通常一个 public 类型（`Point2D.cs` 中的 `Rect` 属于紧密相关例外）

### 4.2 命名空间与 using
- 命名空间前缀 `MengYou.*`，按模块划分：`MengYou.Abstractions`、`MengYou.Abstractions.Models`、`MengYou.Recognition.Image`、`MengYou.Platform.Win32`、`MengYou.Core.Events`、`MengYou.Input.Simulated.Decorators`
- **优先使用文件作用域命名空间**（`namespace X;`），仅在遗留文件中保留块级
- `using` 顺序：`System.*` → 第三方 → `MengYou.*`；每组内字母序；不需要的 using 删除

### 4.3 修饰符与语言特性
- 类默认加 `sealed`（如无继承需求），实现类几乎都是 `sealed`
- 字段一律 `readonly`（除状态字段如 `_disposed`、`State`）
- 优先 `record` / `init`-only 属性表示不可变数据模型（见 `PlayerState`）
- 使用主构造器（primary constructor）声明简单值类型（见 `Point2D(int X, int Y)`）
- 表达式体成员（`=>`）用于单行方法/属性
- 使用 `is { } x` 模式匹配处理可空返回值：`_locator.LocateRegion("HpNumber") is { } hr ? _vision.ReadNumber(hr) ?? 0 : 0`
- 集合优先 `IReadOnlyList<T>` / `IReadOnlyCollection<T>` 作对外返回类型

### 4.4 XML 文档注释
- **所有 public 类型与成员必须写 `/// <summary>` 中文注释**，仓库现状统一如此
- 实现继承接口时使用 `/// <inheritdoc/>`
- 参数说明使用 `/// <param>`；返回值用 `/// <returns>`

### 4.5 异步与并发
- I/O、耗时操作必须 `async`；不要写 `.Result` / `.Wait()`
- `CancellationToken` 顺沿 API 传递到底
- 共享可变集合使用 `ConcurrentDictionary`；订阅表等复合操作用 `lock (_sync)`（见 `SessionEventBus`）
- 前台输入通道用 `SemaphoreSlim(1,1)` 互斥（见 `SessionManager.AcquireForegroundLockAsync`）
- 释放资源用 `IDisposable`；返回释放句柄的方法用嵌套 `sealed class Releaser`/`Subscription` 模式

## 5. 架构原则

- **依赖抽象，不依赖实现**：业务只依赖 `IGameReader` / `IGameController` / `IVisionService` / `IUIElementLocator`。同一接口存在图像识别版与内存版两种实现，运行期由 DI 注入
- **会话隔离**：每个游戏窗口对应一个 `IGameSession`（拥有独立 DI Scope、独立取消令牌），多开互不影响
- **装饰器组合优于继承**：附加行为（如人性化延迟）通过包装 `IGameController` 实现，见 `HumanizedController`
- **事件驱动**：跨模块解耦通过 `ISessionEventBus`（`Publish<TEvent>` / `Subscribe<TEvent>`，事件继承 `GameEvent`）

## 6. 错误处理

- 参数/前置条件失败：抛 `InvalidOperationException` / `ArgumentException`（中文消息，见 `WindowCapture`）
- **绝不吞掉未知异常**；只有确定不影响主流程的场景才 `try/catch`（如事件分发中单个订阅者失败：`catch { /* 单个订阅者异常不影响其他 */ }`）
- 可空返回优于抛异常：读不到内容返回 `null`（如 `ReadText`、`GetActiveDialog`），调用方用模式匹配处理
- P/Invoke 后立即检查返回值，失败时走回落逻辑（见 `PrintWindow` → `BitBlt`）
- 资源获取失败必须清理已获取部分（`try/finally` 释放 HDC 等）

## 7. 平台注意

- Win32 P/Invoke 集中在 `Platform.Win32/`，勿在业务层直接 `[DllImport]`
- 截图坐标使用**客户端坐标系**（不含边框），依赖固定分辨率（见 DevTool README）
- `System.Drawing.Common` 仅在 Windows 可用；跨平台代码不得引用

## 8. 其它

- 已检查：仓库当前无 `.cursor/rules/`、无 `.cursorrules`、无 `.github/copilot-instructions.md`，无既有 `AGENTS.md`
- 提交前：`dotnet build` 通过 + `dotnet format --verify-no-changes` 通过
- 请勿提交 `bin/`、`obj/`、`.vs/` 目录
