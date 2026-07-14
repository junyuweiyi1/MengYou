namespace MengYou.Modules.Path.World;

/// <summary>
/// 路径规划器：Dijkstra 算出跨地图最短代价路径。
/// </summary>
public sealed class PathPlanner
{
    /// <summary>世界图数据。</summary>
    private readonly WorldGraph _graph;

    /// <summary>构造。</summary>
    public PathPlanner(WorldGraph graph)
    {
        _graph = graph;
    }

    /// <summary>规划：返回节点 ID 序列（含起点与终点）；无路径返回空数组。</summary>
    /// <param name="fromMapId">起点地图。</param>
    /// <param name="toMapId">终点地图。</param>
    public IReadOnlyList<PathStep> Plan(string fromMapId, string toMapId)
    {
        if (fromMapId == toMapId) return Array.Empty<PathStep>();
        if (_graph.Get(fromMapId) == null || _graph.Get(toMapId) == null) return Array.Empty<PathStep>();

        // Dijkstra
        var dist = new Dictionary<string, double>();
        var prev = new Dictionary<string, (string prevId, MapConnection edge)>();
        var pq = new PriorityQueue<string, double>();
        foreach (var id in _graph.Nodes.Keys) dist[id] = double.PositiveInfinity;
        dist[fromMapId] = 0;
        pq.Enqueue(fromMapId, 0);

        while (pq.TryDequeue(out var u, out var du))
        {
            if (u == toMapId) break;
            if (du > dist[u]) continue;
            var node = _graph.Get(u)!;
            foreach (var e in node.Connections)
            {
                var nd = du + e.Cost;
                if (nd < dist.GetValueOrDefault(e.To, double.PositiveInfinity))
                {
                    dist[e.To] = nd;
                    prev[e.To] = (u, e);
                    pq.Enqueue(e.To, nd);
                }
            }
        }

        if (!prev.ContainsKey(toMapId)) return Array.Empty<PathStep>();

        // 回溯
        var steps = new List<PathStep>();
        var cur = toMapId;
        while (cur != fromMapId)
        {
            var (p, edge) = prev[cur];
            steps.Add(new PathStep { FromMapId = p, ToMapId = cur, Via = edge });
            cur = p;
        }
        steps.Reverse();
        return steps;
    }
}

/// <summary>规划结果的一步：从某图到另一图。</summary>
public sealed class PathStep
{
    /// <summary>起始地图。</summary>
    public string FromMapId { get; set; } = string.Empty;

    /// <summary>目标地图。</summary>
    public string ToMapId { get; set; } = string.Empty;

    /// <summary>使用的连接边。</summary>
    public MapConnection Via { get; set; } = new();
}
