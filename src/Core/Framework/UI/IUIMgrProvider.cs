namespace iFramework;


public interface IUIMgrProvider
{
    bool IsUIShown(string uiName);

    void ShowUI(string uiName);

    void CloseUI(string uiName);
}