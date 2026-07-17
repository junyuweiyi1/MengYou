
public class BagPannelUIShownReader : UIShownReader
{
    public BagPannelUIShownReader(IVisionServiceMgr vision, IUIElementLocateMgr locator)
        : base(vision, locator)
    {
    }

    public bool IsConfigured => HasTemplate("UI.道具行囊.标题") || HasRegion("道具行囊.标题");

    public async Task<bool> IsUIShown()
    {
        return await TemplateMatch("UI.道具行囊.标题")
            || await RegionText("道具行囊.标题");
    }
}
