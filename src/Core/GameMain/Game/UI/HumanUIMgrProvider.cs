
using iFramework;

public class HumanUIMgrProvider: IUIMgrProvider
{
    private Game _game;

    public HumanUIMgrProvider(Game game)
    {
        _game = game;
    }

    public async Task<bool> IsUIShown(string uiName)
    {
        return await _game.GameReader.IsUIShown(uiName);
    }

    public async Task ShowUI(string uiName)
    {
        await _game.GameControl.ShowUI(uiName);
    }

    public async Task CloseUI(string uiName)
    {
        await _game.GameControl.CloseUI(uiName);
    }
}