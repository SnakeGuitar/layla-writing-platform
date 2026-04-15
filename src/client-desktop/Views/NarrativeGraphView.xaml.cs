using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Layla.Desktop.Models;
using Layla.Desktop.Services;
using Layla.Desktop.ViewModels;

namespace Layla.Desktop.Views
{
    public partial class NarrativeGraphView : Page
    {
        private readonly NarrativeGraphViewModel _viewModel;

        // Drag state
        private GraphNode? _dragNode;
        private Point _dragOffset;
        private bool _isDragging;

        // Map FrameworkElement → model for hit-testing
        private readonly Dictionary<FrameworkElement, GraphNode> _nodeElements = new();
        private readonly Dictionary<FrameworkElement, GraphEdge> _edgeElements = new();

        private static readonly Dictionary<string, Color> EntityColors = new()
        {
            { "Character", (Color)ColorConverter.ConvertFromString("#4FC3F7")! },
            { "Location",  (Color)ColorConverter.ConvertFromString("#81C784")! },
            { "Event",     (Color)ColorConverter.ConvertFromString("#FFB74D")! },
            { "Object",    (Color)ColorConverter.ConvertFromString("#CE93D8")! },
            { "Concept",   (Color)ColorConverter.ConvertFromString("#90A4AE")! },
        };

        public NarrativeGraphView(Guid projectId)
        {
            InitializeComponent();
            _viewModel = ServiceLocator.GetService<NarrativeGraphViewModel>()
                ?? throw new InvalidOperationException("NarrativeGraphViewModel not registered");
            _viewModel.Initialize(projectId);
            DataContext = _viewModel;

            ((INotifyCollectionChanged)_viewModel.Nodes).CollectionChanged += (_, _) => DrawGraph();
            ((INotifyCollectionChanged)_viewModel.Edges).CollectionChanged += (_, _) => DrawGraph();

            Loaded += async (_, _) => await _viewModel.LoadGraphCommand.ExecuteAsync(null);
        }

        // ═══ GRAPH RENDERING ═══

        private void DrawGraph()
        {
            GraphCanvas.Children.Clear();
            _nodeElements.Clear();
            _edgeElements.Clear();

            foreach (var edge in _viewModel.Edges)
                DrawEdge(edge);

            foreach (var node in _viewModel.Nodes)
                DrawNode(node);
        }

        private void DrawEdge(GraphEdge edge)
        {
            if (edge.Source == null || edge.Target == null) return;

            var line = new Line
            {
                X1 = edge.Source.Center.X,
                Y1 = edge.Source.Center.Y,
                X2 = edge.Target.Center.X,
                Y2 = edge.Target.Center.Y,
                Stroke = (Brush)FindResource("BorderColor"),
                StrokeThickness = 2,
                StrokeDashArray = edge.Type == "APPEARS_IN" ? new DoubleCollection { 4, 2 } : null,
                Cursor = Cursors.Hand,
                ToolTip = $"{edge.Type}: {edge.Label}\n(Right-click to delete)"
            };

            // Right-click to delete edge
            line.MouseRightButtonUp += (s, e) =>
            {
                e.Handled = true;
                _viewModel.DeleteRelationshipCommand.Execute(edge);
            };

            _edgeElements[line] = edge;
            GraphCanvas.Children.Add(line);

            // Draw arrowhead
            DrawArrowhead(edge.Source.Center, edge.Target.Center, edge.Target.Radius);

            if (!string.IsNullOrEmpty(edge.Label))
            {
                double midX = (line.X1 + line.X2) / 2;
                double midY = (line.Y1 + line.Y2) / 2;

                var border = new Border
                {
                    Background = (Brush)FindResource("ControlBackground"),
                    BorderBrush = (Brush)FindResource("BorderColor"),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(5, 2, 5, 2),
                    Cursor = Cursors.Hand,
                    ToolTip = $"Right-click to delete"
                };

                border.Child = new TextBlock
                {
                    Text = edge.Label,
                    Foreground = (Brush)FindResource("PrimaryText"),
                    FontSize = 10,
                    FontWeight = FontWeights.Medium
                };

                border.MouseRightButtonUp += (s, e) =>
                {
                    e.Handled = true;
                    _viewModel.DeleteRelationshipCommand.Execute(edge);
                };

                _edgeElements[border] = edge;
                border.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                Canvas.SetLeft(border, midX - border.DesiredSize.Width / 2);
                Canvas.SetTop(border, midY - border.DesiredSize.Height / 2);
                GraphCanvas.Children.Add(border);
            }
        }

        /// <summary>Draws a small triangle arrowhead at the edge of the target node.</summary>
        private void DrawArrowhead(Point from, Point to, double nodeRadius)
        {
            double dx = to.X - from.X;
            double dy = to.Y - from.Y;
            double len = Math.Sqrt(dx * dx + dy * dy);
            if (len < 1) return;

            // Point on the target node's edge
            double ux = dx / len;
            double uy = dy / len;
            double tipX = to.X - ux * (nodeRadius + 2);
            double tipY = to.Y - uy * (nodeRadius + 2);

            double arrowLen = 10;
            double arrowWidth = 5;
            double baseX = tipX - ux * arrowLen;
            double baseY = tipY - uy * arrowLen;

            var arrow = new Polygon
            {
                Points = new PointCollection
                {
                    new Point(tipX, tipY),
                    new Point(baseX + uy * arrowWidth, baseY - ux * arrowWidth),
                    new Point(baseX - uy * arrowWidth, baseY + ux * arrowWidth),
                },
                Fill = (Brush)FindResource("BorderColor"),
                IsHitTestVisible = false
            };
            GraphCanvas.Children.Add(arrow);
        }

        private void DrawNode(GraphNode node)
        {
            var color = EntityColors.GetValueOrDefault(node.EntityType, Colors.Gray);
            var brush = new SolidColorBrush(color);

            // Outer container for dragging and click detection
            var nodeGroup = new Border
            {
                Width = node.Radius * 2,
                Height = node.Radius * 2,
                CornerRadius = new CornerRadius(node.Radius),
                Background = brush,
                BorderBrush = (Brush)FindResource("BorderColor"),
                BorderThickness = new Thickness(2),
                Opacity = 0.9,
                Cursor = Cursors.Hand,
                ToolTip = $"{node.Name} ({node.EntityType})\nDrag to move · Click to view in Wiki",
                Tag = node.EntityId
            };

            var stack = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            stack.Children.Add(new TextBlock
            {
                Text = node.Name,
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                FontSize = 12,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
                MaxWidth = node.Radius * 1.8
            });

            stack.Children.Add(new TextBlock
            {
                Text = node.EntityType,
                Foreground = new SolidColorBrush(Color.FromArgb(180, 255, 255, 255)),
                FontSize = 9,
                HorizontalAlignment = HorizontalAlignment.Center
            });

            nodeGroup.Child = stack;
            _nodeElements[nodeGroup] = node;

            Canvas.SetLeft(nodeGroup, node.Center.X - node.Radius);
            Canvas.SetTop(nodeGroup, node.Center.Y - node.Radius);
            GraphCanvas.Children.Add(nodeGroup);
        }

        // ═══ NODE DRAGGING + CLICK → WIKI ═══

        private void GraphCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var pos = e.GetPosition(GraphCanvas);
            var hit = VisualTreeHelper.HitTest(GraphCanvas, pos);
            if (hit?.VisualHit == null) return;

            // Walk up to find a Border node element
            DependencyObject? current = hit.VisualHit as DependencyObject;
            while (current != null && current != GraphCanvas)
            {
                if (current is Border border && _nodeElements.TryGetValue(border, out var node))
                {
                    _dragNode = node;
                    _dragOffset = new Point(pos.X - node.Center.X, pos.Y - node.Center.Y);
                    _isDragging = false;
                    GraphCanvas.CaptureMouse();
                    e.Handled = true;
                    return;
                }
                current = VisualTreeHelper.GetParent(current);
            }
        }

        private void GraphCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (_dragNode == null || e.LeftButton != MouseButtonState.Pressed) return;

            var pos = e.GetPosition(GraphCanvas);
            var newCenter = new Point(pos.X - _dragOffset.X, pos.Y - _dragOffset.Y);

            // Only start dragging after threshold to differentiate from clicks
            if (!_isDragging)
            {
                var delta = newCenter - _dragNode.Center;
                if (Math.Abs(delta.X) + Math.Abs(delta.Y) < 5) return;
                _isDragging = true;
            }

            _dragNode.Center = newCenter;
            DrawGraph(); // Re-render (edges follow the node)
        }

        private void GraphCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_dragNode != null)
            {
                GraphCanvas.ReleaseMouseCapture();

                if (!_isDragging)
                {
                    // It was a click, not a drag — navigate to wiki
                    _viewModel.OnNodeClicked(_dragNode.EntityId);
                }

                _dragNode = null;
                _isDragging = false;
            }
        }
    }
}
