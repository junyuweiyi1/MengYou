public sealed class 师门 : IDisposable
{
    private const int SpaceVirtualKey = 0x20;
    private readonly Game _game;
    private readonly System.Threading.Timer _hotkeyTimer;
    private int _checking;

    public 师门(Game game)
    {
        _game = game ?? throw new ArgumentNullException(nameof(game));
        _hotkeyTimer = new System.Threading.Timer(OnHotkeyPoll, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(30));
    }

    private void OnHotkeyPoll(object? state)
    {
        if ((User32.GetAsyncKeyState(SpaceVirtualKey) & 1) == 0) return;
        if (Interlocked.Exchange(ref _checking, 1) != 0) return;

        _ = CheckDo();
    }

    private async Task CheckDo()
    {
        try
        {
            if (CheckGetNewTask())
            {
                await DoGetNewTaskAsync();
            }
        }
        catch(Exception e)
        {

        }
        finally
        {
            Volatile.Write(ref _checking, 0);
        }
    }

    private bool CheckGetNewTask()
    {
        var text = _game.FW.VisionServiceMgr.ReadText(RegionConst.kMainUITask);
        return text?.Contains("有什么吩咐", StringComparison.Ordinal) == true
            || text?.Contains("吩咐", StringComparison.Ordinal) == true
            || text?.Contains("吩", StringComparison.Ordinal) == true
            || text?.Contains("咐", StringComparison.Ordinal) == true;
    }

    private async Task DoGetNewTaskAsync()
    {
        var hit = _game.FW.VisionServiceMgr.FindText("师父", RegionConst.kMainUITask);
        if (hit is { } point)
        {
            await _game.FW.InputMgr.ClickAsync(point + RegionConst.kMainUITaskMouseOffset);
        }

        if (_game.FW.VisionServiceMgr.FindImage("师门-镇元大仙1") != null || _game.FW.VisionServiceMgr.FindImage("师门-镇元大仙2") != null)
        {
            await _game.FW.InputMgr.ClickAsync(PointConst.师门弹窗_任务按钮);
        }
    }

    public void Dispose()
    {
        _hotkeyTimer.Dispose();
    }
}
