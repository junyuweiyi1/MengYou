using iFramework;
using System.Collections.Generic;

public class BagItem
{
    /// <summary>物品名。</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>数量。</summary>
    public int Count { get; init; }

    /// <summary>在第几个背包。</summary>
    public int BagIndex { get; init; }

    /// <summary>格子索引（用于点击定位）。</summary>
    public int SlotIndex { get; init; }
}

/// <summary>
/// 背包管理器：负责查询背包快照与使用背包物品。
/// 依赖 <see cref="IGameReader"/> 读取背包快照、<see cref="IUIElementLocateMgr"/> 定位格子坐标、
/// <see cref="IInputMgr"/> 发送鼠标事件。
/// </summary>
public sealed class BagMgr
{
    private readonly Game _game;
    private Dictionary<string, BagItem> _items = new Dictionary<string, BagItem>();

    public BagMgr(Game game)
    {
        _game = game;
    }

    public async void RefreshData()
    {
        _items.Clear();

        var bagSnapshot = await _game.GameReader.GetBagSnapshot();
        foreach (var itemSnapshot in bagSnapshot.Items)
        {
            var bagItem = new BagItem()
            {
                Name = itemSnapshot.Name,
                Count = itemSnapshot.Count,
                BagIndex = itemSnapshot.BagIndex,
                SlotIndex = itemSnapshot.SlotIndex,
            };
            if (!_items.ContainsKey(itemSnapshot.Name))
            {
                _items.Add(itemSnapshot.Name, bagItem);
            }
            else
            {
                _items[itemSnapshot.Name] = bagItem;
            }
        }
    }

    public IEnumerable<BagItem> GetItems()
    {
        return _items.Values;
    }

    public int GetItmeCount(string itemName)
    {
        return GetItem(itemName)?.Count ?? 0;
    }

    public BagItem GetItem(string itemName)
    {
        _items.TryGetValue(itemName, out var result);
        return result;
    }

    /// <summary>
    /// 使用指定名称的物品（右键点击背包格子）。
    /// </summary>
    /// <param name="itemName">物品名。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>true=已发送使用点击；false=背包中未找到该物品或格子无法定位。</returns>
    public async Task<bool> UseItem(string itemName, int useCount, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(itemName)) return false;

        var item = GetItem(itemName);
        if (item == null || item.Count == 0) return false;

        await _game.GameControl.UseBagItem(item.Name, item.BagIndex, item.SlotIndex, useCount);

        // 打开背包并刷新截图，读取最新快照
        if (!await EnsureBagOpenedAsync(ct)) return false;

        var snapshot = _reader.GetBagSnapshot();
        var target = snapshot.Items.FirstOrDefault(
            x => string.Equals(x.Name, itemName, StringComparison.Ordinal) && x.Count > 0);
        if (target == null) return false;

        return await UseSlotAsync(target.SlotIndex, ct);
    }

    /// <summary>
    /// 使用背包中第一个匹配到的加血道具。
    /// </summary>
    /// <param name="ct">取消令牌。</param>
    /// <returns>true=已发送使用点击；false=背包中无任何加血道具。</returns>
    public async Task<bool> UseHealItemAsync(CancellationToken ct = default)
    {
        if (!await EnsureBagOpenedAsync(ct)) return false;

        var snapshot = _reader.GetBagSnapshot();
        // 按白名单顺序查找第一个匹配项
        foreach (var name in _healItemNames)
        {
            var hit = snapshot.Items.FirstOrDefault(
                x => string.Equals(x.Name, name, StringComparison.Ordinal) && x.Count > 0);
            if (hit == null) continue;
            return await UseSlotAsync(hit.SlotIndex, ct);
        }
        return false;
    }

    /// <summary>
    /// 判断背包中是否有指定物品（数量 > 0）。
    /// </summary>
    /// <param name="itemName">物品名。</param>
    /// <returns>true=存在。</returns>
    public bool HasItem(string itemName)
    {
        if (string.IsNullOrWhiteSpace(itemName)) return false;
        var snapshot = _reader.GetBagSnapshot();
        return snapshot.Items.Any(
            x => string.Equals(x.Name, itemName, StringComparison.Ordinal) && x.Count > 0);
    }

    /// <summary>确保背包面板已打开：若已配置 BagButton 则点击一次并等待渲染。</summary>
    private async Task<bool> EnsureBagOpenedAsync(CancellationToken ct)
    {
        // BagButton 未标定时不阻塞流程，直接假定背包已打开
        if (_locator.Locate("BagButton") is not { } bagBtn) return true;

        await _controller.ClickAsync(bagBtn, MouseButton.Left, ct);
        // 等待背包面板动画/渲染
        await Task.Delay(200, ct);
        return true;
    }

    /// <summary>右键点击指定格子中心，触发"使用"。</summary>
    /// <param name="slotIndex">格子索引（1-based）。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>true=已发送点击；false=格子坐标未标定。</returns>
    private async Task<bool> UseSlotAsync(int slotIndex, CancellationToken ct)
    {
        var region = _locator.LocateRegion($"BagSlot.{slotIndex}.Icon");
        if (region == null) return false;

        await _controller.ClickAsync(region.Value.Center, MouseButton.Right, ct);
        return true;
    }
}
