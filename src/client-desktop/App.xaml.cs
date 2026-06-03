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
using Layla.Desktop.Views.User;
using Layla.Client.Shared.Hub;
using Layla.Client.Shared.Services;
using MaterialDesignThemes.Wpf;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Input;

namespace Layla.Desktop;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{

    private string ConfigPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Layla", "theme.txt");
    public string CurrentTheme { get; private set; } = "NeumorphismTheme";

    private static readonly string[] AvailableThemes = { "LightTheme", "DarkTheme", "NeumorphismTheme" };

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

        ServiceCollection? services = new();
        ConfigurationService.Load();
        ConfigurationService.EnsureDefaultConfigFile();
        ConfigureServices(services);
        ServiceProvider? provider = services.BuildServiceProvider();
        ServiceLocator.Initialize(provider);

        string theme = "NeumorphismTheme";
        try
        {
            if (File.Exists(this.ConfigPath))
            {
                string saved = File.ReadAllText(this.ConfigPath).Trim();
                // Only honor the saved value if it maps to a theme that still exists;
                // otherwise keep the default so a stale or removed theme name can't crash startup.
                if (AvailableThemes.Contains(saved)) theme = saved;
            }
        }
        catch { }
        ChangeTheme(theme);

        base.OnStartup(e);

        var mainWindow = new MainWindow();
        this.MainWindow = mainWindow;
        mainWindow.KeyDown += MainWindow_KeyDown;
        mainWindow.Show();
        mainWindow.Navigate(new LoginView());
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
        services.AddSingleton<ManuscriptHubClient>();
        services.AddSingleton<ICollaborationApiService>(sp =>
        {
            var handler = new AuthMessageHandler();
            var httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri(ConfigurationService.WORLDBUILDING_API_URL)
            };
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new("application/json"));
            return new CollaborationApiService(httpClient);
        });

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
