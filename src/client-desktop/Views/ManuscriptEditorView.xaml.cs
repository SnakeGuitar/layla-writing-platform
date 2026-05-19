using Layla.Desktop.Models;
using Layla.Desktop.Services;
using Layla.Desktop.ViewModels;
using Layla.Client.Shared.Models;
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
        private CollaboratorCursorAdorner? _cursorAdorner;

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
            _viewModel.EvictedFromProject += OnEvictedFromProject;
            _viewModel.WikiTokenizerUpdated += OnWikiTokenizerUpdated;
            _viewModel.RequestShowDiff += OnRequestShowDiff;
            _viewModel.CollaboratorCursorMoved += OnCollaboratorCursorMoved;
            _viewModel.RequestFlushAction = async () => await FlushPendingSavesAsync();
            this.Loaded += OnLoaded;
            this.Unloaded += OnUnloaded;

            // Ctrl+S triggers the manual save. The KeyBinding lives on the page
            // so the shortcut works no matter which child control has focus.
            var saveBinding = new KeyBinding(
                new RelayCommandWrapper(async () => await ManualSaveAsync()),
                Key.S, ModifierKeys.Control);
            this.InputBindings.Add(saveBinding);
        }

        // Tiny ICommand adapter so we can wire Ctrl+S via KeyBinding without
        // pulling in CommunityToolkit.Mvvm's RelayCommand for a one-off use.
        private sealed class RelayCommandWrapper : System.Windows.Input.ICommand
        {
            private readonly Func<Task> _action;
            public RelayCommandWrapper(Func<Task> action) => _action = action;
            public event EventHandler? CanExecuteChanged { add { } remove { } }
            public bool CanExecute(object? parameter) => true;
            public async void Execute(object? parameter) => await _action();
        }

        private async void OnUnloaded(object sender, RoutedEventArgs e)
        {
            // Belt-and-suspenders: a Page hosted inside a Frame does not always
            // get its Unloaded raised reliably when the OUTER page navigates
            // away — that is why WorkspaceView ALSO explicitly calls
            // FlushPendingSavesAsync before navigating. Keep this path so
            // direct navigation away from the editor still flushes.
            try
            {
                await FlushPendingSavesAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Unloaded flush failed: {ex.Message}");
            }

            _debounceTimer?.Dispose();
            _debounceTimer = null;
            _viewModel.ContentReloadRequested -= OnContentReloadRequested;
            _viewModel.EvictedFromProject -= OnEvictedFromProject;
            _viewModel.WikiTokenizerUpdated -= OnWikiTokenizerUpdated;
            _viewModel.RequestShowDiff -= OnRequestShowDiff;
            _viewModel.CollaboratorCursorMoved -= OnCollaboratorCursorMoved;
            _viewModel.RequestFlushAction = null;
            
            if (_cursorAdorner != null)
            {
                var layer = AdornerLayer.GetAdornerLayer(EditorRichTextBox);
                layer?.Remove(_cursorAdorner);
                _cursorAdorner = null;
            }
        }

        /// <summary>
        /// Forces a final write of the current editor content to the server.
        /// Called by the host workspace BEFORE it navigates away so that the
        /// user's typing survives even when the Unloaded chain on the nested
        /// Page does not fire (a known WPF Frame edge case). Idempotent —
        /// safe to call repeatedly.
        /// </summary>
        public async Task FlushPendingSavesAsync()
        {
            if (!_isLoaded || _isReadOnly || _viewModel.CurrentChapter == null)
                return;

            string rtf;
            string plainText;
            try
            {
                var textRange = new TextRange(
                    EditorRichTextBox.Document.ContentStart,
                    EditorRichTextBox.Document.ContentEnd);
                plainText = textRange.Text;
                using var ms = new MemoryStream();
                textRange.Save(ms, DataFormats.Rtf);
                rtf = System.Text.Encoding.UTF8.GetString(ms.ToArray());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FlushPendingSavesAsync: extract RTF failed: {ex.Message}");
                return;
            }

            await _viewModel.FlushSaveAsync(new SaveContentArgs { RtfContent = rtf, PlainText = plainText });
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

                    AddManuscriptButton.Visibility = Visibility.Collapsed;
                    DeleteManuscriptButton.Visibility = Visibility.Collapsed;
                    AddChapterButton.Visibility = Visibility.Collapsed;
                }

                var layer = AdornerLayer.GetAdornerLayer(EditorRichTextBox);
                if (layer != null)
                {
                    _cursorAdorner = new CollaboratorCursorAdorner(EditorRichTextBox);
                    layer.Add(_cursorAdorner);
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

        private void OnCollaboratorCursorMoved(string userId, int offset)
        {
            Application.Current.Dispatcher.InvokeAsync(() => 
            {
                _cursorAdorner?.UpdateCursor(userId, offset);
            });
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
                    EditorRichTextBox.Document.Blocks.Add(new Paragraph(new Run("")));
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

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            await ManualSaveAsync();
        }

        /// <summary>
        /// User-triggered save. Bypasses the 1 s auto-save debounce, waits for
        /// any in-flight auto-save to finish, then forces a fresh write of the
        /// current editor content. Surfaces the outcome (success / offline /
        /// error) through StatusMessage so the user gets immediate feedback.
        /// </summary>
        private async Task ManualSaveAsync()
        {
            if (!_isLoaded || _isReadOnly || _viewModel.CurrentChapter == null)
            {
                _viewModel.StatusMessage = "Nothing to save yet — load a chapter first.";
                return;
            }

            _viewModel.StatusMessage = "Saving...";

            try
            {
                await FlushPendingSavesAsync();

                if (_viewModel.HasUnsavedOfflineChanges)
                {
                    _viewModel.StatusMessage = "Saved locally (offline). The worldbuilding service rejected or dropped the request — content will sync the next time you open this chapter while the server is up.";
                }
                else
                {
                    _viewModel.StatusMessage = $"Saved \"{_viewModel.CurrentChapter.Title}\" ✓";
                }
            }
            catch (Exception ex)
            {
                _viewModel.StatusMessage = $"Save failed: {ex.Message}";
            }
        }

        private async void DeleteChapterButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not Chapter chapter) return;

            // Deleting the last chapter is allowed — the ViewModel auto-creates
            // a fresh empty Chapter 1 so the manuscript never ends up unusable.
            var lastChapterNote = _viewModel.CurrentChapters.Count <= 1
                ? "\n\nThis is the only chapter; a new empty Chapter 1 will be created."
                : string.Empty;

            var confirm = MessageBox.Show(
                $"Delete the chapter \"{chapter.Title}\"?{lastChapterNote}\n\nThis cannot be undone.",
                "Delete chapter",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                MessageBoxResult.No);

            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                await _viewModel.DeleteChapterCommand.ExecuteAsync(chapter);
            }
            catch (Exception ex)
            {
                _viewModel.StatusMessage = $"Delete failed: {ex.Message}";
            }
        }

        private async void DeleteManuscriptButton_Click(object sender, RoutedEventArgs e)
        {
            var target = _viewModel.SelectedManuscript;
            if (target == null)
            {
                _viewModel.StatusMessage = "No manuscript selected.";
                return;
            }

            // Deleting the last manuscript is allowed — the ViewModel auto-
            // creates a fresh empty Manuscript 1 with Chapter 1 so the editor
            // always has somewhere to write.
            var lastNote = _viewModel.Manuscripts.Count <= 1
                ? "\n\nThis is the only manuscript; a new empty Manuscript 1 will be created in its place."
                : string.Empty;

            var confirm = MessageBox.Show(
                $"Permanently delete the manuscript \"{target.Title}\" and all of its chapters?{lastNote}\n\nThis cannot be undone.",
                "Delete manuscript",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                MessageBoxResult.No);

            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                await _viewModel.DeleteManuscriptCommand.ExecuteAsync(target);
            }
            catch (Exception ex)
            {
                _viewModel.StatusMessage = $"Delete failed: {ex.Message}";
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
        private async void EditorRichTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_viewModel == null || !_isLoaded || _viewModel.CurrentChapter == null || _suppressToolbarSync) return;

            var countRange = new TextRange(EditorRichTextBox.Document.ContentStart, EditorRichTextBox.Document.ContentEnd);
            string plainText = countRange.Text;
            int wordCount = plainText.Split(new char[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
            _viewModel.UpdateWordCount(wordCount);

            // Debounced/background visual tokenization
            await Dispatcher.InvokeAsync(() => RunVisualTokenization(), System.Windows.Threading.DispatcherPriority.Background);

            // Broadcast cursor position
            try
            {
                int cursorOffset = EditorRichTextBox.Document.ContentStart.GetOffsetToPosition(EditorRichTextBox.CaretPosition);
                await _viewModel.BroadcastCursorPositionAsync(cursorOffset);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to broadcast cursor: {ex.Message}");
            }

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
        /// Extracts the current <see cref="FlowDocument"/> as RTF and plain text, and delegates the
        /// persistence to <see cref="ManuscriptEditorViewModel.SaveContentCommand"/>.
        /// </summary>
        private async Task SaveContentInternalAsync()
        {
            string rtfContent = string.Empty;
            string plainText = string.Empty;

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var textRange = new TextRange(EditorRichTextBox.Document.ContentStart, EditorRichTextBox.Document.ContentEnd);
                plainText = textRange.Text;
                using var ms = new MemoryStream();
                textRange.Save(ms, DataFormats.Rtf);
                rtfContent = System.Text.Encoding.UTF8.GetString(ms.ToArray());
            });

            await _viewModel.SaveContentCommand.ExecuteAsync(new SaveContentArgs { RtfContent = rtfContent, PlainText = plainText });
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

        private async void CreateMilestoneButton_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.CurrentChapter == null) return;
            var args = new SaveContentArgs { RtfContent = GetRtfContent(), PlainText = GetPlainText() };
            await _viewModel.SaveMilestoneAsync(args);
        }

        /// <summary>
        /// Reads the <see cref="FlowDocument"/> and returns it serialized as an RTF string.
        /// </summary>
        private string GetRtfContent()
        {
            var textRange = new TextRange(EditorRichTextBox.Document.ContentStart, EditorRichTextBox.Document.ContentEnd);
            using var ms = new MemoryStream();
            textRange.Save(ms, DataFormats.Rtf);
            return System.Text.Encoding.UTF8.GetString(ms.ToArray());
        }

        private string GetPlainText()
        {
            return new TextRange(EditorRichTextBox.Document.ContentStart, EditorRichTextBox.Document.ContentEnd).Text;
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

            var clickPointer = EditorRichTextBox.GetPositionFromPoint(position, snapToText: true);
            if (clickPointer != null)
            {
                var parent = clickPointer.Parent;
                while (parent != null && !(parent is Hyperlink) && !(parent is FlowDocument) && !(parent is RichTextBox))
                {
                    if (parent is FrameworkContentElement fce)
                        parent = fce.Parent;
                    else if (parent is FrameworkElement fe)
                        parent = fe.Parent;
                    else
                        break;
                }

                if (parent is Hyperlink hyperlink && hyperlink.NavigateUri != null)
                {
                    var uriString = hyperlink.NavigateUri.ToString();
                    if (uriString.StartsWith("wiki://"))
                    {
                        var entityId = uriString.Substring("wiki://".Length).TrimEnd('/');
                        
                        // Create a floating popup menu/preview
                        var popup = new System.Windows.Controls.Primitives.Popup
                        {
                            Placement = System.Windows.Controls.Primitives.PlacementMode.MousePoint,
                            AllowsTransparency = true,
                            StaysOpen = false,
                            PopupAnimation = System.Windows.Controls.Primitives.PopupAnimation.Fade
                        };

                        var mainBorder = new Border
                        {
                            Background = (Brush)FindResource("ControlBackground"),
                            BorderBrush = (Brush)FindResource("AccentColor"),
                            BorderThickness = new Thickness(1),
                            CornerRadius = new CornerRadius(8),
                            Padding = new Thickness(12),
                            Width = 200,
                            Effect = new System.Windows.Media.Effects.DropShadowEffect
                            {
                                Color = Colors.Black,
                                Direction = 315,
                                ShadowDepth = 2,
                                Opacity = 0.25,
                                BlurRadius = 6
                            }
                        };

                        var stack = new StackPanel();

                        // Get text of mention
                        var titleText = new TextBlock
                        {
                            Text = hyperlink.Inlines.FirstOrDefault() is Run r ? r.Text : "Wiki Entry",
                            FontWeight = FontWeights.Bold,
                            FontSize = 14,
                            Foreground = (Brush)FindResource("PrimaryText"),
                            Margin = new Thickness(0, 0, 0, 2)
                        };
                        stack.Children.Add(titleText);

                        // Try to extract entity type from ToolTip content
                        string typeStr = "WIKI ENTRY";
                        if (hyperlink.ToolTip is ToolTip tt && tt.Content is Border b && b.Child is StackPanel sp && sp.Children.Count > 1 && sp.Children[1] is TextBlock tbType)
                        {
                            typeStr = tbType.Text;
                        }
                        var typeText = new TextBlock
                        {
                            Text = typeStr,
                            FontSize = 10,
                            Foreground = (Brush)FindResource("AccentColor"),
                            FontWeight = FontWeights.SemiBold,
                            Margin = new Thickness(0, 0, 0, 12)
                        };
                        stack.Children.Add(typeText);

                        // Open Button
                        var openButton = new Button
                        {
                            Content = "📖 Open Full Wiki",
                            Background = (Brush)FindResource("AccentColor"),
                            Foreground = Brushes.White,
                            BorderThickness = new Thickness(0),
                            Padding = new Thickness(8, 6, 8, 6),
                            Cursor = Cursors.Hand,
                            Margin = new Thickness(0, 0, 0, 6),
                            FontWeight = FontWeights.SemiBold
                        };
                        openButton.Click += (s, ev) =>
                        {
                            WorkspaceMediator.RequestNavigateToWikiEntry(entityId);
                            popup.IsOpen = false;
                        };
                        stack.Children.Add(openButton);

                        // Close Button
                        var closeButton = new Button
                        {
                            Content = "Close",
                            Background = Brushes.Transparent,
                            Foreground = (Brush)FindResource("SecondaryText"),
                            BorderBrush = (Brush)FindResource("BorderColor"),
                            BorderThickness = new Thickness(1),
                            Padding = new Thickness(8, 4, 8, 4),
                            Cursor = Cursors.Hand
                        };
                        closeButton.Click += (s, ev) =>
                        {
                            popup.IsOpen = false;
                        };
                        stack.Children.Add(closeButton);

                        mainBorder.Child = stack;
                        popup.Child = mainBorder;

                        popup.IsOpen = true;

                        e.Handled = true;
                        return;
                    }
                }
            }

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

        private IEnumerable<Run> GetAllRuns(FlowDocument document)
        {
            return GetRunsFromBlocks(document.Blocks);
        }

        private IEnumerable<Run> GetRunsFromBlocks(BlockCollection blocks)
        {
            foreach (var block in blocks)
            {
                if (block is Paragraph p)
                {
                    foreach (var run in GetRunsFromInlines(p.Inlines)) yield return run;
                }
                else if (block is List list)
                {
                    foreach (var listItem in list.ListItems)
                    {
                        foreach (var run in GetRunsFromBlocks(listItem.Blocks)) yield return run;
                    }
                }
            }
        }

        private IEnumerable<Run> GetRunsFromInlines(InlineCollection inlines)
        {
            foreach (var inline in inlines)
            {
                if (inline is Run run) yield return run;
                else if (inline is Span span)
                {
                    foreach (var childRun in GetRunsFromInlines(span.Inlines)) yield return childRun;
                }
            }
        }

        /// <summary>
        /// Scans all text runs in the FlowDocument and programmatically injects
        /// interactive wiki hyperlinks for matches detected by the Aho-Corasick tokenizer.
        /// </summary>
        private void RunVisualTokenization()
        {
            if (_suppressToolbarSync || !_isLoaded || _viewModel.CurrentChapter == null) return;

            _suppressToolbarSync = true;
            try
            {
                var runs = GetAllRuns(EditorRichTextBox.Document).ToList();
                var textRange = new TextRange(EditorRichTextBox.Document.ContentStart, EditorRichTextBox.Document.ContentEnd);
                var detectable = _viewModel.Tokenizer.FindMentions(textRange.Text);

                foreach (var run in runs)
                {
                    // Skip if the run is already nested under a Hyperlink
                    if (run.Parent is Hyperlink) continue;

                    string text = run.Text;
                    foreach (var match in detectable)
                    {
                        int index = text.IndexOf(match.MatchedText, StringComparison.OrdinalIgnoreCase);
                        if (index >= 0)
                        {
                            var pointer = run.ContentStart.GetPositionAtOffset(index);
                            var endPointer = pointer.GetPositionAtOffset(match.MatchedText.Length);

                            var tooltipBorder = new Border
                            {
                                Background = (Brush)FindResource("ControlBackground"),
                                BorderBrush = (Brush)FindResource("AccentColor"),
                                BorderThickness = new Thickness(1),
                                CornerRadius = new CornerRadius(6),
                                Padding = new Thickness(12),
                                MaxWidth = 250
                            };
                            var tooltipStack = new StackPanel();
                            tooltipStack.Children.Add(new TextBlock 
                            { 
                                Text = match.MatchedText, 
                                FontWeight = FontWeights.Bold, 
                                FontSize = 14, 
                                Foreground = (Brush)FindResource("PrimaryText")
                            });
                            tooltipStack.Children.Add(new TextBlock 
                            { 
                                Text = match.EntityType.ToUpper(), 
                                FontSize = 10, 
                                Foreground = (Brush)FindResource("AccentColor"),
                                Margin = new Thickness(0, 2, 0, 8),
                                FontWeight = FontWeights.SemiBold
                            });
                            tooltipStack.Children.Add(new TextBlock 
                            { 
                                Text = "Click to view full details in the Wiki.", 
                                Foreground = (Brush)FindResource("SecondaryText"),
                                FontSize = 11,
                                TextWrapping = TextWrapping.Wrap
                            });
                            tooltipBorder.Child = tooltipStack;

                            var hyperlink = new Hyperlink(pointer, endPointer)
                            {
                                NavigateUri = new Uri($"wiki://{match.EntityId}"),
                                ToolTip = new ToolTip 
                                { 
                                    Content = tooltipBorder,
                                    Background = Brushes.Transparent,
                                    BorderThickness = new Thickness(0),
                                    HasDropShadow = true
                                }
                            };
                            hyperlink.RequestNavigate += (s, e) =>
                            {
                                WorkspaceMediator.RequestNavigateToWikiEntry(match.EntityId);
                            };

                            // Apply premium styled aesthetic
                            hyperlink.Foreground = new SolidColorBrush(Color.FromRgb(103, 58, 183)); // Modern Slate Indigo
                            hyperlink.TextDecorations = TextDecorations.Underline;
                            hyperlink.Cursor = Cursors.Hand;
                            
                            // Only process the first match per run to avoid concurrent modification issues
                            // If there are more matches, they'll be picked up in subsequent debounced passes.
                            break; 
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Tokenization error: {ex.Message}");
            }
            finally
            {
                _suppressToolbarSync = false;
            }
        }

        private void OnWikiTokenizerUpdated()
        {
            Dispatcher.Invoke(() => RunVisualTokenization());
        }

        private void OnEvictedFromProject(Guid projectId)
        {
            Application.Current.Dispatcher.Invoke(async () =>
            {
                MessageBox.Show("Your collaborator access to this project has been revoked by the owner.", "Access Revoked", MessageBoxButton.OK, MessageBoxImage.Error);

                try
                {
                    await FlushPendingSavesAsync();
                }
                catch { }

                var parentPage = GetParentPage(this);
                if (parentPage?.NavigationService != null)
                {
                    parentPage.NavigationService.Navigate(new ProjectListView());
                }
                else
                {
                    this.NavigationService?.Navigate(new ProjectListView());
                }
            });
        }

        private void OnRequestShowDiff(ChapterVersionFull fullVersion)
        {
            Dispatcher.Invoke(async () =>
            {
                try
                {
                    // Extract current editor RTF content
                    string currentRtf = string.Empty;
                    var textRange = new TextRange(EditorRichTextBox.Document.ContentStart, EditorRichTextBox.Document.ContentEnd);
                    using (var ms = new MemoryStream())
                    {
                        textRange.Save(ms, DataFormats.Rtf);
                        currentRtf = System.Text.Encoding.UTF8.GetString(ms.ToArray());
                    }

                    var diffWindow = new ChapterDiffWindow(fullVersion, currentRtf);
                    var parentWindow = Window.GetWindow(this);
                    if (parentWindow != null)
                    {
                        diffWindow.Owner = parentWindow;
                    }

                    if (diffWindow.ShowDialog() == true && diffWindow.Restored)
                    {
                        // User clicked "Restore to this Version" inside the diff dialog
                        await _viewModel.RestoreVersionCommand.ExecuteAsync(fullVersion);
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show(
                        $"Error displaying comparison window:\n\n{ex.Message}\n\nDetails:\n{ex.StackTrace}",
                        "Comparison Window Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error
                    );
                }
            });
        }

        private Page? GetParentPage(DependencyObject child)
        {
            var parent = VisualTreeHelper.GetParent(child);
            if (parent == null) return null;
            if (parent is Page page) return page;
            return GetParentPage(parent);
        }
    }
}
