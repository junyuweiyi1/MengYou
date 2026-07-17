using iFramework;

public class PlayerStatePannelUIShownReader : UIShownReader
{
    public PlayerStatePannelUIShownReader(IVisionServiceMgr vision, IUIElementLocateMgr locator)
        : base(vision, locator)
    {
    }

    public async Task<bool> IsUIShown()
    {
        return await RegionText("人物状态.标题");
    }
}