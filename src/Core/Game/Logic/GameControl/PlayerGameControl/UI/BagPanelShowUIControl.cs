
/// <summary>
/// "道具行囊"面板的 显示/关闭 逻辑。
/// 暂时只支持通过快捷键（组合键）操作：Alt + E。
/// 游戏内该快捷键为开关（toggle），因此打开与关闭发送同一组合键。
/// </summary>
public class BagPanelShowUIControl : ShowUIControl
{
    public BagPanelShowUIControl(IInputMgr input)
        : base(input)
    {
    }

    public async Task ShowUI(CancellationToken ct = default)
    {
        await HotKey(ct, KeyCode.Alt, KeyCode.E);
    }

    public async Task CloseUI(CancellationToken ct = default)
    {
        await HotKey(ct, KeyCode.Alt, KeyCode.E);
    }
}
