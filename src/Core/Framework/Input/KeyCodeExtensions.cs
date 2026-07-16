namespace iFramework;

/// <summary>
/// 键码辅助扩展：判断是否为"扩展键"（Extended Key）。
/// 扩展键（右 Ctrl/Alt、方向键、Insert/Delete/Home/End/PageUp/PageDown、NumLock 等）
/// 在扫描码上需要附加 E0 前缀标志，否则会被系统/游戏误判为数字键盘上的等价键或被忽略。
/// </summary>
public static class KeyCodeExtensions
{
    public static bool IsExtendedKey(this KeyCode key) => key switch
    {
        KeyCode.RControl => true,
        KeyCode.RAlt => true,
        KeyCode.Left => true,
        KeyCode.Up => true,
        KeyCode.Right => true,
        KeyCode.Down => true,
        KeyCode.Insert => true,
        KeyCode.Delete => true,
        KeyCode.Home => true,
        KeyCode.End => true,
        KeyCode.PageUp => true,
        KeyCode.PageDown => true,
        _ => false,
    };
}
