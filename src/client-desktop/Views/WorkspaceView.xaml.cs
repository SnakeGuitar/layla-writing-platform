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
        private ManuscriptEditorView? _editorView;
        private WikiEntityEditorView? _wikiView;
        private NarrativeGraphView? _graphView;

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
            this.Unloaded += WorkspaceView_Unloaded;
        }

        private void WorkspaceView_Loaded(object sender, RoutedEventArgs e)
        {
            if (_viewModel.CurrentProject != null)
            {
                var projectId = _viewModel.CurrentProject.Id;
                _editorView = new ManuscriptEditorView(projectId);
                _wikiView = new WikiEntityEditorView(projectId);
                _graphView = new NarrativeGraphView(projectId);

                EditorFrame.Navigate(_editorView);
                WikiFrame.Navigate(_wikiView);
                GraphFrame.Navigate(_graphView);
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

            // Subscribe to cross-tab navigation events
            WorkspaceMediator.NavigateToWikiEntry += OnNavigateToWikiEntry;
            WorkspaceMediator.NavigateToChapter += OnNavigateToChapter;
            WorkspaceMediator.NavigateToGraph += OnNavigateToGraph;
        }

        private void WorkspaceView_Unloaded(object sender, RoutedEventArgs e)
        {
            WorkspaceMediator.NavigateToWikiEntry -= OnNavigateToWikiEntry;
            WorkspaceMediator.NavigateToChapter -= OnNavigateToChapter;
            WorkspaceMediator.NavigateToGraph -= OnNavigateToGraph;
        }

        private void OnNavigateToWikiEntry(string entityId)
        {
            Dispatcher.Invoke(() =>
            {
                // Switch to Wiki tab (index 1)
                var tabControl = FindTabControl();
                if (tabControl != null)
                    tabControl.SelectedIndex = 1;

                _wikiView?.SelectEntityById(entityId);
            });
        }

        private void OnNavigateToChapter(string manuscriptId, string chapterId)
        {
            Dispatcher.Invoke(() =>
            {
                // Switch to Editor tab (index 0)
                var tabControl = FindTabControl();
                if (tabControl != null)
                    tabControl.SelectedIndex = 0;

                _editorView?.NavigateToChapter(manuscriptId, chapterId);
            });
        }

        private void OnNavigateToGraph(string? entityId)
        {
            Dispatcher.Invoke(() =>
            {
                // Switch to Graph tab (index 2)
                var tabControl = FindTabControl();
                if (tabControl != null)
                    tabControl.SelectedIndex = 2;
            });
        }

        private TabControl? FindTabControl()
        {
            return FindChild<TabControl>(this);
        }

        private static T? FindChild<T>(DependencyObject parent) where T : DependencyObject
        {
            int count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                if (child is T found) return found;
                var result = FindChild<T>(child);
                if (result != null) return result;
            }
            return null;
        }
    }
}
