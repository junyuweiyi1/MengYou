using System.Drawing;
using System.Drawing.Imaging;
using MengYou.Platform.Win32.Native;

/// <summary>
/// 窗口截图工具：优先使用 PrintWindow（可截被遮挡的窗口），失败时回落 BitBlt。
/// </summary>
public static class WindowCapture
{
    /// <summary>PrintWindow 标志：截取整个客户端区域。</summary>
    private const uint PW_CLIENTONLY = 0x1;

    /// <summary>PrintWindow 标志：含渲染表面（Windows 8.1+）。</summary>
    private const uint PW_RENDERFULLCONTENT = 0x2;

    /// <summary>截取指定窗口客户端区域。</summary>
    /// <param name="hWnd">目标窗口句柄。</param>
    /// <returns>位图；调用者负责 Dispose。</returns>
    public static Bitmap CaptureClient(IntPtr hWnd)
    {
        User32.GetClientRect(hWnd, out var rect);
        var width = rect.Right - rect.Left;
        var height = rect.Bottom - rect.Top;
        if (width <= 0 || height <= 0) throw new InvalidOperationException("窗口客户端区域无效");

        var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bmp))
        {
            var hdcBmp = g.GetHdc();
            try
            {
                // 首选 PrintWindow：可以截取后台窗口
                var ok = Gdi32.PrintWindow(hWnd, hdcBmp, PW_CLIENTONLY | PW_RENDERFULLCONTENT);
                if (!ok)
                {
                    // 回落方案：BitBlt from window DC
                    var hdcSrc = Gdi32.GetDC(hWnd);
                    Gdi32.BitBlt(hdcBmp, 0, 0, width, height, hdcSrc, 0, 0, Gdi32.SRCCOPY);
                    Gdi32.ReleaseDC(hWnd, hdcSrc);
                }
            }
            finally
            {
                g.ReleaseHdc(hdcBmp);
            }
        }
        return bmp;
    }
}
