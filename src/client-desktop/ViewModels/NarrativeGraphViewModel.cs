using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Layla.Desktop.Models;
using Layla.Desktop.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Layla.Desktop.ViewModels
{
    /// <summary>
    /// ViewModel for the narrative graph viewer.
    /// Loads nodes and edges from the worldbuilding API and arranges them
    /// on a canvas using a simple force-directed layout.
    /// </summary>
    public partial class NarrativeGraphViewModel : ObservableObject
    {
        private readonly IGraphApiService _graphApi;
        private readonly IWikiApiService _wikiApi;
        private Guid _projectId;

        /// <summary><c>true</c> while graph data is being fetched.</summary>
        [ObservableProperty]
        private bool _isLoading;

        /// <summary>Nodes on the graph canvas.</summary>
        public ObservableCollection<GraphNode> Nodes { get; } = new();

        /// <summary>Edges on the graph canvas.</summary>
        public ObservableCollection<GraphEdge> Edges { get; } = new();

        public NarrativeGraphViewModel(IGraphApiService graphApi, IWikiApiService wikiApi)
        {
            _graphApi = graphApi;
            _wikiApi = wikiApi;
        }

        /// <summary>Sets the project context.</summary>
        public void Initialize(Guid projectId)
        {
            _projectId = projectId;
        }

        /// <summary>
        /// Fetches the full graph from the API and populates <see cref="Nodes"/> and <see cref="Edges"/>.
        /// Applies a circular layout to position nodes on the canvas.
        /// </summary>
        [RelayCommand]
        public async Task LoadGraphAsync()
        {
            IsLoading = true;
            try
            {
                var result = await _graphApi.GetGraphAsync(_projectId);
                if (result == null) return;

                Nodes.Clear();
                Edges.Clear();

                var nodeMap = new Dictionary<string, GraphNode>();

                ArrangeCircular(result.Nodes, 600, 400, Math.Min(300, result.Nodes.Count * 30));

                foreach (var node in result.Nodes)
                {
                    Nodes.Add(node);
                    nodeMap[node.EntityId] = node;
                }

                foreach (var edge in result.Edges)
                {
                    if (nodeMap.TryGetValue(edge.SourceId, out var source) &&
                        nodeMap.TryGetValue(edge.TargetId, out var target))
                    {
                        edge.Source = source;
                        edge.Target = target;
                        Edges.Add(edge);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NarrativeGraphVM] Load failed: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Arranges nodes in a circle around a center point.
        /// </summary>
        private static void ArrangeCircular(List<GraphNode> nodes, double cx, double cy, double radius)
        {
            if (nodes.Count == 0) return;
            if (nodes.Count == 1)
            {
                nodes[0].Center = new Point(cx, cy);
                return;
            }

            double angleStep = 2 * Math.PI / nodes.Count;
            for (int i = 0; i < nodes.Count; i++)
            {
                double angle = i * angleStep - Math.PI / 2;
                nodes[i].Center = new Point(
                    cx + radius * Math.Cos(angle),
                    cy + radius * Math.Sin(angle));
            }
        }
    }
}
