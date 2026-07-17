using iFramework;

public class Logic
{
    public IGameReader GameReader { get; private set; } = null!;
    public IGameControl GameControl { get; private set; } = null!;

    public BagMgr BagMgr { get; private set; } = null!;
    public 师门 MasterTasks { get; private set; } = null!;

    public void Initialize(
        Game game,
        IReadOnlyDictionary<string, KeyCode[]>? uiHotkeys = null)
    {
        GameReader = new ImageGameReader(game);
        GameControl = new PlayerGameControl(game, uiHotkeys);

        game.FW.UIMgr.SetProvider(new HumanUIMgrProvider(game));

        BagMgr = new BagMgr(game);
        MasterTasks = new 师门(game);
    }

    public void Dispose()
    {
        MasterTasks?.Dispose();
    }
}
