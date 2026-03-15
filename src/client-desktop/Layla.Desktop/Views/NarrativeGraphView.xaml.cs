using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Layla.Desktop.Models;
using Layla.Desktop.ViewModels;
using Layla.Desktop.Services;
using System.Collections.Specialized;
using System.Collections.Generic;

namespace Layla.Desktop.Views
{
    public partial class NarrativeGraphView : Page
    {
        private readonly NarrativeGraphViewModel _viewModel;

        public NarrativeGraphView()
        {
            InitializeComponent();
            _viewModel = ServiceLocator.GetService<NarrativeGraphViewModel>() ?? throw new InvalidOperationException("ViewModel not found");
            DataContext = _viewModel;
            
            DrawGraph();
            
            ((INotifyCollectionChanged)_viewModel.Nodes).CollectionChanged += (s, e) => DrawGraph();
            ((INotifyCollectionChanged)_viewModel.Edges).CollectionChanged += (s, e) => DrawGraph();
        }

        private void DrawGraph()
        {
            GraphCanvas.Children.Clear();

            foreach (var edge in _viewModel.Edges)
            {
                DrawEdge(edge);
            }

            foreach (var node in _viewModel.Nodes)
            {
                DrawNode(node);
            }
        }

        private void DrawEdge(GraphEdge edge)
        {
            var line = new Line
            {
                X1 = edge.Source.Center.X,
                Y1 = edge.Source.Center.Y,
                X2 = edge.Target.Center.X,
                Y2 = edge.Target.Center.Y,
                Stroke = (Brush)FindResource("BorderColor"),
                StrokeThickness = 3
            };
            GraphCanvas.Children.Add(line);

            double midX = (line.X1 + line.X2) / 2;
            double midY = (line.Y1 + line.Y2) / 2;

            var border = new Border
            {
                Background = (Brush)FindResource("ControlBackground"),
                BorderBrush = (Brush)FindResource("BorderColor"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(5),
                Padding = new Thickness(5, 2, 5, 2)
            };

            var textLabel = new TextBlock
            {
                Text = edge.Label,
                Foreground = (Brush)FindResource("PrimaryText"),
                FontSize = 12,
                FontWeight = FontWeights.Medium
            };
            border.Child = textLabel;

            border.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            Canvas.SetLeft(border, midX - (border.DesiredSize.Width / 2));
            Canvas.SetTop(border, midY - (border.DesiredSize.Height / 2));
            
            GraphCanvas.Children.Add(border);
        }

        private void DrawNode(GraphNode node)
        {
            var ellipse = new Ellipse
            {
                Width = node.Radius * 2,
                Height = node.Radius * 2,
                Fill = (Brush)FindResource("AccentColor"),
                Stroke = (Brush)FindResource("BorderColor"),
                StrokeThickness = 3
            };

            Canvas.SetLeft(ellipse, node.Center.X - node.Radius);
            Canvas.SetTop(ellipse, node.Center.Y - node.Radius);
            GraphCanvas.Children.Add(ellipse);

            var stackPanel = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            var nameText = new TextBlock
            {
                Text = node.Name,
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            stackPanel.Children.Add(nameText);

            var subtitleText = new TextBlock
            {
                Text = node.Subtitle,
                Foreground = (Brush)FindResource("SecondaryText"),
                FontSize = 10,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 2, 0, 0)
            };
            stackPanel.Children.Add(subtitleText);

            stackPanel.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            Canvas.SetLeft(stackPanel, node.Center.X - (stackPanel.DesiredSize.Width / 2));
            Canvas.SetTop(stackPanel, node.Center.Y - (stackPanel.DesiredSize.Height / 2));
            
            GraphCanvas.Children.Add(stackPanel);
        }
    }
}
