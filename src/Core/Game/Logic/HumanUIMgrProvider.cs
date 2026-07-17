

public class HumanUIMgrProvider : IUIMgrProvider
{
    private Game _game;

    public HumanUIMgrProvider(Game game)
    {
        _game = game;
    }

    public async Task<bool> IsUIShown(string uiName, CancellationToken ct = default)
    {
        return await _game.Logic.GameReader.IsUIShown(uiName, ct);
    }

    public async Task<bool> ShowUI(string uiName, CancellationToken ct = default)
    {
        _game.FW.VisionServiceMgr.Refresh();
        if (await IsUIShown(uiName, ct))
            return true;

        // 面板快捷键是 toggle，只允许发送一次；重复发送会在检测稍慢时把刚打开的面板再次关闭。
        await _game.Logic.GameControl.ShowUI(uiName, ct);
        for (var i = 0; i < 10; i++)
        {
            await Task.Delay(100, ct);
            _game.FW.VisionServiceMgr.Refresh();
            if (await IsUIShown(uiName, ct)) return true;
        }
        return false;
    }

    public async Task<bool> CloseUI(string uiName, CancellationToken ct = default)
    {
        _game.FW.VisionServiceMgr.Refresh();
        if (!await IsUIShown(uiName, ct))
            return true;

        await _game.Logic.GameControl.CloseUI(uiName, ct);
        for (var i = 0; i < 10; i++)
        {
            await Task.Delay(100, ct);
            _game.FW.VisionServiceMgr.Refresh();
            if (!await IsUIShown(uiName, ct)) return true;
        }
        return false;
    }
}
