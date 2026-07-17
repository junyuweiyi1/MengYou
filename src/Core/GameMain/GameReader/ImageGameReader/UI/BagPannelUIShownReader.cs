using iFramework;

public class BagPannelUIShownReader : UIShownReader, MengYou.UI.IUiStateDetector
{
    public BagPannelUIShownReader(IVisionServiceMgr vision, IUIElementLocateMgr locator)
        :base(vision,locator)
    {
    }

    public MengYou.UI.UiId UiId => MengYou.UI.GameUiIds.Bag;

    public async Task<bool> IsUIShown()
    {
        return await RegionText("道具行囊.标题");
    }

    public async Task<MengYou.UI.UiObservation> ObserveAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        if (!CanReadText)
            return MengYou.UI.UiObservation.Unknown(UiId, "当前 OCR 引擎不可用。");

        var visible = await IsUIShown().ConfigureAwait(false);
        return new MengYou.UI.UiObservation
        {
            UiId = UiId,
            Visibility = visible
                ? MengYou.UI.UiVisibility.Visible
                : MengYou.UI.UiVisibility.Hidden,
            Confidence = visible ? 0.9 : 0.6,
            ObservedAt = DateTimeOffset.UtcNow,
            Evidence = "region:道具行囊.标题",
        };
    }
}
