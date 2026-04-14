using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
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

        private static readonly Dictionary<string, Color> EntityColors = new()
        {
            { "Character", (Color)ColorConverter.ConvertFromString("#4FC3F7")! },
            { "Location",  (Color)ColorConverter.ConvertFromString("#81C784")! },
            { "Event",     (Color)ColorConverter.ConvertFromString("#FFB74D")! },
            { "Object",    (Color)ColorConverter.ConvertFromString("#CE93D8")! },
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

        private void DrawGraph()
        {
            GraphCanvas.Children.Clear();

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
                StrokeDashArray = edge.Type == "APPEARS_IN" ? new DoubleCollection { 4, 2 } : null
            };
            GraphCanvas.Children.Add(line);

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
                    Padding = new Thickness(5, 2, 5, 2)
                };

                border.Child = new TextBlock
                {
                    Text = edge.Label,
                    Foreground = (Brush)FindResource("PrimaryText"),
                    FontSize = 10,
                    FontWeight = FontWeights.Medium
                };

                border.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                Canvas.SetLeft(border, midX - border.DesiredSize.Width / 2);
                Canvas.SetTop(border, midY - border.DesiredSize.Height / 2);
                GraphCanvas.Children.Add(border);
            }
        }

        private void DrawNode(GraphNode node)
        {
            var color = EntityColors.GetValueOrDefault(node.EntityType, Colors.Gray);
            var brush = new SolidColorBrush(color);

            var ellipse = new Ellipse
            {
                Width = node.Radius * 2,
                Height = node.Radius * 2,
                Fill = brush,
                Stroke = (Brush)FindResource("BorderColor"),
                StrokeThickness = 2,
                Opacity = 0.9
            };

            Canvas.SetLeft(ellipse, node.Center.X - node.Radius);
            Canvas.SetTop(ellipse, node.Center.Y - node.Radius);
            GraphCanvas.Children.Add(ellipse);

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

            stack.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            Canvas.SetLeft(stack, node.Center.X - stack.DesiredSize.Width / 2);
            Canvas.SetTop(stack, node.Center.Y - stack.DesiredSize.Height / 2);
            GraphCanvas.Children.Add(stack);
        }
    }
}
