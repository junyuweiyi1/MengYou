namespace MengYou.Runtime;

/// <summary>会话级功能生命周期监督器。</summary>
public interface IGameFeatureSupervisor : IDisposable, IAsyncDisposable
{
    IReadOnlyCollection<GameFeatureStatus> Statuses { get; }

    Task<GameFeatureStatus> StartAsync(
        IGameFeature feature,
        CancellationToken ct = default);

    Task<GameFeatureStatus?> StopAsync(
        string featureId,
        CancellationToken ct = default);

    Task StopAllAsync(CancellationToken ct = default);
}
