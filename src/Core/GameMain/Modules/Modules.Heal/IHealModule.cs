namespace MengYou.Abstractions.Modules;

/// <summary>自动加血模块契约。</summary>
public interface IHealModule : IModule
{
    /// <summary>动态更新加血策略（低血量阈值等）。</summary>
    void Configure(double selfHpThreshold, double selfMpThreshold);
}
