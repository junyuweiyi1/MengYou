namespace MengYou.Runtime;

/// <summary>会话级动作执行器：保证单个会话内只有一个动作写入游戏。</summary>
public interface IGameActionExecutor : IDisposable, IAsyncDisposable
{
    bool IsRunning { get; }

    Task StartAsync(CancellationToken ct = default);

    Task<GameActionResult> ExecuteAsync(IGameAction action, CancellationToken ct = default);

    Task StopAsync(CancellationToken ct = default);
}
