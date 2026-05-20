using System.Collections.Generic;

namespace client_web.Models.Worldbuilding;

public class GraphNode
{
    public string EntityId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public double X { get; set; }
    public double Y { get; set; }
    public double Radius { get; set; } = 35;
}

public class GraphEdge
{
    public string SourceId { get; set; } = string.Empty;
    public string TargetId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public GraphNode? Source { get; set; }
    public GraphNode? Target { get; set; }
}

public class GraphResult
{
    public List<GraphNode> Nodes { get; set; } = new();
    public List<GraphEdge> Edges { get; set; } = new();
}
