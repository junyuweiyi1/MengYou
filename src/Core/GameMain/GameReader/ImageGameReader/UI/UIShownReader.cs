
using iFramework;

public class UIShownReader
{
    private readonly IVisionServiceMgr _vision;
    private readonly IUIElementLocateMgr _locator;

    protected bool CanReadText => _vision.CanReadText;

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
    {
        return Task.FromResult(_vision.FindTemplate(templateKey, threshold) != null);
    }
}
