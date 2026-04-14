using System;
using System.Windows;
using System.Windows.Controls;
using Layla.Desktop.Models;
using Layla.Desktop.ViewModels;
using Layla.Desktop.Services;

namespace Layla.Desktop.Views
{
    public partial class WorkspaceView : Page
    {
        private readonly WorkspaceViewModel _viewModel;

        public WorkspaceView(Project currentProject)
        {
            InitializeComponent();
            _viewModel = ServiceLocator.GetService<WorkspaceViewModel>() ?? throw new InvalidOperationException("ViewModel not found");
            DataContext = _viewModel;
            _viewModel.Initialize(currentProject);

            _viewModel.OnLogout += (s, e) => NavigationService.Navigate(new LoginView());
            _viewModel.OnBackToProjects += (s, e) => NavigationService.Navigate(new ProjectListView());
            _viewModel.OnSettings += (s, e) => NavigationService.Navigate(new SettingsView());

            this.Loaded += WorkspaceView_Loaded;
        }

        private void WorkspaceView_Loaded(object sender, RoutedEventArgs e)
        {
            if (_viewModel.CurrentProject != null)
            {
                var projectId = _viewModel.CurrentProject.Id;
                EditorFrame.Navigate(new ManuscriptEditorView(projectId));
                WikiFrame.Navigate(new WikiEntityEditorView(projectId));
                GraphFrame.Navigate(new NarrativeGraphView(projectId));
                VoiceFrame.Navigate(new VoicePanelView(projectId));
            }

            try
            {
                while (NavigationService != null && NavigationService.CanGoBack)
                {
                    NavigationService.RemoveBackEntry();
                }
            }
            catch { }
        }
    }
}
