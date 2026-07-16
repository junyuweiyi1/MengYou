namespace iFramework;


public interface IUIMgrProvider
{
    Task<bool> IsUIShown(string uiName, CancellationToken ct = default);

    Task<bool> ShowUI(string uiName, CancellationToken ct = default);

    Task<bool> CloseUI(string uiName, CancellationToken ct = default);
}