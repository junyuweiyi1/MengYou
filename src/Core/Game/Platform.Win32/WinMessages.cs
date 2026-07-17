/// <summary>
/// Windows 消息常量：模拟点击/键盘用。
/// </summary>
public static class WinMessages
{
    /// <summary>虚拟键：Alt。</summary>
    public const ushort VK_MENU = 0x12;

    /// <summary>鼠标左键按下。</summary>
    public const uint WM_LBUTTONDOWN = 0x0201;
    /// <summary>鼠标左键抬起。</summary>
    public const uint WM_LBUTTONUP = 0x0202;
    /// <summary>鼠标右键按下。</summary>
    public const uint WM_RBUTTONDOWN = 0x0204;
    /// <summary>鼠标右键抬起。</summary>
    public const uint WM_RBUTTONUP = 0x0205;
    /// <summary>鼠标移动。</summary>
    public const uint WM_MOUSEMOVE = 0x0200;
    /// <summary>键盘按下。</summary>
    public const uint WM_KEYDOWN = 0x0100;
    /// <summary>键盘抬起。</summary>
    public const uint WM_KEYUP = 0x0101;
    /// <summary>字符消息。</summary>
    public const uint WM_CHAR = 0x0102;
    /// <summary>带 Alt 上下文的系统键按下。</summary>
    public const uint WM_SYSKEYDOWN = 0x0104;
    /// <summary>带 Alt 上下文的系统键抬起。</summary>
    public const uint WM_SYSKEYUP = 0x0105;

    /// <summary>SendInput 鼠标事件：左键按下。</summary>
    public const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    /// <summary>SendInput 鼠标事件：左键抬起。</summary>
    public const uint MOUSEEVENTF_LEFTUP = 0x0004;
    /// <summary>SendInput 鼠标事件：右键按下。</summary>
    public const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
    /// <summary>SendInput 鼠标事件：右键抬起。</summary>
    public const uint MOUSEEVENTF_RIGHTUP = 0x0010;
    /// <summary>SendInput 鼠标事件：绝对坐标。</summary>
    public const uint MOUSEEVENTF_ABSOLUTE = 0x8000;
    /// <summary>SendInput 鼠标事件：移动。</summary>
    public const uint MOUSEEVENTF_MOVE = 0x0001;

    /// <summary>SendInput 键盘：按键抬起。</summary>
    public const uint KEYEVENTF_KEYUP = 0x0002;
    /// <summary>SendInput 键盘：扫描码模式（不再走 VirtualKey，模拟真实硬件按键上报）。</summary>
    public const uint KEYEVENTF_SCANCODE = 0x0008;
    /// <summary>SendInput 键盘：扩展键（如右 Ctrl/Alt、方向键、Insert/Delete/Home/End/PageUp/PageDown 等）。</summary>
    public const uint KEYEVENTF_EXTENDEDKEY = 0x0001;

    /// <summary>MapVirtualKey：VK 转扫描码。</summary>
    public const uint MAPVK_VK_TO_VSC = 0x00;

    /// <summary>将鼠标坐标打包为 lParam（低字=x，高字=y）。</summary>
    public static IntPtr MakeLParam(int x, int y) => (IntPtr)((y << 16) | (x & 0xFFFF));
}
