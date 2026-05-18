using Layla.Desktop.Services.Graphs;
using Layla.Desktop.Services.Manuscripts;
using Layla.Desktop.Services.Projetcs;
using Layla.Desktop.Services.User;
using Layla.Desktop.Services.User.Authentication;
using Layla.Desktop.Services.Wikis;
using Layla.Desktop.ViewModels.Manuscripts;
using Layla.Desktop.ViewModels.Projects;
using Layla.Desktop.ViewModels.User;
using Layla.Desktop.ViewModels.Wikis;
using MaterialDesignThemes.Wpf;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace Layla.Desktop;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{

    private string ConfigPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Layla", "theme.txt");
    public string CurrentTheme { get; private set; } = "SpaceTheme";

    protected override void OnStartup(StartupEventArgs e)
    {
        for (int i = 0; i < e.Args.Length; i++)
        {
            if (e.Args[i].StartsWith("--profile="))
            {
                SessionManager.ProfileName = e.Args[i].Replace("--profile=", "session_");
            }
            else if ((e.Args[i] == "-p" || e.Args[i] == "--profile") && i + 1 < e.Args.Length)
            {
                SessionManager.ProfileName = "session_" + e.Args[i + 1];
                i++;
            }
        }

        SessionManager.LoadSession();
        base.OnStartup(e);

        var services = new ServiceCollection();
        ConfigureServices(services);
        var provider = services.BuildServiceProvider();
        ServiceLocator.Initialize(provider);

        string theme = "SpaceTheme";
        try
        {
            if (File.Exists(ConfigPath))
                theme = File.ReadAllText(ConfigPath).Trim();
        }
        catch { }
        ChangeTheme(theme);

        this.Dispatcher.InvokeAsync(() =>
        {
            if (this.MainWindow != null)
            {
                this.MainWindow.KeyDown += MainWindow_KeyDown;
            }
        });
    }

    private bool _isFullscreen = false;
    private WindowStyle _previousWindowStyle = WindowStyle.SingleBorderWindow;
    private WindowState _previousWindowState = WindowState.Normal;

    public bool IsFullscreen => _isFullscreen;

    public void SetFullscreen(bool isFullscreen)
    {
        if (this.MainWindow == null) return;
        if (_isFullscreen == isFullscreen) return;

        if (isFullscreen)
        {
            _previousWindowStyle = this.MainWindow.WindowStyle;
            _previousWindowState = this.MainWindow.WindowState;

            this.MainWindow.WindowStyle = WindowStyle.None;
            this.MainWindow.WindowState = WindowState.Maximized;
            _isFullscreen = true;
        }
        else
        {
            this.MainWindow.WindowStyle = _previousWindowStyle;
            this.MainWindow.WindowState = _previousWindowState;
            _isFullscreen = false;
        }
    }

    private void MainWindow_KeyDown(object sender, KeyEventArgs e)
    {
        if (this.MainWindow == null) return;

        if (e.Key == Key.F11)
        {
            SetFullscreen(!_isFullscreen);
        }
        else if (e.Key == Key.Escape && _isFullscreen)
        {
            SetFullscreen(false);
        }
    }

    public void ChangeTheme(string theme)
    {
        CurrentTheme = theme;
        try
        {
            var dir = Path.GetDirectoryName(ConfigPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir!);
            File.WriteAllText(ConfigPath, theme);
        }
        catch { }

        var existingTheme = Resources.MergedDictionaries.FirstOrDefault(d => d.Source != null && d.Source.OriginalString.StartsWith("Themes/"));
        if (existingTheme != null)
        {
            Resources.MergedDictionaries.Remove(existingTheme);
        }
        Resources.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri($"Themes/{theme}.xaml", UriKind.Relative)
        });

        PaletteHelper paletteHelper = new PaletteHelper();
        var materialTheme = paletteHelper.GetTheme();
        if (theme == "LightTheme")
        {
            materialTheme.SetBaseTheme(BaseTheme.Light);
        }
        else
        {
            materialTheme.SetBaseTheme(BaseTheme.Dark);
        }
        paletteHelper.SetTheme(materialTheme);
    }

    private void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IManuscriptApiService, ManuscriptApiService>();
        services.AddSingleton<IProjectApiService, ProjectApiService>();
        services.AddSingleton<IAuthService, AuthService>();
        services.AddSingleton<IWikiApiService, WikiApiService>();
        services.AddSingleton<IGraphApiService, GraphApiService>();
        services.AddSingleton<LocalCacheManager>();

        services.AddTransient<ManuscriptEditorViewModel>();
        services.AddTransient<ProjectListViewModel>();
        services.AddTransient<LoginViewModel>();
        services.AddTransient<WorkspaceViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<SignUpViewModel>();
        services.AddTransient<WikiEntityEditorViewModel>();
        services.AddTransient<VoicePanelViewModel>();
        services.AddTransient<NarrativeGraphViewModel>();
        services.AddTransient<PublicProjectsViewModel>();
        services.AddTransient<ReaderWorkspaceViewModel>();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        base.OnExit(e);
    }
}
