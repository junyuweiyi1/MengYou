namespace iFramework;


public class UIMgr : IUIMgr
{
    private IUIMgrProvider _provider;

    public void SetProvider(IUIMgrProvider provider)
    {
        _provider = provider;
    }

    public async bool IsUIShown(string uiName)
    {
        return _provider?.IsUIShown(uiName);
    }

    public async void ShowUI(string uiName)
    {
        if (await IsUIShown(uiName))
            return;

        _provider?.ShowUI(uiName);
    }

    public async void CloseUI(string uiName)
    {
        if (!await IsUIShown(uiName))
            return;

        _provider?.CloseUI(uiName);
    }
}