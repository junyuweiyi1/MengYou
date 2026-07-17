
using iFramework;

public class HumanUIMgrProvider: IUIMgrProvider
{
    private Game _game;

    public HumanUIMgrProvider(Game game)
    {
        _game = game;
    }

    public async Task<bool> IsUIShown(string uiName, CancellationToken ct = default)
    {
        return await _game.GameReader.IsUIShown(uiName, ct);
    }

    public async Task<bool> ShowUI(string uiName, CancellationToken ct = default)
    {
        _game.VisionServiceMgr.Refresh();
        if (await IsUIShown(uiName, ct))
            return true;

        for (var i = 0; i < 5; i++)
        {
            await _game.GameControl.ShowUI(uiName);

            for (var j = 0; j < 5; j++)
            {
                await Task.Delay(100, ct);
                _game.VisionServiceMgr.Refresh();
                if (await IsUIShown(uiName))
                    return true;
            }
        }
        return false;
    }

    public async Task<bool> CloseUI(string uiName, CancellationToken ct = default)
    {
        _game.VisionServiceMgr.Refresh();
        if (!await IsUIShown(uiName, ct))
            return true;

        for (var i = 0; i < 5; i++)
        {
            await _game.GameControl.CloseUI(uiName);

            for (var j = 0; j < 5; j++)
            {
                await Task.Delay(100, ct);
                _game.VisionServiceMgr.Refresh();
                if (!await IsUIShown(uiName))
                    return true;
            }
        }
        return false;
    }
}