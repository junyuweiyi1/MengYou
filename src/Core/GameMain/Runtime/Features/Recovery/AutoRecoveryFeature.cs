namespace MengYou.Runtime;

/// <summary>
/// 根据状态快照自动使用恢复物品。只消费健康的观测结果，并为生命、魔法分别限流。
/// </summary>
public sealed class AutoRecoveryFeature : IGameFeature
{
    public const string FeatureId = "auto-recovery";

    private readonly IRecoveryService _recoveryService;
    private readonly AutoRecoveryOptions _options;
    private DateTimeOffset _lastHealthAttempt = DateTimeOffset.MinValue;
    private DateTimeOffset _lastManaAttempt = DateTimeOffset.MinValue;
    private GameActionResult? _lastResult;

    public AutoRecoveryFeature(
        IRecoveryService recoveryService,
        AutoRecoveryOptions? options = null)
    {
        _recoveryService = recoveryService
            ?? throw new ArgumentNullException(nameof(recoveryService));
        _options = options ?? new AutoRecoveryOptions();
        _options.Validate();
    }

    public string Id => FeatureId;

    public string DisplayName => "自动恢复";

    /// <summary>最近一次恢复尝试的结构化结果，便于 UI 展示和诊断。</summary>
    public GameActionResult? LastResult => Volatile.Read(ref _lastResult);

    public async Task RunAsync(GameFeatureContext context, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        await foreach (var snapshot in context.StateStore
            .WatchAsync(-1, ct)
            .ConfigureAwait(false))
        {
            if (snapshot.Health != ObservationHealth.Healthy || snapshot.User == null)
                continue;

            var request = SelectRecovery(snapshot.User, DateTimeOffset.UtcNow);
            if (request == null)
                continue;

            MarkAttempt(request.Value.Resource, DateTimeOffset.UtcNow);
            var result = await _recoveryService
                .RecoverAsync(
                    request.Value.Resource,
                    request.Value.ItemName,
                    _options.UseCount,
                    ct)
                .ConfigureAwait(false);
            Volatile.Write(ref _lastResult, result);
        }
    }

    private RecoveryRequest? SelectRecovery(UserStateSnapshot user, DateTimeOffset now)
    {
        if (_options.EnableHealth
            && IsBelowThreshold(user.Hp, user.MaxHp, _options.HealthThreshold)
            && IsCooldownElapsed(_lastHealthAttempt, now))
        {
            return new RecoveryRequest(RecoveryResource.Health, _options.HealthItemName);
        }

        if (_options.EnableMana
            && IsBelowThreshold(user.Mp, user.MaxMp, _options.ManaThreshold)
            && IsCooldownElapsed(_lastManaAttempt, now))
        {
            return new RecoveryRequest(RecoveryResource.Mana, _options.ManaItemName);
        }

        return null;
    }

    private bool IsCooldownElapsed(DateTimeOffset previous, DateTimeOffset now)
        => now - previous >= _options.Cooldown;

    private void MarkAttempt(RecoveryResource resource, DateTimeOffset now)
    {
        if (resource == RecoveryResource.Health)
            _lastHealthAttempt = now;
        else
            _lastManaAttempt = now;
    }

    private static bool IsBelowThreshold(int value, int maximum, double threshold)
        => maximum > 0 && value >= 0 && (double)value / maximum <= threshold;

    private readonly record struct RecoveryRequest(
        RecoveryResource Resource,
        string ItemName);
}
