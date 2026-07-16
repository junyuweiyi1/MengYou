namespace iFramework;


public interface IUIMgrProvider
{
    Task<bool> IsUIShown(string uiName);

    Task ShowUI(string uiName);

    Task CloseUI(string uiName);
}