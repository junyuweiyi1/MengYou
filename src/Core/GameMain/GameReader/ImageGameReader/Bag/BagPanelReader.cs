using iFramework;

/// <summary>
/// 背包读取器：遍历 BagSlot.1..N 格子，识别物品名与数量。
/// UILayout 约定：
/// - BagSlot.N.Icon     格子图标矩形（用于判断"是否有物品"，检测非透明像素）
/// - BagSlot.N.Count    数量数字矩形（OCR 数字）
/// - BagSlot.N.Name     物品名矩形（可选；若未配置则通过图标模板匹配识别名字）
/// </summary>
public sealed class BagPanelReader
{
    /// <summary>视觉。</summary>
    private readonly IVisionService _vision;

    /// <summary>UI 布局。</summary>
    private readonly IUIElementLocateMgr _locator;

    /// <summary>物品名识别所用模板前缀（可选）：如 "ItemIcon.飞行符"。</summary>
    private readonly IReadOnlyList<string> _knownItemNames;

    /// <summary>模板匹配阈值。</summary>
    private const double MatchThreshold = 0.85;

    /// <summary>格子最大数（梦幻西游背包一页 20 格，共 4 页 = 80 格，可配置）。</summary>
    private const int MaxSlots = 80;

    /// <summary>构造。</summary>
    /// <param name="vision">视觉服务。</param>
    /// <param name="locator">UI 布局。</param>
    /// <param name="knownItemNames">已知物品清单（用于图标模板匹配识别）。</param>
    public BagPanelReader(IVisionService vision, IUIElementLocateMgr locator, IReadOnlyList<string>? knownItemNames = null)
    {
        _vision = vision;
        _locator = locator;
        _knownItemNames = knownItemNames ?? Array.Empty<string>();
    }

    /// <summary>读取整个背包快照。</summary>
    public BagSnapshot ReadAll()
    {
        var items = new List<BagItemSnapshot>();
        for (var i = 1; i <= MaxSlots; i++)
        {
            var key = $"BagSlot.{i}";
            var iconRegion = _locator.LocateRegion($"{key}.Icon");
            if (iconRegion == null) continue;
            // 判断格子是否有物品：中心像素非透明/非空即视为存在
            if (!HasItem(iconRegion.Value)) continue;

            var name = ReadItemName(key, iconRegion.Value);
            var countRegion = _locator.LocateRegion($"{key}.Count");
            var count = countRegion != null ? (_vision.ReadNumber(countRegion.Value) ?? 1) : 1;

            items.Add(new BagItemSnapshot { Name = name, Count = count, SlotIndex = i });
        }
        return new BagSnapshot { Items = items };
    }

    /// <summary>判定格子是否有物品：采样中心 3x3 像素，只要有一个不接近背景色即认为有物品。</summary>
    private bool HasItem(Rect region)
    {
        var cx = region.X + region.Width / 2;
        var cy = region.Y + region.Height / 2;
        for (var dy = -1; dy <= 1; dy++)
        {
            for (var dx = -1; dx <= 1; dx++)
            {
                var argb = _vision.GetPixel(new Point2D(cx + dx, cy + dy));
                if (!IsBackgroundColor(argb)) return true;
            }
        }
        return false;
    }

    /// <summary>判定像素是否接近背包空格背景色（深灰系）。</summary>
    private static bool IsBackgroundColor(int argb)
    {
        var r = (argb >> 16) & 0xFF;
        var g = (argb >> 8) & 0xFF;
        var b = argb & 0xFF;
        // 深灰：三通道均低且相近
        return r < 80 && g < 80 && b < 80 && Math.Abs(r - g) < 15 && Math.Abs(g - b) < 15;
    }

    /// <summary>识别物品名：优先用 Name 区域 OCR，回落到图标模板匹配。</summary>
    private string ReadItemName(string slotKey, Rect iconRegion)
    {
        var nameRegion = _locator.LocateRegion($"{slotKey}.Name");
        if (nameRegion != null)
        {
            var t = _vision.ReadText(nameRegion.Value);
            if (!string.IsNullOrWhiteSpace(t)) return t;
        }
        // 图标模板匹配：遍历已知物品名
        foreach (var candidate in _knownItemNames)
        {
            var hit = _vision.FindTemplate($"ItemIcon.{candidate}", MatchThreshold);
            if (hit == null) continue;
            // 命中点是否落在当前格子区域内
            if (hit.Value.X >= iconRegion.X && hit.Value.X < iconRegion.X + iconRegion.Width
                && hit.Value.Y >= iconRegion.Y && hit.Value.Y < iconRegion.Y + iconRegion.Height)
            {
                return candidate;
            }
        }
        return string.Empty;
    }
}
