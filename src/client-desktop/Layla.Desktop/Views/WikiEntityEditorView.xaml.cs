using Layla.Desktop.ViewModels;
using System.Windows.Controls;
using System;
using Layla.Desktop.Services;

namespace Layla.Desktop.Views
{
    public partial class WikiEntityEditorView : Page
    {
        private readonly WikiEntityEditorViewModel _viewModel;

        public WikiEntityEditorView()
        {
            InitializeComponent();
            _viewModel = ServiceLocator.GetService<WikiEntityEditorViewModel>() ?? throw new InvalidOperationException("ViewModel not found");
            DataContext = _viewModel;
        }
    }
}
