namespace MengYou.Runtime;

/// <summary>动作执行期间需要独占的资源。</summary>
[Flags]
public enum GameActionResources
{
    None = 0,
    Input = 1,
    Ui = 2,
    Foreground = 4,
}

/// <summary>可由会话动作执行器调度的最小操作单元。</summary>
public interface IGameAction
{
    string Name { get; }

    GameActionResources RequiredResources { get; }

    Task<GameActionResult> ExecuteAsync(CancellationToken ct = default);
}

/// <summary>通过委托快速定义动作，供面板控制和业务模块适配现有实现。</summary>
public sealed class DelegateGameAction : IGameAction
{
    private readonly Func<CancellationToken, Task<GameActionResult>> _execute;

    public DelegateGameAction(
        string name,
        GameActionResources requiredResources,
        Func<CancellationToken, Task<GameActionResult>> execute)
    {
        Name = string.IsNullOrWhiteSpace(name)
            ? throw new ArgumentException("动作名不能为空。", nameof(name))
            : name;
        RequiredResources = requiredResources;
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
    }

    public string Name { get; }

    public GameActionResources RequiredResources { get; }

    public Task<GameActionResult> ExecuteAsync(CancellationToken ct = default) => _execute(ct);
}
