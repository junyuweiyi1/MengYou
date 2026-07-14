using MengYou.Abstractions.Models;

namespace MengYou.Abstractions.Modules;

/// <summary>寻路目标：目标地图 + 目标坐标。</summary>
public sealed class PathTarget
{
    /// <summary>目标地图 ID。</summary>
    public string TargetMapId { get; init; } = string.Empty;

    /// <summary>目标坐标（该地图内）。</summary>
    public Point2D TargetPosition { get; init; }
}

/// <summary>寻路模块契约。</summary>
public interface IPathModule : IModule
{
    /// <summary>启动一次寻路任务；完成或失败时返回。</summary>
    Task<bool> NavigateAsync(PathTarget target, CancellationToken ct = default);
}
