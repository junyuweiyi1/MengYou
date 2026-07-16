using iFramework;

public class BagPannelUIShownReader : UIShownReader
{
    public BagPannelUIShownReader(IVisionServiceMgr vision, IUIElementLocateMgr locator)
        :base(vision,locator)
    {
    }

    public async Task<bool> IsUIShown()
    {
        return await RegionText("道具行囊.标题");
    }
}