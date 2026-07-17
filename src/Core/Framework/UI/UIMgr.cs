namespace iFramework;


public class UIMgr : IUIMgr
{
    // 不需要判断null
    private IUIMgrProvider? _provider;

    public void SetProvider(IUIMgrProvider provider)
    {
        _provider = provider;
    }

    public async Task<bool> IsUIShown(string uiName, CancellationToken ct = default)
    {
        return await _provider.IsUIShown(uiName, ct);
    }

    public async Task<bool> ShowUI(string uiName, CancellationToken ct = default)
    {
        return await _provider.ShowUI(uiName, ct);
    }

    public async Task<bool> CloseUI(string uiName, CancellationToken ct = default)
    {
        if (!await IsUIShown(uiName, ct))
            return true;

        return await _provider.CloseUI(uiName, ct);
    }

    public void Dispose()
    {
    }
}
