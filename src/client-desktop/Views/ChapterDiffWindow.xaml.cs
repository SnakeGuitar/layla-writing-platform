using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using Layla.Client.Shared.Models;

namespace Layla.Desktop.Views
{
    public partial class ChapterDiffWindow : Window
    {
        public bool Restored { get; private set; }

        public ChapterDiffWindow(ChapterVersionFull selectedVersion, string currentRtf)
        {
            InitializeComponent();

            VersionInfoText.Text = $"Comparing version from {selectedVersion.CreatedAt:yyyy-MM-dd HH:mm:ss} (Created by: {selectedVersion.CreatedBy})";

            // Extract plain text from RTF content for comparison
            string oldText = RtfToPlainText(selectedVersion.Content);
            string newText = RtfToPlainText(currentRtf);

            // Compute visual diffs
            var diffs = WordDiffCompare(oldText, newText);

            // Render to RichTextBox
            RenderDiffs(diffs);
        }

        private static string RtfToPlainText(string rtf)
        {
            if (string.IsNullOrWhiteSpace(rtf)) return string.Empty;
            try
            {
                var doc = new FlowDocument();
                var range = new TextRange(doc.ContentStart, doc.ContentEnd);
                using var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(rtf));
                range.Load(ms, DataFormats.Rtf);
                return range.Text;
            }
            catch
            {
                return rtf;
            }
        }

        private void RenderDiffs(List<DiffWord> diffs)
        {
            DiffParagraph.Inlines.Clear();

            foreach (var diff in diffs)
            {
                var run = new Run(diff.Text + " ");
                if (diff.Type == DiffType.Inserted)
                {
                    run.Foreground = new SolidColorBrush(Color.FromRgb(46, 125, 50)); // Forest Green
                    run.Background = new SolidColorBrush(Color.FromRgb(232, 245, 233)); // Pastel Green
                    run.FontWeight = FontWeights.SemiBold;
                }
                else if (diff.Type == DiffType.Deleted)
                {
                    run.Foreground = new SolidColorBrush(Color.FromRgb(198, 40, 40)); // Red-orange
                    run.Background = new SolidColorBrush(Color.FromRgb(255, 235, 238)); // Pastel Red
                    run.TextDecorations = TextDecorations.Strikethrough;
                }
                else
                {
                    run.Foreground = new SolidColorBrush(Color.FromRgb(224, 224, 224)); // Light grey (dark mode)
                }
                DiffParagraph.Inlines.Add(run);
            }
        }

        private void RestoreButton_Click(object sender, RoutedEventArgs e)
        {
            Restored = true;
            DialogResult = true;
            Close();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        // --- Custom LCS Word Diff Engine ---
        private enum DiffType { Unchanged, Inserted, Deleted }
        private class DiffWord
        {
            public string Text { get; set; } = string.Empty;
            public DiffType Type { get; set; }
        }

        private static List<DiffWord> WordDiffCompare(string oldText, string newText)
        {
            var oldWords = oldText.Split(new[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.None);
            var newWords = newText.Split(new[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.None);

            int n = oldWords.Length;
            int m = newWords.Length;
            int[,] dp = new int[n + 1, m + 1];

            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    if (oldWords[i - 1] == newWords[j - 1])
                        dp[i, j] = dp[i - 1, j - 1] + 1;
                    else
                        dp[i, j] = Math.Max(dp[i - 1, j], dp[i, j - 1]);
                }
            }

            var result = new List<DiffWord>();
            int x = n, y = m;
            while (x > 0 || y > 0)
            {
                if (x > 0 && y > 0 && oldWords[x - 1] == newWords[y - 1])
                {
                    result.Add(new DiffWord { Text = oldWords[x - 1], Type = DiffType.Unchanged });
                    x--; y--;
                }
                else if (y > 0 && (x == 0 || dp[x, y - 1] >= dp[x - 1, y]))
                {
                    result.Add(new DiffWord { Text = newWords[y - 1], Type = DiffType.Inserted });
                    y--;
                }
                else if (x > 0 && (y == 0 || dp[x, y - 1] < dp[x - 1, y]))
                {
                    result.Add(new DiffWord { Text = oldWords[x - 1], Type = DiffType.Deleted });
                    x--;
                }
            }
            result.Reverse();
            return result;
        }
    }
}
