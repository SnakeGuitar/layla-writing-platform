using Layla.Desktop.Services;
using Layla.Desktop.Services.Graphs;
using Layla.Desktop.Services.Manuscripts;
using Layla.Desktop.Services.Projetcs;
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
    public string CurrentTheme { get; private set; } = "CyberMinimalismTheme";

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

        ServiceCollection? services = new();
        ConfigureServices(services);
        ServiceProvider? provider = services.BuildServiceProvider();
        ServiceLocator.Initialize(provider);

        string theme = "CyberMinimalismTheme";
        try
        {
            if (File.Exists(this.ConfigPath))
            {
                string saved = File.ReadAllText(this.ConfigPath).Trim();
                // Migrate old SpaceTheme to CyberMinimalismTheme
                if (saved == "SpaceTheme") saved = "CyberMinimalismTheme";
                theme = saved;
            }
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

    public bool IsFullscreen => this._isFullscreen;

    public void SetFullscreen(bool isFullscreen)
    {
        if (this.MainWindow == null) return;
        if (this._isFullscreen == isFullscreen) return;

        if (isFullscreen)
        {
            this._previousWindowStyle = this.MainWindow.WindowStyle;
            this._previousWindowState = this.MainWindow.WindowState;

            this.MainWindow.WindowStyle = WindowStyle.None;
            this.MainWindow.WindowState = WindowState.Maximized;
            this._isFullscreen = true;
        }
        else
        {
            this.MainWindow.WindowStyle = this._previousWindowStyle;
            this.MainWindow.WindowState = this._previousWindowState;
            this._isFullscreen = false;
        }
    }

    private void MainWindow_KeyDown(object sender, KeyEventArgs e)
    {
        if (this.MainWindow == null) return;

        if (e.Key == Key.F11)
        {
            SetFullscreen(!this._isFullscreen);
        }
        else if (e.Key == Key.Escape && this._isFullscreen)
        {
            SetFullscreen(false);
        }
    }

    public void ChangeTheme(string theme)
    {
        this.CurrentTheme = theme;
        try
        {
            string? dir = Path.GetDirectoryName(this.ConfigPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir!);
            File.WriteAllText(this.ConfigPath, theme);
        }
        catch { }

        ResourceDictionary? existingTheme = this.Resources.MergedDictionaries.FirstOrDefault(d => d.Source != null && d.Source.OriginalString.StartsWith("Themes/"));
        if (existingTheme != null)
        {
            this.Resources.MergedDictionaries.Remove(existingTheme);
        }
        this.Resources.MergedDictionaries.Add(new()
        {
            Source = new($"Themes/{theme}.xaml", UriKind.Relative)
        });

        PaletteHelper paletteHelper = new();
        Theme? materialTheme = paletteHelper.GetTheme();
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
