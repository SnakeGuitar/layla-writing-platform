using System.Configuration;
using System.Data;
using System.Windows;
using MaterialDesignThemes.Wpf;
using Microsoft.Extensions.DependencyInjection;
using Layla.Desktop.Services;

namespace Layla.Desktop
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {

        private string ConfigPath => System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Layla", "theme.txt");
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
                if (System.IO.File.Exists(ConfigPath))
                    theme = System.IO.File.ReadAllText(ConfigPath).Trim();
            } 
            catch {}
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

        private void MainWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (this.MainWindow == null) return;

            if (e.Key == System.Windows.Input.Key.F11)
            {
                SetFullscreen(!_isFullscreen);
            }
            else if (e.Key == System.Windows.Input.Key.Escape && _isFullscreen)
            {
                SetFullscreen(false);
            }
        }

        public void ChangeTheme(string theme)
        {
            CurrentTheme = theme;
            try 
            {
                var dir = System.IO.Path.GetDirectoryName(ConfigPath);
                if (!System.IO.Directory.Exists(dir)) System.IO.Directory.CreateDirectory(dir!);
                System.IO.File.WriteAllText(ConfigPath, theme);
            } 
            catch {}

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

            services.AddTransient<ViewModels.ManuscriptEditorViewModel>();
            services.AddTransient<ViewModels.ProjectListViewModel>();
            services.AddTransient<ViewModels.LoginViewModel>();
            services.AddTransient<ViewModels.WorkspaceViewModel>();
            services.AddTransient<ViewModels.SettingsViewModel>();
            services.AddTransient<ViewModels.SignUpViewModel>();
            services.AddTransient<ViewModels.WikiEntityEditorViewModel>();
            services.AddTransient<ViewModels.VoicePanelViewModel>();
            services.AddTransient<ViewModels.NarrativeGraphViewModel>();
            services.AddTransient<ViewModels.PublicProjectsViewModel>();
            services.AddTransient<ViewModels.ReaderWorkspaceViewModel>();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
        }
    }
}
