using Layla.Desktop.Models;
using Layla.Desktop.Services;
using Layla.Desktop.ViewModels;
using System;
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

            Loaded += async (_, _) => await _viewModel.LoadEntriesCommand.ExecuteAsync(null);
        }

        private async void EntryListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ListBox lb && lb.SelectedItem is WikiEntry entry)
            {
                await _viewModel.SelectEntryCommand.ExecuteAsync(entry);
            }
        }
    }
}
