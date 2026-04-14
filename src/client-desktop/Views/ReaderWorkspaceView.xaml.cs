using System;
using System.Windows;
using System.Windows.Controls;
using Layla.Desktop.Models;
using Layla.Desktop.ViewModels;
using Layla.Desktop.Services;

namespace Layla.Desktop.Views
{
    public partial class ReaderWorkspaceView : Page
    {
        private readonly ReaderWorkspaceViewModel _viewModel;

        public ReaderWorkspaceView(Project project)
        {
            InitializeComponent();
            _viewModel = ServiceLocator.GetService<ReaderWorkspaceViewModel>() ?? throw new InvalidOperationException("ViewModel not found");
            DataContext = _viewModel;
            _viewModel.Initialize(project);

            _viewModel.OnBackToPublicProjects += (s, e) => NavigationService.Navigate(new PublicProjectsView());
            _viewModel.OnLogout += (s, e) => NavigationService.Navigate(new LoginView());

            this.Loaded += ReaderWorkspaceView_Loaded;
        }

        private async void ReaderWorkspaceView_Loaded(object sender, RoutedEventArgs e)
        {
            if (_viewModel.CurrentProject != null)
            {
                var projectId = _viewModel.CurrentProject.Id;
                EditorFrame.Navigate(new ManuscriptEditorView(projectId, isReadOnly: true));
                WikiFrame.Navigate(new WikiEntityEditorView(projectId));
                GraphFrame.Navigate(new NarrativeGraphView(projectId));
                VoiceFrame.Navigate(new VoicePanelView(projectId));

                await _viewModel.StartWatchingPresenceAsync();
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
