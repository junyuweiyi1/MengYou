namespace MengYou.UI;

/// <summary>单个面板的状态检测器。</summary>
public interface IUiStateDetector
{
    UiId UiId { get; }

    Task<UiObservation> ObserveAsync(CancellationToken ct = default);
}

/// <summary>单个面板的底层打开/关闭控制器，不负责动作排队。</summary>
public interface IUiController
{
    UiId UiId { get; }

    Task OpenAsync(CancellationToken ct = default);

    Task CloseAsync(CancellationToken ct = default);
}

/// <summary>UI 模块注册表。新增面板通过注册扩展，无需修改中心 switch。</summary>
public interface IUiRegistry
{
    IReadOnlyCollection<UiId> RegisteredIds { get; }

    void RegisterDetector(IUiStateDetector detector);

    void RegisterController(IUiController controller);

    Task<UiObservation> ObserveAsync(UiId uiId, CancellationToken ct = default);

    Task<IReadOnlyDictionary<UiId, UiObservation>> ObserveAllAsync(CancellationToken ct = default);

    Task OpenAsync(UiId uiId, CancellationToken ct = default);

    Task CloseAsync(UiId uiId, CancellationToken ct = default);
}
