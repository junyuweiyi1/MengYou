using MengYou.Core.Config;

namespace MengYou.Modules.Path.World;

/// <summary>
/// 世界地图图数据：所有地图节点 + 连接。
/// </summary>
public sealed class WorldGraph
{
    /// <summary>节点表：Key=MapId。</summary>
    public Dictionary<string, MapNode> Nodes { get; set; } = new();

    /// <summary>从 YAML 文件加载。</summary>
    public static WorldGraph LoadFromFile(string path)
    {
        var raw = YamlConfigLoader.LoadFromFile<WorldGraphYaml>(path);
        var g = new WorldGraph();
        foreach (var kv in raw.Maps)
        {
            g.Nodes[kv.Key] = new MapNode
            {
                Id = kv.Key,
                DisplayName = kv.Value.DisplayName ?? kv.Key,
                Connections = kv.Value.Connections,
            };
        }
        return g;
    }

    /// <summary>取节点，找不到返回 null。</summary>
    public MapNode? Get(string mapId) => Nodes.TryGetValue(mapId, out var n) ? n : null;
}

/// <summary>YAML 直接映射结构（内部使用）。</summary>
internal sealed class WorldGraphYaml
{
    /// <summary>地图表。</summary>
    public Dictionary<string, WorldGraphYamlMap> Maps { get; set; } = new();
}

/// <summary>YAML 中每个地图的定义。</summary>
internal sealed class WorldGraphYamlMap
{
    /// <summary>显示名。</summary>
    public string? DisplayName { get; set; }

    /// <summary>连接列表。</summary>
    public List<MapConnection> Connections { get; set; } = new();
}
