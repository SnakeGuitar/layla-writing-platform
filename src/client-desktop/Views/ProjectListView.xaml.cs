using Layla.Desktop.Services;
using Layla.Desktop.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System;
using System.Threading.Tasks;

namespace Layla.Desktop.Views
{
    public partial class ProjectListView : Page
    {
        private readonly ProjectListViewModel _viewModel;

        public ProjectListView()
        {
            InitializeComponent();
            _viewModel = ServiceLocator.GetService<ProjectListViewModel>() ?? throw new InvalidOperationException("ViewModel not found");
            DataContext = _viewModel;
            _viewModel.OnLogout += (s, e) => {
                SessionManager.ClearSession();
                NavigationService.Navigate(new LoginView());
            };
            this.Loaded += Page_Loaded;
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            await _viewModel.LoadProjectsCommand.ExecuteAsync(null);
        }

        private void ProjectsListView_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (ProjectsListView.SelectedItem is Models.Project selectedProject)
            {
                NavigationService.Navigate(new WorkspaceView(selectedProject));
            }
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new SettingsView());
        }

        private void BrowsePublicButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new PublicProjectsView());
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            Services.SessionManager.ClearSession();
            NavigationService.Navigate(new LoginView());
        }
    }
}
