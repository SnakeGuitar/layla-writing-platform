using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace Layla.Desktop.Views
{
    public class CollaboratorCursorAdorner : Adorner
    {
        private readonly RichTextBox _richTextBox;
        private readonly Dictionary<string, CursorInfo> _cursors = new();

        private class CursorInfo
        {
            public int Offset { get; set; }
            public Brush Color { get; set; } = Brushes.Red;
            public string Name { get; set; } = string.Empty;
            public DateTime LastMoved { get; set; }
        }

        private static readonly Brush[] _colors = new Brush[]
        {
            Brushes.Red, Brushes.Blue, Brushes.Green, Brushes.Orange, Brushes.Purple, Brushes.Teal
        };

        public CollaboratorCursorAdorner(RichTextBox adornedElement) : base(adornedElement)
        {
            _richTextBox = adornedElement;
            IsHitTestVisible = false;
        }

        public void UpdateCursor(string userId, int offset)
        {
            if (!_cursors.TryGetValue(userId, out var info))
            {
                var color = _colors[_cursors.Count % _colors.Length];
                info = new CursorInfo { Color = color, Name = userId.Substring(0, Math.Min(userId.Length, 5)) };
                _cursors[userId] = info;
            }
            info.Offset = offset;
            info.LastMoved = DateTime.UtcNow;
            
            // Clean up stale cursors
            var threshold = DateTime.UtcNow.AddMinutes(-5);
            var staleKeys = _cursors.Where(kvp => kvp.Value.LastMoved < threshold).Select(kvp => kvp.Key).ToList();
            foreach (var key in staleKeys) _cursors.Remove(key);

            InvalidateVisual();
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            foreach (var kvp in _cursors)
            {
                var info = kvp.Value;
                try
                {
                    var pointer = _richTextBox.Document.ContentStart.GetPositionAtOffset(info.Offset);
                    if (pointer == null) continue;

                    var rect = pointer.GetCharacterRect(LogicalDirection.Forward);
                    if (rect.IsEmpty) continue;

                    var startPoint = rect.TopLeft;
                    var endPoint = rect.BottomLeft;

                    // Draw vertical line for cursor
                    var pen = new Pen(info.Color, 2);
                    drawingContext.DrawLine(pen, startPoint, endPoint);

                    // Draw name tag above cursor
                    var typeface = new Typeface("Segoe UI");
                    var formattedText = new FormattedText(
                        info.Name,
                        System.Globalization.CultureInfo.InvariantCulture,
                        FlowDirection.LeftToRight,
                        typeface,
                        10,
                        Brushes.White,
                        VisualTreeHelper.GetDpi(this).PixelsPerDip);

                    var tagRect = new Rect(startPoint.X, startPoint.Y - 14, formattedText.Width + 4, formattedText.Height + 2);
                    drawingContext.DrawRectangle(info.Color, null, tagRect);
                    drawingContext.DrawText(formattedText, new Point(startPoint.X + 2, startPoint.Y - 14));
                }
                catch 
                { 
                    // Offset might be out of bounds if text was deleted locally before update arrives
                }
            }
        }
    }
}
