using Layla.Desktop.Services;
using Layla.Desktop.ViewModels;
using System;
using System.Windows;
using System.Windows.Controls;

namespace Layla.Desktop.Views
{
    public partial class PublicProjectsView : Page
    {
        private readonly PublicProjectsViewModel _viewModel;

        public PublicProjectsView()
        {
            InitializeComponent();
            _viewModel = ServiceLocator.GetService<PublicProjectsViewModel>() ?? throw new InvalidOperationException("ViewModel not found");
            DataContext = _viewModel;

            _viewModel.OnBackToMyProjects += (s, e) => NavigationService.Navigate(new ProjectListView());
            _viewModel.OnOpenProject += (s, project) => NavigationService.Navigate(new ReaderWorkspaceView(project));

            this.Loaded += async (s, e) => await _viewModel.LoadPublicProjectsCommand.ExecuteAsync(null);
        }

        private void PublicProjectsList_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (PublicProjectsList.SelectedItem is Models.Project selectedProject)
            {
                NavigationService.Navigate(new ReaderWorkspaceView(selectedProject));
            }
        }
    }
}
