using CommunityToolkit.Mvvm.ComponentModel;
using Layla.Desktop.Models;
using System.Collections.ObjectModel;
using System.Windows;

namespace Layla.Desktop.ViewModels
{
    public partial class NarrativeGraphViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<GraphNode> _nodes = new();

        [ObservableProperty]
        private ObservableCollection<GraphEdge> _edges = new();

        public NarrativeGraphViewModel()
        {
            InitializeMockData();
        }

        private void InitializeMockData()
        {
            // Mock data for demonstration
            var node1 = new GraphNode { Id = "1", Name = "Fox", Subtitle = "Protagonist", Center = new Point(100, 100) };
            var node2 = new GraphNode { Id = "2", Name = "Forest", Subtitle = "Location", Center = new Point(300, 200) };
            
            Nodes.Add(node1);
            Nodes.Add(node2);
            
            Edges.Add(new GraphEdge { Source = node1, Target = node2, Label = "Enters" });
        }
    }
}
