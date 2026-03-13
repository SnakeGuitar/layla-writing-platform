using Layla.Desktop.Services;
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

namespace Layla.Desktop.Views
{
    public partial class ManuscriptEditorView : Page
    {
        private readonly IManuscriptApiService _apiService;
        private readonly Guid _projectId;
        private Guid? _currentChapterId;
        private bool _isLoaded = false;
        private bool _isSaving = false;
        private System.Threading.Timer _debounceTimer;

        private AdornerLayer _adornerLayer;
        private ImageResizerAdorner _currentAdorner;
        private Image _selectedImage;

        public ManuscriptEditorView(Guid projectId)
        {
            InitializeComponent();
            _projectId = projectId;
            _apiService = new ManuscriptApiService();
            this.Loaded += OnLoaded;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var manuscript = await _apiService.GetManuscriptAsync(_projectId);
                Models.Chapter targetChapter = null;

                if (manuscript != null && manuscript.Chapters.Any())
                {
                    targetChapter = await _apiService.GetChapterAsync(_projectId, manuscript.Chapters.OrderBy(c => c.Order).First().ChapterId);
                }
                else
                {
                    targetChapter = await _apiService.CreateChapterAsync(_projectId, "Chapter 1", string.Empty, 0);
                }

                if (targetChapter != null)
                {
                    _currentChapterId = targetChapter.ChapterId;
                    
                    if (!string.IsNullOrEmpty(targetChapter.Content))
                    {
                        EditorRichTextBox.Document.Blocks.Clear();
                        TextRange textRange = new TextRange(EditorRichTextBox.Document.ContentStart, EditorRichTextBox.Document.ContentEnd);
                        using (MemoryStream ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(targetChapter.Content)))
                        {
                            textRange.Load(ms, DataFormats.Rtf);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load manuscript: {ex.Message}");
            }
            finally
            {
                _isLoaded = true;
            }
        }

        private void EditorRichTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (WordCountTextBlock != null)
            {
                TextRange countRange = new TextRange(EditorRichTextBox.Document.ContentStart, EditorRichTextBox.Document.ContentEnd);
                int wordCount = countRange.Text.Split(new char[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
                WordCountTextBlock.Text = $"{wordCount} word{(wordCount != 1 ? "s" : "")}";
            }

            if (!_isLoaded || _currentChapterId == null) return;

            _debounceTimer?.Change(System.Threading.Timeout.Infinite, 0);
            _debounceTimer = new System.Threading.Timer(async _ => await SaveContentAsync(), null, 1000, System.Threading.Timeout.Infinite);
        }

        private async Task SaveContentAsync()
        {
            if (_isSaving || _currentChapterId == null) return;
            _isSaving = true;

            try
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

                var currentChapter = await _apiService.GetChapterAsync(_projectId, _currentChapterId.Value);
                if (currentChapter != null)
                {
                    await _apiService.UpdateChapterAsync(
                        _projectId, 
                        _currentChapterId.Value, 
                        currentChapter.Title, 
                        rtfContent, 
                        currentChapter.Order
                    );
                    Debug.WriteLine("Saved to API.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Auto-save failed: {ex.Message}");
            }
            finally
            {
                _isSaving = false;
            }
        }

        /// <summary>
        /// Handles the insertion of an image. Wraps the image in a Figure to allow
        /// text to wrap around it naturally.
        /// </summary>
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
                _adornerLayer.Add(_currentAdorner);
            }
        }

        private void ClearImageSelection()
        {
            if (_adornerLayer != null && _currentAdorner != null)
            {
                _adornerLayer.Remove(_currentAdorner);
                _currentAdorner = null;
                _adornerLayer = null;
                _selectedImage = null;
            }
        }
    }
}
