
public class PlayerStatePannelUIShownReader : UIShownReader
{
    public PlayerStatePannelUIShownReader(IVisionServiceMgr vision, IUIElementLocateMgr locator)
        : base(vision, locator)
    {
    }

    public bool IsConfigured => HasTemplate("UI.人物状态.标题") || HasRegion("人物状态.标题");

    public async Task<bool> IsUIShown()
    {
        return await TemplateMatch("UI.人物状态.标题")
            || await RegionText("人物状态.标题");
    }
}
