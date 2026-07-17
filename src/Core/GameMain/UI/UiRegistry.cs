using System.Collections.ObjectModel;

namespace MengYou.UI;

/// <summary>会话级 UI 模块注册表。</summary>
public sealed class UiRegistry : IUiRegistry
{
    private readonly Dictionary<UiId, IUiStateDetector> _detectors = new();
    private readonly Dictionary<UiId, IUiController> _controllers = new();
    private readonly object _sync = new();

    public IReadOnlyCollection<UiId> RegisteredIds
    {
        get
        {
            lock (_sync)
            {
                return _detectors.Keys
                    .Concat(_controllers.Keys)
                    .Distinct()
                    .ToArray();
            }
        }
    }

    public void RegisterDetector(IUiStateDetector detector)
    {
        ArgumentNullException.ThrowIfNull(detector);
        lock (_sync)
        {
            if (!_detectors.TryAdd(detector.UiId, detector))
                throw new InvalidOperationException($"UI 检测器重复注册：{detector.UiId}");
        }
    }

    public void RegisterController(IUiController controller)
    {
        ArgumentNullException.ThrowIfNull(controller);
        lock (_sync)
        {
            if (!_controllers.TryAdd(controller.UiId, controller))
                throw new InvalidOperationException($"UI 控制器重复注册：{controller.UiId}");
        }
    }

    public async Task<UiObservation> ObserveAsync(UiId uiId, CancellationToken ct = default)
    {
        IUiStateDetector? detector;
        lock (_sync)
            _detectors.TryGetValue(uiId, out detector);

        if (detector == null)
            return UiObservation.Unknown(uiId, "未注册 UI 检测器。");

        try
        {
            return await detector.ObserveAsync(ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return UiObservation.Unknown(uiId, $"检测异常：{ex.Message}");
        }
    }

    public async Task<IReadOnlyDictionary<UiId, UiObservation>> ObserveAllAsync(
        CancellationToken ct = default)
    {
        IUiStateDetector[] detectors;
        lock (_sync)
            detectors = _detectors.Values.ToArray();

        var result = new Dictionary<UiId, UiObservation>();
        foreach (var detector in detectors)
        {
            ct.ThrowIfCancellationRequested();
            result[detector.UiId] = await ObserveAsync(detector.UiId, ct).ConfigureAwait(false);
        }

        return new ReadOnlyDictionary<UiId, UiObservation>(result);
    }

    public Task OpenAsync(UiId uiId, CancellationToken ct = default)
        => GetController(uiId).OpenAsync(ct);

    public Task CloseAsync(UiId uiId, CancellationToken ct = default)
        => GetController(uiId).CloseAsync(ct);

    private IUiController GetController(UiId uiId)
    {
        lock (_sync)
        {
            if (_controllers.TryGetValue(uiId, out var controller))
                return controller;
        }

        throw new NotSupportedException($"UI 未注册打开/关闭控制器：{uiId}");
    }
}
