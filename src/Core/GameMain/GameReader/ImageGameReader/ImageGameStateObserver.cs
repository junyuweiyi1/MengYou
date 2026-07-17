using MengYou.UI;
using MengYou.Runtime;

/// <summary>图像后端的会话状态观察器。</summary>
public sealed class ImageGameStateObserver : IGameStateObserver
{
    private readonly IGameReader _reader;
    private readonly IUiRegistry _uiRegistry;

    public ImageGameStateObserver(IGameReader reader, IUiRegistry uiRegistry)
    {
        _reader = reader;
        _uiRegistry = uiRegistry;
    }

    public async Task<GameStateSnapshot> ObserveAsync(CancellationToken ct = default)
    {
        var user = await _reader.GetUserSnapshot(ct).ConfigureAwait(false);
        var uiStates = await _uiRegistry.ObserveAllAsync(ct).ConfigureAwait(false);
        return new GameStateSnapshot
        {
            CapturedAt = DateTimeOffset.UtcNow,
            Health = ObservationHealth.Healthy,
            User = user,
            UiStates = uiStates,
        };
    }
}
