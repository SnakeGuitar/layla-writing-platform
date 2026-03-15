using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Layla.Desktop.Models;
using Layla.Desktop.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Layla.Desktop.ViewModels
{
    public partial class ManuscriptEditorViewModel : ObservableObject
    {
        private readonly IManuscriptApiService _apiService;
        private Guid _projectId;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private bool _isSaving;

        [ObservableProperty]
        private string _wordCountText = "0 words";

        [ObservableProperty]
        private Chapter? _currentChapter;

        public ManuscriptEditorViewModel(IManuscriptApiService apiService)
        {
            _apiService = apiService;
        }

        public void Initialize(Guid projectId)
        {
            _projectId = projectId;
        }

        [RelayCommand]
        private async Task LoadManuscriptAsync()
        {
            IsLoading = true;
            try
            {
                var manuscript = await _apiService.GetManuscriptAsync(_projectId);
                Chapter? targetChapter = null;

                if (manuscript != null && manuscript.Chapters.Any())
                {
                    targetChapter = await _apiService.GetChapterAsync(_projectId, manuscript.Chapters.OrderBy(c => c.Order).First().ChapterId);
                }
                else
                {
                    targetChapter = await _apiService.CreateChapterAsync(_projectId, "Chapter 1", string.Empty, 0);
                }

                CurrentChapter = targetChapter;
                OnPropertyChanged(nameof(CanEdit));
            }
            catch (Exception ex)
            {
                // In a real app, we would use a DialogService
                System.Diagnostics.Debug.WriteLine($"Failed to load manuscript: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        public bool CanEdit => CurrentChapter != null;

        [RelayCommand]
        public async Task SaveContentAsync(string rtfContent)
        {
            if (IsSaving || CurrentChapter == null) return;
            IsSaving = true;

            try
            {
                // We fetch the latest to ensure we have the correct title/order if they changed elsewhere (though unlikely here)
                var currentChapter = await _apiService.GetChapterAsync(_projectId, CurrentChapter.ChapterId);
                if (currentChapter != null)
                {
                    await _apiService.UpdateChapterAsync(
                        _projectId,
                        CurrentChapter.ChapterId,
                        currentChapter.Title,
                        rtfContent,
                        currentChapter.Order
                    );
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

        public void UpdateWordCount(int count)
        {
            WordCountText = $"{count} word{(count != 1 ? "s" : "")}";
        }
    }
}
