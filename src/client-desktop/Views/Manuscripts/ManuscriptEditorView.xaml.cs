using Layla.Desktop.Models;
using Layla.Desktop.Services;
using Layla.Desktop.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Layla.Desktop.Views
{
    /// <summary>
    /// Code-behind for <c>ManuscriptEditorView.xaml</c>.
    /// Handles toolbar synchronisation, auto-save debouncing, color picking,
    /// strikethrough toggling, and image insertion/resizing within the
    /// <see cref="System.Windows.Controls.RichTextBox"/>.
    /// </summary>
    public partial class ManuscriptEditorView : Page
    {
        private readonly ManuscriptEditorViewModel _viewModel;
        private bool _isLoaded = false;
        private bool _isReadOnly = false;
        private bool _suppressToolbarSync = false;
        private System.Threading.Timer? _debounceTimer;

        private AdornerLayer? _adornerLayer;
        private ImageResizerAdorner? _currentAdorner;
        private Image? _selectedImage;

        private bool _isPickingFontColor = true;

        private static readonly double[] FontSizes = { 8, 9, 10, 11, 12, 14, 16, 18, 20, 24, 28, 32, 36, 48, 72 };

        private static readonly Color[] PaletteColors = {
            Colors.Black, Colors.DarkSlateGray, Colors.DimGray, Colors.Gray, Colors.DarkGray, Colors.Silver, Colors.LightGray, Colors.White,
            Colors.DarkRed, Colors.Red, Colors.OrangeRed, Colors.Orange, Colors.Gold, Colors.Yellow, Colors.GreenYellow, Colors.LawnGreen,
            Colors.DarkGreen, Colors.Green, Colors.MediumSeaGreen, Colors.Teal, Colors.DarkCyan, Colors.DeepSkyBlue, Colors.DodgerBlue, Colors.Blue,
            Colors.DarkBlue, Colors.Navy, Colors.Indigo, Colors.DarkViolet, Colors.Purple, Colors.MediumOrchid, Colors.HotPink, Colors.Crimson,
            Colors.SaddleBrown, Colors.Sienna, Colors.Chocolate, Colors.Peru, Colors.DarkGoldenrod, Colors.Goldenrod, Colors.Tan, Colors.BlanchedAlmond,
        };

        /// <summary>
        /// Constructs the view, resolves its ViewModel from the DI container,
        /// and subscribes to the content-reload notification.
        /// </summary>
        /// <param name="projectId">The project whose manuscripts should be loaded.</param>
        /// <param name="isReadOnly">
        /// When <c>true</c>, the editor is rendered in read-only mode:
        /// the toolbar is hidden and the <see cref="System.Windows.Controls.RichTextBox"/> rejects input.
        /// </param>
        public ManuscriptEditorView(Guid projectId, bool isReadOnly = false)
        {
            InitializeComponent();
            _isReadOnly = isReadOnly;
            _viewModel = ServiceLocator.GetService<ManuscriptEditorViewModel>() ?? throw new InvalidOperationException("ViewModel not found");
            DataContext = _viewModel;
            _viewModel.Initialize(projectId);
            _viewModel.ContentReloadRequested += OnContentReloadRequested;
            this.Loaded += OnLoaded;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                InitializeFontFamilyComboBox();
                InitializeFontSizeComboBox();

                await _viewModel.LoadManuscriptCommand.ExecuteAsync(null);
                LoadCurrentChapterContent();
                _isLoaded = true;

                if (_isReadOnly)
                {
                    EditorRichTextBox.IsReadOnly = true;
                    foreach (var child in GetVisualChildren<ToolBarTray>(this))
                        child.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"OnLoaded failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Populates the font-family ComboBox with all system fonts sorted alphabetically
        /// and pre-selects "Segoe UI".
        /// </summary>
        private void InitializeFontFamilyComboBox()
        {
            var families = Fonts.SystemFontFamilies.OrderBy(f => f.Source).ToList();
            FontFamilyComboBox.ItemsSource = families;
            var segoe = families.FirstOrDefault(f => f.Source == "Segoe UI");
            FontFamilyComboBox.SelectedItem = segoe ?? families.FirstOrDefault();
        }

        /// <summary>
        /// Populates the font-size ComboBox with the standard size presets and pre-selects 14pt.
        /// </summary>
        private void InitializeFontSizeComboBox()
        {
            FontSizeComboBox.ItemsSource = FontSizes;
            FontSizeComboBox.SelectedItem = 14.0;
        }

        /// <summary>
        /// Handler for <see cref="ManuscriptEditorViewModel.ContentReloadRequested"/>.
        /// Marshals the call to the UI dispatcher before delegating to
        /// <see cref="LoadCurrentChapterContent"/>.
        /// </summary>
        private void OnContentReloadRequested()
        {
            Application.Current.Dispatcher.Invoke(() => LoadCurrentChapterContent());
        }

        /// <summary>
        /// Replaces the editor's <see cref="FlowDocument"/> with the RTF content stored in
        /// <see cref="ManuscriptEditorViewModel.CurrentChapter"/>.
        /// Toolbar sync is suppressed during the load to avoid spurious property changes.
        /// </summary>
        private void LoadCurrentChapterContent()
        {
            _suppressToolbarSync = true;
            try
            {
                if (_viewModel.CurrentChapter != null && !string.IsNullOrEmpty(_viewModel.CurrentChapter.Content))
                {
                    EditorRichTextBox.Document.Blocks.Clear();
                    var textRange = new TextRange(EditorRichTextBox.Document.ContentStart, EditorRichTextBox.Document.ContentEnd);
                    using var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(_viewModel.CurrentChapter.Content));
                    textRange.Load(ms, DataFormats.Rtf);
                }
                else
                {
                    EditorRichTextBox.Document.Blocks.Clear();
                    EditorRichTextBox.Document.Blocks.Add(new Paragraph(new Run("Start writing your amazing story here...")));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load chapter content: {ex.Message}");
                EditorRichTextBox.Document.Blocks.Clear();
                EditorRichTextBox.Document.Blocks.Add(new Paragraph(new Run("")));
            }
            finally
            {
                _suppressToolbarSync = false;
            }
        }

        private async void ManuscriptComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isLoaded) return;
            try
            {
                if (ManuscriptComboBox.SelectedItem is Manuscript selected)
                    await _viewModel.SelectManuscriptItemCommand.ExecuteAsync(selected);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ManuscriptComboBox_SelectionChanged failed: {ex.Message}");
            }
        }

        private async void ChapterListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isLoaded) return;
            try
            {
                if (ChapterListBox.SelectedItem is Chapter selected)
                    await _viewModel.SelectChapterItemCommand.ExecuteAsync(selected);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ChapterListBox_SelectionChanged failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Recalculates the word count and schedules an auto-save after a 1-second
        /// debounce to avoid flooding the API on every keystroke.
        /// </summary>
        private void EditorRichTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_viewModel == null || !_isLoaded || _viewModel.CurrentChapter == null || _suppressToolbarSync) return;

            var countRange = new TextRange(EditorRichTextBox.Document.ContentStart, EditorRichTextBox.Document.ContentEnd);
            int wordCount = countRange.Text.Split(new char[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
            _viewModel.UpdateWordCount(wordCount);

            if (_debounceTimer != null)
            {
                _debounceTimer.Change(1000, System.Threading.Timeout.Infinite);
            }
            else
            {
                _debounceTimer = new System.Threading.Timer(async _ => await SaveContentInternalAsync(), null, 1000, System.Threading.Timeout.Infinite);
            }
        }

        /// <summary>
        /// Extracts the current <see cref="FlowDocument"/> as RTF and delegates the
        /// persistence to <see cref="ManuscriptEditorViewModel.SaveContentCommand"/>.
        /// </summary>
        private async Task SaveContentInternalAsync()
        {
            string rtfContent = string.Empty;

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var textRange = new TextRange(EditorRichTextBox.Document.ContentStart, EditorRichTextBox.Document.ContentEnd);
                using var ms = new MemoryStream();
                textRange.Save(ms, DataFormats.Rtf);
                rtfContent = System.Text.Encoding.UTF8.GetString(ms.ToArray());
            });

            await _viewModel.SaveContentCommand.ExecuteAsync(rtfContent);
        }

        private void EditorRichTextBox_SelectionChanged(object sender, RoutedEventArgs e)
        {
            if (_suppressToolbarSync) return;
            SyncToolbarToSelection();
        }

        /// <summary>
        /// Reads the font family and size of the current selection and reflects them
        /// in the toolbar ComboBoxes. Uses <see cref="_suppressToolbarSync"/> to prevent
        /// the ComboBox change events from re-applying the values to the selection.
        /// </summary>
        private void SyncToolbarToSelection()
        {
            _suppressToolbarSync = true;
            try
            {
                var selection = EditorRichTextBox.Selection;
                if (selection == null) return;

                var fontFamily = selection.GetPropertyValue(TextElement.FontFamilyProperty);
                if (fontFamily is FontFamily ff)
                    FontFamilyComboBox.SelectedItem = ff;

                var fontSize = selection.GetPropertyValue(TextElement.FontSizeProperty);
                if (fontSize is double fs)
                    FontSizeComboBox.Text = fs.ToString();
            }
            finally
            {
                _suppressToolbarSync = false;
            }
        }

        /// <summary>
        /// Applies the selected <see cref="FontFamily"/> to the current editor selection.
        /// </summary>
        private void FontFamilyComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_suppressToolbarSync || !_isLoaded) return;
            if (FontFamilyComboBox.SelectedItem is FontFamily selectedFont)
            {
                EditorRichTextBox.Selection.ApplyPropertyValue(TextElement.FontFamilyProperty, selectedFont);
                EditorRichTextBox.Focus();
            }
        }

        private void FontSizeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFontSizeFromComboBox();
        }

        private void FontSizeComboBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ApplyFontSizeFromComboBox();
                EditorRichTextBox.Focus();
            }
        }

        /// <summary>
        /// Parses the text in the font-size ComboBox and applies it to the current
        /// selection. Accepts values in the range 1–200.
        /// </summary>
        private void ApplyFontSizeFromComboBox()
        {
            if (_suppressToolbarSync || !_isLoaded) return;

            if (double.TryParse(FontSizeComboBox.Text, out double size) && size >= 1 && size <= 200)
                EditorRichTextBox.Selection.ApplyPropertyValue(TextElement.FontSizeProperty, size);
        }

        /// <summary>
        /// Opens the color picker popup in font-color mode.
        /// </summary>
        private void FontColorButton_Click(object sender, RoutedEventArgs e)
        {
            _isPickingFontColor = true;
            ColorPickerTitle.Text = "Font Color";
            ShowColorPicker();
        }

        /// <summary>
        /// Opens the color picker popup in highlight-color mode.
        /// </summary>
        private void HighlightColorButton_Click(object sender, RoutedEventArgs e)
        {
            _isPickingFontColor = false;
            ColorPickerTitle.Text = "Highlight Color";
            ShowColorPicker();
        }

        /// <summary>
        /// Populates <see cref="ColorPalettePanel"/> with swatches from <see cref="PaletteColors"/>
        /// and opens the popup. In highlight mode a "Clear" button is appended so the user
        /// can remove background colour from the selection.
        /// </summary>
        private void ShowColorPicker()
        {
            ColorPalettePanel.Children.Clear();
            foreach (var color in PaletteColors)
            {
                var btn = new Button
                {
                    Width = 20,
                    Height = 20,
                    Margin = new Thickness(2),
                    Background = new SolidColorBrush(color),
                    BorderThickness = new Thickness(1),
                    BorderBrush = Brushes.Gray,
                    Cursor = Cursors.Hand,
                    Tag = color,
                };
                btn.Click += ColorSwatch_Click;
                ColorPalettePanel.Children.Add(btn);
            }

            if (!_isPickingFontColor)
            {
                var clearBtn = new Button
                {
                    Width = 44,
                    Height = 20,
                    Margin = new Thickness(2),
                    Content = "Clear",
                    FontSize = 9,
                    Cursor = Cursors.Hand,
                    Background = Brushes.Transparent,
                    BorderThickness = new Thickness(1),
                    BorderBrush = Brushes.Gray,
                };
                clearBtn.Click += (s, ev) =>
                {
                    EditorRichTextBox.Selection.ApplyPropertyValue(TextElement.BackgroundProperty, null);
                    HighlightColorIndicator.Background = Brushes.Transparent;
                    ColorPickerPopup.IsOpen = false;
                    EditorRichTextBox.Focus();
                };
                ColorPalettePanel.Children.Add(clearBtn);
            }

            ColorPickerPopup.IsOpen = true;
        }

        /// <summary>
        /// Applies the clicked swatch colour to the current selection — as foreground in
        /// font-color mode or as background in highlight mode — and updates the indicator
        /// strip on the corresponding toolbar button.
        /// </summary>
        private void ColorSwatch_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Color color)
            {
                var brush = new SolidColorBrush(color);

                if (_isPickingFontColor)
                {
                    EditorRichTextBox.Selection.ApplyPropertyValue(TextElement.ForegroundProperty, brush);
                    FontColorIndicator.Background = brush;

                }
                else
                {
                    EditorRichTextBox.Selection.ApplyPropertyValue(TextElement.BackgroundProperty, brush);
                    HighlightColorIndicator.Background = brush;
                    HighlightPreviewBrush.Color = color;
                }

                ColorPickerPopup.IsOpen = false;
                EditorRichTextBox.Focus();
            }
        }

        /// <summary>
        /// Toggles strikethrough on the current selection. If strikethrough is already
        /// present it is removed; otherwise it is applied.
        /// </summary>
        private void StrikethroughButton_Click(object sender, RoutedEventArgs e)
        {
            var selection = EditorRichTextBox.Selection;
            if (selection.IsEmpty) return;

            var currentDecorations = selection.GetPropertyValue(Inline.TextDecorationsProperty);
            if (currentDecorations is TextDecorationCollection decorations && decorations.Contains(TextDecorations.Strikethrough[0]))
            {
                var newDecorations = new TextDecorationCollection(decorations.Where(d => !TextDecorations.Strikethrough.Contains(d)));
                selection.ApplyPropertyValue(Inline.TextDecorationsProperty, newDecorations);
            }
            else
            {
                selection.ApplyPropertyValue(Inline.TextDecorationsProperty, TextDecorations.Strikethrough);
            }
            EditorRichTextBox.Focus();
        }

        /// <summary>
        /// Opens a file-picker dialog and inserts the chosen image inline at the caret position.
        /// Images wider than 600 px are scaled down proportionally.
        /// </summary>
        private void InsertImageButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Image files (*.png;*.jpeg;*.jpg;*.gif;*.bmp;*.webp)|*.png;*.jpeg;*.jpg;*.gif;*.bmp;*.webp|All files (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(openFileDialog.FileName);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();

                    double initialWidth = bitmap.PixelWidth;
                    double initialHeight = bitmap.PixelHeight;

                    double maxWidth = 600;
                    if (initialWidth > maxWidth)
                    {
                        initialHeight = maxWidth * (initialHeight / initialWidth);
                        initialWidth = maxWidth;
                    }

                    var image = new Image
                    {
                        Source = bitmap,
                        Width = initialWidth,
                        Height = initialHeight,
                        Stretch = Stretch.Fill,
                        Margin = new Thickness(0),
                        ToolTip = System.IO.Path.GetFileName(openFileDialog.FileName),
                    };

                    _ = new InlineUIContainer(image, EditorRichTextBox.CaretPosition);
                    EditorRichTextBox.Focus();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Could not insert image: {ex.Message}");
                }
            }
        }

        private void EditorRichTextBox_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var position = e.GetPosition(EditorRichTextBox);
            var hitTestResult = VisualTreeHelper.HitTest(EditorRichTextBox, position);

            if (hitTestResult?.VisualHit is Image clickedImage)
                SelectImage(clickedImage);
            else
                ClearImageSelection();
        }

        /// <summary>
        /// Selects the clicked <paramref name="image"/> element, adjusts the text selection
        /// to encompass it, and attaches an <see cref="ImageResizerAdorner"/>.
        /// </summary>
        private void SelectImage(Image image)
        {
            ClearImageSelection();
            _selectedImage = image;

            if (image.Parent is InlineUIContainer container)
                EditorRichTextBox.Selection.Select(container.ContentStart, container.ContentEnd);
            else if (image.Parent is BlockUIContainer blockContainer)
                EditorRichTextBox.Selection.Select(blockContainer.ContentStart, blockContainer.ContentEnd);

            _adornerLayer = AdornerLayer.GetAdornerLayer(_selectedImage);
            if (_adornerLayer != null)
            {
                _currentAdorner = new ImageResizerAdorner(_selectedImage);
                _adornerLayer.Add(_currentAdorner);
            }
        }

        /// <summary>
        /// Removes the <see cref="ImageResizerAdorner"/> from the currently selected image
        /// and clears the selection state.
        /// </summary>
        private void ClearImageSelection()
        {
            if (_adornerLayer != null && _currentAdorner != null)
            {
                _adornerLayer.Remove(_currentAdorner);
                _currentAdorner = null;
                _adornerLayer = null;
            }
            _selectedImage = null;
        }

        /// <summary>
        /// When the user clicks a mention in the context panel, navigate to the
        /// corresponding wiki entry in the Wiki tab.
        /// </summary>
        private void MentionsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ListBox lb && lb.SelectedItem is Mention mention)
            {
                WorkspaceMediator.RequestNavigateToWikiEntry(mention.EntityId);
                lb.SelectedItem = null; // reset so the same item can be clicked again
            }
        }

        /// <summary>
        /// Called by <see cref="Views.WorkspaceView"/> when cross-tab navigation requests
        /// opening a specific chapter (e.g. from a wiki appearance click).
        /// </summary>
        public async void NavigateToChapter(string manuscriptId, string chapterId)
        {
            try
            {
                await _viewModel.NavigateToChapterAsync(manuscriptId, chapterId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"NavigateToChapter failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Recursively enumerates all visual-tree descendants of <paramref name="parent"/>
        /// that are assignable to <typeparamref name="T"/>.
        /// </summary>
        private static IEnumerable<T> GetVisualChildren<T>(DependencyObject parent) where T : DependencyObject
        {
            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typedChild)
                    yield return typedChild;
                foreach (var grandChild in GetVisualChildren<T>(child))
                    yield return grandChild;
            }
        }
    }
}
