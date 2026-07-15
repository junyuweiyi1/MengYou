namespace iFramework;


public interface IUIMgr
{
    void SetProvider(IUIMgrProvider provider);

    bool IsUIShown(string uiName);

    void ShowUI(string uiName);

    void CloseUI(string uiName);
}