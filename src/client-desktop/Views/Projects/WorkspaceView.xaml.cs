using Layla.Desktop.Models.Projects;
using Layla.Desktop.Services;
using Layla.Desktop.Services.Projetcs;
using Layla.Desktop.ViewModels.Projects;
using Layla.Desktop.Views.Manuscripts;
using Layla.Desktop.Views.User;
using Layla.Desktop.Views.Wikis;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;

namespace Layla.Desktop.Views.Projects;

public partial class WorkspaceView : Page
{
    private readonly WorkspaceViewModel _viewModel;
    private ManuscriptEditorView? _editorView;
    private WikiEntityEditorView? _wikiView;
    private NarrativeGraphView? _graphView;

    // ── Sidebar collapse/expand ──────────────────────────────────────────
    private bool _isSidebarExpanded = true;
    private const double SidebarExpandedWidth  = 260;
    private const double SidebarCollapsedWidth = 52;

    public WorkspaceView(Project currentProject)
    {
        InitializeComponent();
        _viewModel = ServiceLocator.GetService<WorkspaceViewModel>() ?? throw new InvalidOperationException("ViewModel not found");
        DataContext = _viewModel;
        _viewModel.Initialize(currentProject);

        _viewModel.OnLogout += (s, e) => ((MainWindow)Application.Current.MainWindow).Navigate(new LoginView());
        _viewModel.OnBackToProjects += (s, e) => ((MainWindow)Application.Current.MainWindow).Navigate(new ProjectListView());
        _viewModel.OnSettings += (s, e) => ((MainWindow)Application.Current.MainWindow).Navigate(new SettingsView());

        this.Loaded += WorkspaceView_Loaded;
        this.Unloaded += WorkspaceView_Unloaded;
    }

    private void WorkspaceView_Loaded(object sender, RoutedEventArgs e)
    {
        if (_viewModel.CurrentProject != null)
        {
            Guid projectId = _viewModel.CurrentProject.Id;
            bool isReadOnly = _viewModel.IsReadOnly;
            _editorView = new ManuscriptEditorView(projectId, isReadOnly);
            _wikiView = new WikiEntityEditorView(projectId, isReadOnly);
            _graphView = new NarrativeGraphView(projectId, isReadOnly);

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
        _viewModel.Dispose();
    }

    private void OnNavigateToWikiEntry(string entityId)
    {
        Dispatcher.Invoke(() =>
        {
            // Switch to Wiki tab (index 1)
            TabControl? tabControl = FindTabControl();
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
            TabControl? tabControl = FindTabControl();
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
            TabControl? tabControl = FindTabControl();
            if (tabControl != null)
                tabControl.SelectedIndex = 2;
        });
    }

    /// <summary>
    /// Toggles the sidebar between expanded (260 px) and collapsed (52 px, icon-only).
    /// Animates the <see cref="SidebarColumn"/> width via a <see cref="DispatcherTimer"/>
    /// because WPF does not natively animate <see cref="System.Windows.GridLength"/>.
    /// </summary>
    private void ToggleSidebar_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        _isSidebarExpanded = !_isSidebarExpanded;

        double targetWidth  = _isSidebarExpanded ? SidebarExpandedWidth  : SidebarCollapsedWidth;
        double startWidth   = SidebarColumn.Width.Value;
        double totalDelta   = targetWidth - startWidth;
        const int steps     = 16;
        int currentStep     = 0;

        // Flip text / alignment immediately so they don't flicker mid-animation
        ApplySidebarTextVisibility(_isSidebarExpanded);

        DispatcherTimer timer = new() { Interval = TimeSpan.FromMilliseconds(12) };
        timer.Tick += (_, _) =>
        {
            currentStep++;
            // Ease-in-out (smoothstep)
            double t = (double)currentStep / steps;
            double eased = t * t * (3 - 2 * t);
            SidebarColumn.Width = new System.Windows.GridLength(startWidth + totalDelta * eased);

            if (currentStep >= steps)
            {
                SidebarColumn.Width = new System.Windows.GridLength(targetWidth);
                timer.Stop();
            }
        };
        timer.Start();

        ToolTipService.SetToolTip(ToggleSidebarButton, _isSidebarExpanded ? "Colapsar menú" : "Expandir menú");
    }

    /// <summary>
    /// Shows or hides the text labels and header elements depending on sidebar state.
    /// </summary>
    private void ApplySidebarTextVisibility(bool expanded)
    {
        Visibility text  = expanded ? Visibility.Visible   : Visibility.Collapsed;
        var        align = expanded ? HorizontalAlignment.Left : HorizontalAlignment.Center;

        SidebarTitleText.Visibility = text;
        SidebarBadge.Visibility     = text;
        BackBtnText.Visibility      = text;
        CollabBtnText.Visibility    = text;
        SettingsBtnText.Visibility  = text;
        LogoutBtnText.Visibility    = text;

        BackBtn.HorizontalContentAlignment     = align;
        CollabBtn.HorizontalContentAlignment   = align;
        SettingsBtn.HorizontalContentAlignment = align;
        LogoutBtn.HorizontalContentAlignment   = align;
    }

    private TabControl? FindTabControl()
    {
        return FindChild<TabControl>(this);
    }

    /// <summary>
    /// When the role ComboBox changes, fire the ChangeCollaboratorRole command.
    /// Uses the ComboBox Tag to identify which collaborator to update.
    /// A flag prevents re-entrancy when the ComboBox is being programmatically synced.
    /// </summary>
    private bool _suppressRoleChange = false;
    private async void CollaboratorRoleComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressRoleChange) return;
        if (sender is not ComboBox cb) return;
        if (cb.Tag is not Collaborator collaborator) return;
        if (cb.SelectedItem is not ComboBoxItem item) return;

        string? selectedRole = item.Content?.ToString();
        if (selectedRole == null || selectedRole == collaborator.Role) return;

        _suppressRoleChange = true;
        try
        {
            await _viewModel.ChangeCollaboratorRoleCommand.ExecuteAsync(collaborator);
        }
        finally
        {
            _suppressRoleChange = false;
        }
    }

    private static T? FindChild<T>(DependencyObject parent) where T : DependencyObject
    {
        int count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < count; i++)
        {
            DependencyObject child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
            if (child is T found) return found;
            T? result = FindChild<T>(child);
            if (result != null) return result;
        }
        return null;
    }
}
