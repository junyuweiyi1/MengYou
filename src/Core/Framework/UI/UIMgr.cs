namespace iFramework;


public class UIMgr : IUIMgr
{
    private IUIMgrProvider _provider;

    public void SetProvider(IUIMgrProvider provider)
    {
        _provider = provider;
    }

    public async Task<bool> IsUIShown(string uiName)
    {
        return await _provider?.IsUIShown(uiName);
    }

    public async Task ShowUI(string uiName)
    {
        if (await IsUIShown(uiName))
            return;

        await _provider?.ShowUI(uiName);
    }

    public async Task CloseUI(string uiName)
    {
        if (!await IsUIShown(uiName))
            return;

        await _provider?.CloseUI(uiName);
    }
}