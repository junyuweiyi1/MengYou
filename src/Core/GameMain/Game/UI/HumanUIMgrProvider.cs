
using iFramework;

public class HumanUIMgrProvider: IUIMgrProvider
{
    private Game _game;

    public HumanUIMgrProvider(Game game)
    {
        _game = game;
    }

    public async bool IsUIShown(string uiName)
    {
        return await _game.GameReader.IsUIShown(uiName);
    }

    public async void ShowUI(string uiName)
    {
        await _game.GameControl.ShowUI(uiName);
    }

    public async void CloseUI(string uiName)
    {
        await _game.GameControl.CloseUI(uiName);
    }
}