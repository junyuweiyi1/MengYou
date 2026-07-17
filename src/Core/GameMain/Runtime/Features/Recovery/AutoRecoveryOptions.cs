namespace MengYou.Runtime;

/// <summary>自动恢复策略。阈值采用 0 到 1 之间的角色资源比例。</summary>
public sealed record AutoRecoveryOptions
{
    public bool EnableHealth { get; init; } = true;

    public double HealthThreshold { get; init; } = 0.5;

    public string HealthItemName { get; init; } = "包子";

    public bool EnableMana { get; init; } = true;

    public double ManaThreshold { get; init; } = 0.3;

    public string ManaItemName { get; init; } = "佛手";

    public int UseCount { get; init; } = 1;

    /// <summary>同一种资源两次尝试之间的最短间隔，防止识别延迟导致连续使用。</summary>
    public TimeSpan Cooldown { get; init; } = TimeSpan.FromSeconds(3);

    internal void Validate()
    {
        ValidateThreshold(HealthThreshold, nameof(HealthThreshold));
        ValidateThreshold(ManaThreshold, nameof(ManaThreshold));
        if (EnableHealth && string.IsNullOrWhiteSpace(HealthItemName))
            throw new ArgumentException("启用生命恢复时必须配置物品名。", nameof(HealthItemName));
        if (EnableMana && string.IsNullOrWhiteSpace(ManaItemName))
            throw new ArgumentException("启用魔法恢复时必须配置物品名。", nameof(ManaItemName));
        if (UseCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(UseCount));
        if (Cooldown < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(Cooldown));
    }

    private static void ValidateThreshold(double threshold, string parameterName)
    {
        if (double.IsNaN(threshold) || threshold <= 0 || threshold > 1)
            throw new ArgumentOutOfRangeException(parameterName, "阈值必须大于 0 且不超过 1。");
    }
}
