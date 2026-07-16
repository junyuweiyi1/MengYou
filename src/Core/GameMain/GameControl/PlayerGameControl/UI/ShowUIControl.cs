
using iFramework;
using System.Globalization;
using static User32;

public class ShowUIControl
{
    private readonly IInputMgr _input;

    public ShowUIControl(IInputMgr input)
    {
        _input = input;
    }

    /// <summary>发送一个或多个键（组合键），例如 HotKey(ct, KeyCode.Alt, KeyCode.E) 表示 Alt+E。</summary>
    public async Task HotKey(CancellationToken ct, params KeyCode[] keys)
    {
        await _input.SendKeyAsync(ct, keys);
    }
}