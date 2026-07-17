

public class UIShownReader
{
    private readonly IVisionServiceMgr _vision;
    private readonly IUIElementLocateMgr _locator;

    public UIShownReader(IVisionServiceMgr vision, IUIElementLocateMgr locator)
    {
        _vision = vision;
        _locator = locator;
    }

    public Task<bool> RegionText(string regionKey)
    {
        var region = _locator.LocateRegion(regionKey);
        if (region == null) return Task.FromResult(false);

        var text = _vision.ReadText(region.Value);
        return Task.FromResult(!string.IsNullOrWhiteSpace(text));
    }

    public Task<bool> TemplateMatch(string templateKey, double threshold = 0.85)
        => Task.FromResult(_vision.HasTemplate(templateKey)
            && _vision.FindImage(templateKey, threshold) != null);

    protected bool HasTemplate(string templateKey) => _vision.HasTemplate(templateKey);

    protected bool HasRegion(string regionKey) => _locator.LocateRegion(regionKey) is not null;
}
