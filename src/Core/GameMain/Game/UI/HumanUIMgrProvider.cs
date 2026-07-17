
using iFramework;
using MengYou.UI;

public class HumanUIMgrProvider: IUIMgrProvider
{
    private readonly IUiRegistry _uiRegistry;
    private readonly IGameControl _control;
    private readonly IVisionServiceMgr _vision;

    public HumanUIMgrProvider(
        IUiRegistry uiRegistry,
        IGameControl control,
        IVisionServiceMgr vision)
    {
        _uiRegistry = uiRegistry;
        _control = control;
        _vision = vision;
    }

    public async Task<bool> IsUIShown(string uiName, CancellationToken ct = default)
    {
        var observation = await ObserveAsync(uiName, ct).ConfigureAwait(false);
        return observation.Visibility == UiVisibility.Visible;
    }

    public async Task<bool> ShowUI(string uiName, CancellationToken ct = default)
    {
        _vision.Refresh();
        var initial = await ObserveAsync(uiName, ct).ConfigureAwait(false);
        if (initial.Visibility == UiVisibility.Visible)
            return true;
        if (initial.Visibility == UiVisibility.Unknown)
            return false;

        for (var i = 0; i < 5; i++)
        {
            await _control.ShowUI(uiName, ct);

            for (var j = 0; j < 5; j++)
            {
                await Task.Delay(100, ct);
                _vision.Refresh();
                var observation = await ObserveAsync(uiName, ct).ConfigureAwait(false);
                if (observation.Visibility == UiVisibility.Visible)
                    return true;
            }
        }
        return false;
    }

    public async Task<bool> CloseUI(string uiName, CancellationToken ct = default)
    {
        _vision.Refresh();
        var initial = await ObserveAsync(uiName, ct).ConfigureAwait(false);
        if (initial.Visibility == UiVisibility.Hidden)
            return true;
        if (initial.Visibility == UiVisibility.Unknown)
            return false;

        for (var i = 0; i < 5; i++)
        {
            await _control.CloseUI(uiName, ct);

            for (var j = 0; j < 5; j++)
            {
                await Task.Delay(100, ct);
                _vision.Refresh();
                var observation = await ObserveAsync(uiName, ct).ConfigureAwait(false);
                if (observation.Visibility == UiVisibility.Hidden)
                    return true;
            }
        }
        return false;
    }

    private Task<UiObservation> ObserveAsync(string uiName, CancellationToken ct)
        => _uiRegistry.ObserveAsync(GameUiIds.FromName(uiName), ct);
}
