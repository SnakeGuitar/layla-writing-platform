using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Layla.Desktop.Models;
using Layla.Desktop.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Layla.Desktop.ViewModels
{
    /// <summary>
    /// ViewModel for the manuscript editor.
    /// Manages the list of manuscripts, the chapter navigation for the selected manuscript,
    /// and the currently loaded chapter content. Exposes CRUD commands for both manuscripts
    /// and chapters, and fires <see cref="ContentReloadRequested"/> when the view should
    /// replace the RTF content in the editor.
    /// </summary>
    public partial class ManuscriptEditorViewModel : ObservableObject
    {
        private readonly IManuscriptApiService _apiService;
        private Guid _projectId;

        /// <summary><c>true</c> while the initial manuscript list is being fetched.</summary>
        [ObservableProperty]
        private bool _isLoading;

        /// <summary><c>true</c> while a chapter auto-save is in progress.</summary>
        [ObservableProperty]
        private bool _isSaving;

        /// <summary>Formatted word count shown in the editor footer (e.g. "342 words").</summary>
        [ObservableProperty]
        private string _wordCountText = "0 words";

        /// <summary>The chapter whose content is currently displayed in the editor, with full RTF.</summary>
        [ObservableProperty]
        private Chapter? _currentChapter;

        /// <summary>The manuscript currently selected in the sidebar ComboBox.</summary>
        [ObservableProperty]
        private Manuscript? _selectedManuscript;

        /// <summary>The chapter currently selected in the sidebar ListBox.</summary>
        [ObservableProperty]
        private Chapter? _selectedChapterItem;

        /// <summary>All manuscripts belonging to the current project, ordered by <see cref="Manuscript.Order"/>.</summary>
        public ObservableCollection<Manuscript> Manuscripts { get; } = new();

        /// <summary>Chapters of <see cref="SelectedManuscript"/>, ordered by <see cref="Chapter.Order"/>.</summary>
        public ObservableCollection<Chapter> CurrentChapters { get; } = new();

        /// <summary>Wiki entities detected in the currently loaded chapter.</summary>
        public ObservableCollection<Mention> CurrentMentions { get; } = new();

        /// <summary>
        /// Raised on the calling thread when the view should reload the RTF content
        /// from <see cref="CurrentChapter"/> into the editor control.
        /// </summary>
        public event Action? ContentReloadRequested;

        /// <summary>Initialises the ViewModel via dependency injection.</summary>
        public ManuscriptEditorViewModel(IManuscriptApiService apiService)
        {
            _apiService = apiService;
        }

        /// <summary>
        /// Sets the project context. Must be called before any command is executed.
        /// </summary>
        public void Initialize(Guid projectId)
        {
            _projectId = projectId;
        }

        /// <summary>
        /// Fetches all manuscripts from the API and populates <see cref="Manuscripts"/>.
        /// If the project has no manuscripts, creates a default one with a single chapter.
        /// </summary>
        [RelayCommand]
        private async Task LoadManuscriptAsync()
        {
            IsLoading = true;
            try
            {
                var manuscripts = await _apiService.GetManuscriptsByProjectAsync(_projectId);

                Manuscripts.Clear();
                CurrentChapters.Clear();

                if (manuscripts != null && manuscripts.Count > 0)
                {
                    foreach (var m in manuscripts.OrderBy(m => m.Order))
                        Manuscripts.Add(m);

                    await SelectManuscriptAsync(Manuscripts.First());
                }
                else
                {
                    var newMs = await _apiService.CreateManuscriptAsync(_projectId, "Manuscript 1", 0);
                    if (newMs != null)
                    {
                        var firstChapter = await _apiService.CreateChapterAsync(_projectId, newMs.ManuscriptId, "Chapter 1", string.Empty, 0);
                        if (firstChapter != null)
                            newMs.Chapters.Add(firstChapter);

                        Manuscripts.Add(newMs);
                        await SelectManuscriptAsync(newMs);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load manuscripts: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Switches the active manuscript, refreshes <see cref="CurrentChapters"/>,
        /// and selects the first chapter.
        /// </summary>
        private async Task SelectManuscriptAsync(Manuscript manuscript)
        {
            SelectedManuscript = manuscript;
            CurrentChapters.Clear();

            var fresh = await _apiService.GetManuscriptAsync(_projectId, manuscript.ManuscriptId);
            if (fresh != null)
            {
                foreach (var ch in fresh.Chapters.OrderBy(c => c.Order))
                    CurrentChapters.Add(ch);
            }

            if (CurrentChapters.Any())
                await SelectChapterAsync(CurrentChapters.First());
            else
                CurrentChapter = null;
        }

        /// <summary>
        /// Command counterpart of <see cref="SelectManuscriptAsync"/> — no-op when the
        /// requested manuscript is already selected.
        /// </summary>
        [RelayCommand]
        public async Task SelectManuscriptItemAsync(Manuscript? manuscript)
        {
            if (manuscript == null || manuscript.ManuscriptId == SelectedManuscript?.ManuscriptId) return;
            await SelectManuscriptAsync(manuscript);
        }

        /// <summary>
        /// Fetches the full chapter content from the API and sets <see cref="CurrentChapter"/>,
        /// then raises <see cref="ContentReloadRequested"/> so the view reloads the editor.
        /// </summary>
        private async Task SelectChapterAsync(Chapter chapterIndex)
        {
            if (SelectedManuscript == null) return;

            var fullChapter = await _apiService.GetChapterAsync(_projectId, SelectedManuscript.ManuscriptId, chapterIndex.ChapterId);
            CurrentChapter = fullChapter ?? chapterIndex;
            SelectedChapterItem = chapterIndex;

            CurrentMentions.Clear();
            if (CurrentChapter?.Mentions != null)
            {
                foreach (var mention in CurrentChapter.Mentions)
                    CurrentMentions.Add(mention);
            }

            ContentReloadRequested?.Invoke();
        }

        /// <summary>
        /// Command counterpart of <see cref="SelectChapterAsync"/> — no-op when the
        /// requested chapter is already active.
        /// </summary>
        [RelayCommand]
        public async Task SelectChapterItemAsync(Chapter? chapter)
        {
            if (chapter == null || chapter.ChapterId == CurrentChapter?.ChapterId) return;
            await SelectChapterAsync(chapter);
        }

        /// <summary>
        /// Creates a new manuscript appended after the existing ones, bootstraps it with
        /// a single default chapter, and switches the editor to it.
        /// </summary>
        [RelayCommand]
        private async Task AddManuscriptAsync()
        {
            var order = Manuscripts.Count;
            var newMs = await _apiService.CreateManuscriptAsync(_projectId, $"Manuscript {order + 1}", order);
            if (newMs != null)
            {
                var firstChapter = await _apiService.CreateChapterAsync(_projectId, newMs.ManuscriptId, "Chapter 1", string.Empty, 0);
                if (firstChapter != null)
                    newMs.Chapters.Add(firstChapter);

                Manuscripts.Add(newMs);
                await SelectManuscriptAsync(newMs);
            }
        }

        /// <summary>
        /// Deletes the specified manuscript and all its chapters.
        /// Refuses to delete when only one manuscript remains.
        /// </summary>
        [RelayCommand]
        private async Task DeleteManuscriptAsync(Manuscript? manuscript)
        {
            if (manuscript == null || Manuscripts.Count <= 1) return;

            var deleted = await _apiService.DeleteManuscriptAsync(_projectId, manuscript.ManuscriptId);
            if (deleted)
            {
                Manuscripts.Remove(manuscript);
                if (SelectedManuscript?.ManuscriptId == manuscript.ManuscriptId && Manuscripts.Any())
                    await SelectManuscriptAsync(Manuscripts.First());
            }
        }

        /// <summary>
        /// Renames the currently selected manuscript and refreshes the observable collection
        /// so the sidebar ComboBox reflects the change.
        /// </summary>
        [RelayCommand]
        private async Task RenameManuscriptAsync(string? newTitle)
        {
            if (SelectedManuscript == null || string.IsNullOrWhiteSpace(newTitle)) return;

            var updated = await _apiService.UpdateManuscriptAsync(_projectId, SelectedManuscript.ManuscriptId, newTitle, null);
            if (updated != null)
            {
                SelectedManuscript.Title = newTitle;
                var index = Manuscripts.IndexOf(SelectedManuscript);
                if (index >= 0)
                    Manuscripts[index] = SelectedManuscript;
            }
        }

        /// <summary>
        /// Creates a new chapter at the end of the current manuscript's chapter list
        /// and switches the editor to it.
        /// </summary>
        [RelayCommand]
        private async Task AddChapterAsync()
        {
            if (SelectedManuscript == null) return;

            var order = CurrentChapters.Count;
            var newChapter = await _apiService.CreateChapterAsync(
                _projectId, SelectedManuscript.ManuscriptId,
                $"Chapter {order + 1}", string.Empty, order);

            if (newChapter != null)
            {
                CurrentChapters.Add(newChapter);
                await SelectChapterAsync(newChapter);
            }
        }

        /// <summary>
        /// Deletes the specified chapter from the current manuscript.
        /// Refuses to delete when only one chapter remains.
        /// </summary>
        [RelayCommand]
        private async Task DeleteChapterAsync(Chapter? chapter)
        {
            if (chapter == null || SelectedManuscript == null || CurrentChapters.Count <= 1) return;

            var deleted = await _apiService.DeleteChapterAsync(_projectId, SelectedManuscript.ManuscriptId, chapter.ChapterId);
            if (deleted)
            {
                CurrentChapters.Remove(chapter);
                if (CurrentChapter?.ChapterId == chapter.ChapterId && CurrentChapters.Any())
                    await SelectChapterAsync(CurrentChapters.First());
            }
        }

        /// <summary><c>true</c> when a chapter is loaded and the editor can accept input.</summary>
        public bool CanEdit => CurrentChapter != null;

        /// <summary>
        /// Persists <paramref name="rtfContent"/> to the API for <see cref="CurrentChapter"/>.
        /// Guards against concurrent saves with <see cref="IsSaving"/>.
        /// </summary>
        [RelayCommand]
        public async Task SaveContentAsync(string rtfContent)
        {
            if (IsSaving || CurrentChapter == null || SelectedManuscript == null) return;
            IsSaving = true;

            try
            {
                var saved = await _apiService.UpdateChapterAsync(
                    _projectId,
                    SelectedManuscript.ManuscriptId,
                    CurrentChapter.ChapterId,
                    CurrentChapter.Title,
                    rtfContent,
                    CurrentChapter.Order
                );

                if (saved?.Mentions != null)
                {
                    CurrentMentions.Clear();
                    foreach (var mention in saved.Mentions)
                        CurrentMentions.Add(mention);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Auto-save failed: {ex.Message}");
            }
            finally
            {
                IsSaving = false;
            }
        }

        /// <summary>Updates <see cref="WordCountText"/> from a raw word <paramref name="count"/>.</summary>
        public void UpdateWordCount(int count)
        {
            WordCountText = $"{count} word{(count != 1 ? "s" : "")}";
        }
    }
}
