using Layla.Desktop.Models;
using Layla.Desktop.Services;
using Layla.Desktop.ViewModels;
using System;
using System.Linq;
using System.Windows.Controls;

namespace Layla.Desktop.Views
{
    public partial class WikiEntityEditorView : Page
    {
        private readonly WikiEntityEditorViewModel _viewModel;

        public WikiEntityEditorView(Guid projectId)
        {
            InitializeComponent();
            _viewModel = ServiceLocator.GetService<WikiEntityEditorViewModel>()
                ?? throw new InvalidOperationException("WikiEntityEditorViewModel not registered");
            _viewModel.Initialize(projectId);
            DataContext = _viewModel;

            Loaded += async (_, _) =>
            {
                try { await _viewModel.LoadEntriesCommand.ExecuteAsync(null); }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"LoadEntries failed: {ex.Message}"); }
            };
        }

        private async void EntryListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is not ListBox lb || lb.SelectedItem is not WikiEntry entry) return;
            try
            {
                await _viewModel.SelectEntryCommand.ExecuteAsync(entry);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EntryListBox_SelectionChanged failed: {ex.Message}");
            }
        }

        /// <summary>
        /// When the user clicks an appearance record, navigate to that chapter
        /// in the Manuscript Editor tab.
        /// </summary>
        private void AppearanceListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ListBox lb && lb.SelectedItem is AppearanceRecord appearance)
            {
                WorkspaceMediator.RequestNavigateToChapter(appearance.ManuscriptId, appearance.ChapterId);
                lb.SelectedItem = null;
            }
        }

        /// <summary>
        /// Called by <see cref="WorkspaceView"/> when cross-tab navigation requests
        /// selection of a specific wiki entity.
        /// </summary>
        public async void SelectEntityById(string entityId)
        {
            try
            {
                var entry = _viewModel.Entries.FirstOrDefault(e => e.EntityId == entityId);
                if (entry != null)
                {
                    _viewModel.SelectedEntry = entry;
                    await _viewModel.SelectEntryCommand.ExecuteAsync(entry);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SelectEntityById failed: {ex.Message}");
            }
        }
    }
}
