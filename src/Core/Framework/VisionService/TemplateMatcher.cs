namespace iFramework;

/// <summary>
/// 模板匹配器：基于 OpenCV NCC（归一化互相关）算法。
/// </summary>
public sealed class TemplateMatcher
{
    /// <summary>已加载的模板缓存：Key=模板键名。</summary>
    private readonly Dictionary<string, Mat> _templates = new();

    /// <summary>模板文件根目录。</summary>
    private readonly string _templateDir;

    /// <summary>构造。</summary>
    /// <param name="templateDir">存放 png 模板的目录。</param>
    public TemplateMatcher(string templateDir)
    {
        _templateDir = templateDir;
    }

    /// <summary>预加载或懒加载模板。</summary>
    private Mat GetTemplate(string key)
    {
        if (_templates.TryGetValue(key, out var m)) return m;
        var path = Path.Combine(_templateDir, key + ".png");
        if (!File.Exists(path)) throw new FileNotFoundException($"模板未找到：{path}");
        var mat = Cv2.ImRead(path, ImreadModes.Color);
        _templates[key] = mat;
        return mat;
    }

    /// <summary>在源图中查找模板，返回中心点。</summary>
    /// <param name="source">当前截图。</param>
    /// <param name="key">模板键。</param>
    /// <param name="threshold">相似度阈值 0~1。</param>
    public Point2D? FindOne(Bitmap source, string key, double threshold = 0.85)
    {
        using var src = BitmapConverter.ToMat(source);
        var tmpl = GetTemplate(key);
        using var result = new Mat();
        Cv2.MatchTemplate(src, tmpl, result, TemplateMatchModes.CCoeffNormed);
        Cv2.MinMaxLoc(result, out _, out var maxVal, out _, out var maxLoc);
        if (maxVal < threshold) return null;
        return new Point2D(maxLoc.X + tmpl.Width / 2, maxLoc.Y + tmpl.Height / 2);
    }

    /// <summary>查找模板全部出现位置。</summary>
    public IReadOnlyList<Point2D> FindAll(Bitmap source, string key, double threshold = 0.85)
    {
        using var src = BitmapConverter.ToMat(source);
        var tmpl = GetTemplate(key);
        using var result = new Mat();
        Cv2.MatchTemplate(src, tmpl, result, TemplateMatchModes.CCoeffNormed);
        var points = new List<Point2D>();
        // 简单非极大值抑制：遍历超过阈值的点，抑制附近区域
        var w = tmpl.Width;
        var h = tmpl.Height;
        for (var y = 0; y < result.Rows; y++)
        {
            for (var x = 0; x < result.Cols; x++)
            {
                if (result.At<float>(y, x) >= threshold)
                {
                    points.Add(new Point2D(x + w / 2, y + h / 2));
                    // 抑制半个模板范围内的点
                    Cv2.Rectangle(result, new OpenCvSharp.Rect(x - w / 2, y - h / 2, w, h), new Scalar(0), thickness: -1);
                }
            }
        }
        return points;
    }
}
