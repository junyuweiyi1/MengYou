using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace MengYou.DevTool.Services;

/// <summary>
/// 模板 PNG 保存服务：从原始截图裁剪矩形并保存到模板目录。
/// </summary>
public sealed class TemplateStore
{
    /// <summary>模板目录。</summary>
    public string TemplateDir { get; }

    /// <summary>构造。</summary>
    public TemplateStore(string templateDir)
    {
        TemplateDir = templateDir;
        Directory.CreateDirectory(templateDir);
    }

    /// <summary>裁剪并保存为 PNG。</summary>
    /// <param name="source">完整截图。</param>
    /// <param name="rect">裁剪矩形（客户端坐标）。</param>
    /// <param name="templateKey">模板键（作为文件名）。</param>
    /// <returns>保存的文件路径。</returns>
    public string SaveTemplate(Bitmap source, Rectangle rect, string templateKey)
    {
        // Clamp 到源图范围
        rect = Rectangle.Intersect(rect, new Rectangle(0, 0, source.Width, source.Height));
        if (rect.Width <= 0 || rect.Height <= 0)
            throw new ArgumentException("裁剪矩形无效", nameof(rect));

        using var crop = source.Clone(rect, PixelFormat.Format32bppArgb);
        var path = Path.Combine(TemplateDir, templateKey + ".png");
        crop.Save(path, ImageFormat.Png);
        return path;
    }

    /// <summary>列出已有模板文件名（去后缀）。</summary>
    public IReadOnlyList<string> List()
    {
        if (!Directory.Exists(TemplateDir)) return Array.Empty<string>();
        return Directory.GetFiles(TemplateDir, "*.png")
            .Select(Path.GetFileNameWithoutExtension)
            .OfType<string>()
            .OrderBy(s => s)
            .ToList();
    }

    /// <summary>删除指定模板。</summary>
    public bool Delete(string templateKey)
    {
        var path = Path.Combine(TemplateDir, templateKey + ".png");
        if (!File.Exists(path)) return false;
        File.Delete(path);
        return true;
    }
}
