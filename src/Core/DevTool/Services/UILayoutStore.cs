using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MengYou.DevTool.Services;

/// <summary>
/// UILayout JSON 的可读写数据模型。
/// 与 Recognition.Image.UILayoutLocator 使用相同的文件格式。
/// </summary>
public sealed class UILayoutDocument
{
    /// <summary>点位表：Key → [x, y]。</summary>
    [JsonPropertyName("points")]
    public Dictionary<string, int[]> Points { get; set; } = new();

    /// <summary>区域表：Key → [x, y, w, h]。</summary>
    [JsonPropertyName("regions")]
    public Dictionary<string, int[]> Regions { get; set; } = new();
}

/// <summary>
/// UILayout 文件读写服务。
/// </summary>
public sealed class UILayoutStore
{
    /// <summary>文件绝对路径。</summary>
    public string FilePath { get; }

    /// <summary>当前文档。</summary>
    public UILayoutDocument Document { get; private set; } = new();

    /// <summary>JSON 选项：驼峰命名 + 缩进。</summary>
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <summary>构造。</summary>
    public UILayoutStore(string filePath)
    {
        FilePath = filePath;
        Load();
    }

    /// <summary>从磁盘加载；不存在则用空文档。</summary>
    public void Load()
    {
        if (!File.Exists(FilePath))
        {
            Document = new UILayoutDocument();
            return;
        }
        try
        {
            var json = File.ReadAllText(FilePath);
            Document = JsonSerializer.Deserialize<UILayoutDocument>(json, JsonOpts) ?? new UILayoutDocument();
        }
        catch
        {
            Document = new UILayoutDocument();
        }
    }

    /// <summary>写回磁盘。</summary>
    public void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        var json = JsonSerializer.Serialize(Document, JsonOpts);
        File.WriteAllText(FilePath, json);
    }

    /// <summary>添加或更新一个区域。</summary>
    public void SetRegion(string key, int x, int y, int w, int h)
    {
        Document.Regions[key] = new[] { x, y, w, h };
    }

    /// <summary>添加或更新一个点。</summary>
    public void SetPoint(string key, int x, int y)
    {
        Document.Points[key] = new[] { x, y };
    }

    /// <summary>删除某个键（点或区域）。</summary>
    public void Remove(string key)
    {
        Document.Points.Remove(key);
        Document.Regions.Remove(key);
    }
}
