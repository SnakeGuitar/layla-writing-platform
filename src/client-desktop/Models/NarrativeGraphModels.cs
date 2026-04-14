using System.Windows;

namespace Layla.Desktop.Models
{
    /// <summary>
    /// Entity node in the narrative graph, deserialized from the worldbuilding API.
    /// </summary>
    public class GraphNode
    {
        /// <summary>UUID of the underlying wiki entry.</summary>
        public string EntityId { get; set; } = string.Empty;

        /// <summary>Display name of the entity.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Entity type (Character, Location, Event, Object).</summary>
        public string EntityType { get; set; } = string.Empty;

        /// <summary>Computed center position on the graph canvas.</summary>
        public Point Center { get; set; }

        /// <summary>Display radius on the canvas.</summary>
        public double Radius { get; set; } = 40;
    }

    /// <summary>
    /// Directed edge between two nodes in the narrative graph.
    /// </summary>
    public class GraphEdge
    {
        /// <summary>Source entity ID.</summary>
        public string SourceId { get; set; } = string.Empty;

        /// <summary>Target entity ID.</summary>
        public string TargetId { get; set; } = string.Empty;

        /// <summary>Relationship type (e.g. RELATED_TO, APPEARS_IN).</summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>Human-readable label for the edge.</summary>
        public string Label { get; set; } = string.Empty;

        /// <summary>Resolved source node reference (set by the view model).</summary>
        public GraphNode? Source { get; set; }

        /// <summary>Resolved target node reference (set by the view model).</summary>
        public GraphNode? Target { get; set; }
    }

    /// <summary>
    /// Full graph response from the worldbuilding API.
    /// </summary>
    public class GraphResult
    {
        /// <summary>All entity nodes in the graph.</summary>
        public System.Collections.Generic.List<GraphNode> Nodes { get; set; } = new();

        /// <summary>All directed edges between nodes.</summary>
        public System.Collections.Generic.List<GraphEdge> Edges { get; set; } = new();
    }
}
