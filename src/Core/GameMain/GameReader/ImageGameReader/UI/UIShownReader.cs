
using iFramework;

public class UIShownReader
{
    private readonly IVisionServiceMgr _vision;
    private readonly IUIElementLocateMgr _locator;

    public UIShownReader(IVisionServiceMgr vision, IUIElementLocateMgr locator)
    {
        _vision = vision;
        _locator = locator;
    }

    public async Task<bool> RegionText(string regionKey)
    {
        var region = _locator.LocateRegion(regionKey);
        if (region == null) return false;

        var text = _vision.ReadText(region.Value);
        return !string.IsNullOrWhiteSpace(text);
    }

    public async Task<bool> TemplateMatch(string templateKey, double threshold = 0.85)
    {
        return _vision.FindTemplate(templateKey, threshold) != null;
    }
}