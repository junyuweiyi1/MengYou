namespace MengYou.Abstractions.Modules;

/// <summary>
/// 所有业务模块的统一契约：可启停 + 有状态。
/// </summary>
public interface IModule
{
    /// <summary>模块名（用于日志/UI）。</summary>
    string Name { get; }

    /// <summary>是否正在运行。</summary>
    bool IsRunning { get; }

    /// <summary>启动模块。</summary>
    Task StartAsync(CancellationToken ct = default);

    /// <summary>停止模块。</summary>
    Task StopAsync(CancellationToken ct = default);
}
