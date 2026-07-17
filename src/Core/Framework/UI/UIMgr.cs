namespace iFramework;


public class UIMgr : IUIMgr
{
    private IUIMgrProvider? _provider;

    private IUIMgrProvider Provider => _provider
        ?? throw new InvalidOperationException("UI 管理器尚未配置 Provider。");

    public void SetProvider(IUIMgrProvider provider)
    {
        _provider = provider;
    }

    public async Task<bool> IsUIShown(string uiName, CancellationToken ct = default)
    {
        return await Provider.IsUIShown(uiName, ct);
    }

    public async Task<bool> ShowUI(string uiName, CancellationToken ct = default)
    {
        return await Provider.ShowUI(uiName, ct);
    }

    public async Task<bool> CloseUI(string uiName, CancellationToken ct = default)
    {
        return await Provider.CloseUI(uiName, ct);
    }

    public void Dispose()
    {
    }
}
