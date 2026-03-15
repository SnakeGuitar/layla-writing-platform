using Layla.Desktop.Services;
using Layla.Desktop.ViewModels;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Threading.Tasks;
using System.Linq;
using System.Diagnostics;
using System;

namespace Layla.Desktop.Views
{
    public partial class ManuscriptEditorView : Page
    {
        private readonly ManuscriptEditorViewModel _viewModel;
        private bool _isLoaded = false;
        private System.Threading.Timer? _debounceTimer;

        private AdornerLayer? _adornerLayer;
        private ImageResizerAdorner? _currentAdorner;
        private Image? _selectedImage;

        public ManuscriptEditorView(Guid projectId)
        {
            InitializeComponent();
            _viewModel = ServiceLocator.GetService<ManuscriptEditorViewModel>() ?? throw new InvalidOperationException("ViewModel not found");
            DataContext = _viewModel;
            _viewModel.Initialize(projectId);
            this.Loaded += OnLoaded;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            await _viewModel.LoadManuscriptCommand.ExecuteAsync(null);
            
            if (_viewModel.CurrentChapter != null && !string.IsNullOrEmpty(_viewModel.CurrentChapter.Content))
            {
                EditorRichTextBox.Document.Blocks.Clear();
                TextRange textRange = new TextRange(EditorRichTextBox.Document.ContentStart, EditorRichTextBox.Document.ContentEnd);
                using (MemoryStream ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(_viewModel.CurrentChapter.Content)))
                {
                    textRange.Load(ms, DataFormats.Rtf);
                }
            }
            _isLoaded = true;
        }

        private void EditorRichTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextRange countRange = new TextRange(EditorRichTextBox.Document.ContentStart, EditorRichTextBox.Document.ContentEnd);
            int wordCount = countRange.Text.Split(new char[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
            _viewModel.UpdateWordCount(wordCount);

            if (!_isLoaded || _viewModel.CurrentChapter == null) return;

            _debounceTimer?.Change(System.Threading.Timeout.Infinite, 0);
            _debounceTimer = new System.Threading.Timer(async _ => await SaveContentInternalAsync(), null, 1000, System.Threading.Timeout.Infinite);
        }

        private async Task SaveContentInternalAsync()
        {
            string rtfContent = string.Empty;

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                TextRange textRange = new TextRange(EditorRichTextBox.Document.ContentStart, EditorRichTextBox.Document.ContentEnd);
                using (MemoryStream ms = new MemoryStream())
                {
                    textRange.Save(ms, DataFormats.Rtf);
                    rtfContent = System.Text.Encoding.UTF8.GetString(ms.ToArray());
                }
            });

            await _viewModel.SaveContentCommand.ExecuteAsync(rtfContent);
        }

        private void InsertImageButton_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Image files (*.png;*.jpeg;*.jpg)|*.png;*.jpeg;*.jpg|All files (*.*)|*.*"
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

                    double initialWidth = bitmap.Width;
                    double initialHeight = bitmap.Height;
                    if (initialWidth > 300)
                    {
                        initialHeight = 300 * (initialHeight / initialWidth);
                        initialWidth = 300;
                    }

                    var image = new Image
                    {
                        Source = bitmap,
                        Width = initialWidth,
                        Height = initialHeight,
                        Stretch = Stretch.Fill,
                        Margin = new Thickness(0)
                    };

                    var blockContainer = new BlockUIContainer(image);
                    var caretPosition = EditorRichTextBox.CaretPosition;

                    Figure figure;

                    if (caretPosition.Paragraph != null)
                    {
                        figure = new Figure(blockContainer, caretPosition);
                    }
                    else
                    {
                        figure = new Figure(blockContainer);
                        var paragraph = new Paragraph();
                        paragraph.Inlines.Add(figure);
                        EditorRichTextBox.Document.Blocks.Add(paragraph);
                    }

                    figure.HorizontalAnchor = FigureHorizontalAnchor.ContentLeft;
                    figure.WrapDirection = WrapDirection.Both;
                    figure.Margin = new Thickness(0, 0, 15, 15);
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
            {
                SelectImage(clickedImage);
            }
            else
            {
                ClearImageSelection();
            }
        }

        private void SelectImage(Image image)
        {
            ClearImageSelection();

            _selectedImage = image;

            if (image.Parent is InlineUIContainer container)
            {
                EditorRichTextBox.Selection.Select(container.ContentStart, container.ContentEnd);
            }
            else if (image.Parent is BlockUIContainer blockContainer)
            {
                EditorRichTextBox.Selection.Select(blockContainer.ContentStart, blockContainer.ContentEnd);
            }

            _adornerLayer = AdornerLayer.GetAdornerLayer(_selectedImage);
            if (_adornerLayer != null)
            {
                _currentAdorner = new ImageResizerAdorner(_selectedImage);
                _adornerLayer?.Add(_currentAdorner);
            }
        }

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
    }
}
