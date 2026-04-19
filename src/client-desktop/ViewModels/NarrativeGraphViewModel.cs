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
    /// Loads nodes and edges from the worldbuilding API, arranges them
    /// on a canvas, and supports relationship creation/deletion.
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

        // ── Relationship creation dialog state ──

        /// <summary>Controls visibility of the "Add Relationship" dialog.</summary>
        [ObservableProperty]
        private bool _isAddRelationshipVisible;

        /// <summary>All wiki entries available as source/target for new relationships.</summary>
        public ObservableCollection<WikiEntry> AvailableEntities { get; } = new();

        /// <summary>Source entity selected in the dialog.</summary>
        [ObservableProperty]
        private WikiEntry? _newRelSource;

        /// <summary>Target entity selected in the dialog.</summary>
        [ObservableProperty]
        private WikiEntry? _newRelTarget;

        /// <summary>Relationship type for the new edge.</summary>
        [ObservableProperty]
        private string _newRelType = "RELATED_TO";

        /// <summary>Human-readable label for the new edge.</summary>
        [ObservableProperty]
        private string _newRelLabel = string.Empty;

        /// <summary>Error message for the relationship dialog.</summary>
        [ObservableProperty]
        private string _addRelError = string.Empty;

        /// <summary>Available relationship types for the ComboBox.</summary>
        public string[] RelationshipTypes { get; } =
        {
            "RELATED_TO", "BELONGS_TO", "KNOWS", "LOVES", "HATES",
            "LOCATED_IN", "PART_OF", "CAUSES", "PRECEDES", "FOLLOWS"
        };

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

                ArrangeCircular(result.Nodes, 600, 400, Math.Min(300, Math.Max(120, result.Nodes.Count * 35)));

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
        /// Opens the "Add Relationship" dialog after loading available wiki entities.
        /// </summary>
        [RelayCommand]
        public async Task OpenAddRelationshipAsync()
        {
            AddRelError = string.Empty;
            NewRelSource = null;
            NewRelTarget = null;
            NewRelType = "RELATED_TO";
            NewRelLabel = string.Empty;

            try
            {
                var entries = await _wikiApi.GetEntriesAsync(_projectId);
                AvailableEntities.Clear();
                if (entries != null)
                {
                    foreach (var e in entries.OrderBy(e => e.Name))
                        AvailableEntities.Add(e);
                }
            }
            catch { }

            IsAddRelationshipVisible = true;
        }

        /// <summary>Closes the "Add Relationship" dialog without creating.</summary>
        [RelayCommand]
        public void CancelAddRelationship()
        {
            IsAddRelationshipVisible = false;
        }

        /// <summary>Creates the relationship and refreshes the graph.</summary>
        [RelayCommand]
        public async Task ConfirmAddRelationshipAsync()
        {
            if (NewRelSource == null || NewRelTarget == null)
            {
                AddRelError = "Select both source and target entities.";
                return;
            }
            if (NewRelSource.EntityId == NewRelTarget.EntityId)
            {
                AddRelError = "Source and target must be different entities.";
                return;
            }

            var label = string.IsNullOrWhiteSpace(NewRelLabel) ? NewRelType : NewRelLabel;
            var success = await _graphApi.CreateRelationshipAsync(
                _projectId, NewRelSource.EntityId, NewRelTarget.EntityId, NewRelType, label);

            if (success)
            {
                IsAddRelationshipVisible = false;
                await LoadGraphAsync();
            }
            else
            {
                AddRelError = "Failed to create relationship.";
            }
        }

        /// <summary>Deletes a relationship edge and refreshes the graph.</summary>
        [RelayCommand]
        public async Task DeleteRelationshipAsync(GraphEdge? edge)
        {
            if (edge == null) return;

            var confirm = MessageBox.Show(
                $"Delete relationship \"{edge.Label}\" between nodes?",
                "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes) return;

            var success = await _graphApi.DeleteRelationshipAsync(
                _projectId, edge.SourceId, edge.TargetId);

            if (success)
                await LoadGraphAsync();
        }

        /// <summary>
        /// Handles a node click — navigates to the wiki entry for that node.
        /// </summary>
        public void OnNodeClicked(string entityId)
        {
            WorkspaceMediator.RequestNavigateToWikiEntry(entityId);
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
