namespace MengYou.Runtime;

/// <summary>会话后台感知循环。</summary>
public interface IGameObservationLoop : IDisposable, IAsyncDisposable
{
    bool IsRunning { get; }

    Task StartAsync(CancellationToken ct = default);

    Task StopAsync(CancellationToken ct = default);
}
