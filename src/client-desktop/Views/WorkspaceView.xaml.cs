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

            // Flush any pending editor changes BEFORE navigating — the
            // nested Page's Unloaded event is not reliable across all
            // WPF Frame navigation paths, so we don't depend on it.
            _viewModel.OnLogout += async (s, e) =>
            {
                await FlushEditorAsync();
                Services.SessionManager.ClearSession();
                if (NavigationService != null)
                {
                    NavigationService.Navigate(new LoginView());
                }
            };
            _viewModel.OnBackToProjects += async (s, e) =>
            {
                await FlushEditorAsync();
                NavigationService.Navigate(new ProjectListView());
            };
            _viewModel.OnSettings += async (s, e) =>
            {
                await FlushEditorAsync();
                NavigationService.Navigate(new SettingsView());
            };

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

            // Releases the WorkspaceViewModel's subscription on the Singleton
            // IProjectApiService and stops the heartbeat timer.
            _viewModel.Dispose();
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

        private async System.Threading.Tasks.Task FlushEditorAsync()
        {
            if (_editorView == null) return;
            try
            {
                await _editorView.FlushPendingSavesAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FlushEditorAsync failed: {ex.Message}");
            }
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
