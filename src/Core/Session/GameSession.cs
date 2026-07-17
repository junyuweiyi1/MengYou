/// <summary>
/// 单游戏窗口会话。负责底层 Game、自动化循环、手动命令和停止清理的完整生命周期。
/// </summary>
public sealed class GameSession : IGameSession
{

    public GameSession(SessionData data)
    {
        SessionId = Guid.NewGuid();
        WindowHandle = data.WindowHandle;
        ProcessId = data.ProcessId;
        DisplayName = data.DisplayName;

        Game = new Game(WindowHandle, data.InputMode, data.UiHotkeys);
    }

    public Guid SessionId { get; }

    public IntPtr WindowHandle { get; }

    public int ProcessId { get; }

    public string DisplayName { get; }

    public SessionState State { get; private set; } = SessionState.Idle;

    public Game Game { get; }


    public async Task StartAsync(CancellationToken ct = default)
    {
      
    }

    public async Task StopAsync(CancellationToken ct = default)
    {        
    }
  
    public void Dispose()
    {
    }
}
