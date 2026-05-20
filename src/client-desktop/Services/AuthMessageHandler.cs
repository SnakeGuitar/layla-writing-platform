using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Layla.Desktop.Services
{
    /// <summary>
    /// Injects the current session JWT into every outgoing request. Used as
    /// a <see cref="DelegatingHandler"/> so the header lives on the request
    /// object instead of being mutated on the shared <see cref="HttpClient"/>
    /// — the previous pattern raced when concurrent calls used the singleton
    /// client and one mid-flight logout could strip another request's header.
    /// </summary>
    public sealed class AuthMessageHandler : DelegatingHandler
    {
        public AuthMessageHandler() : base(new HttpClientHandler()) { }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.Headers.Authorization is null && SessionManager.IsAuthenticated)
            {
                request.Headers.Authorization = new AuthenticationHeaderValue(
                    "Bearer", SessionManager.CurrentToken);
            }
            
            var response = await base.SendAsync(request, cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                SessionManager.ClearSession();
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    var mainWindow = System.Windows.Application.Current.MainWindow;
                    if (mainWindow is System.Windows.Navigation.NavigationWindow navWindow)
                    {
                        navWindow.Navigate(new System.Uri("Views/LoginView.xaml", System.UriKind.Relative));
                    }
                    else
                    {
                        // Fallback: scan open windows for the NavigationWindow
                        foreach (System.Windows.Window window in System.Windows.Application.Current.Windows)
                        {
                            if (window is System.Windows.Navigation.NavigationWindow nw)
                            {
                                nw.Navigate(new System.Uri("Views/LoginView.xaml", System.UriKind.Relative));
                                break;
                            }
                        }
                    }
                });
            }

            return response;
        }
    }
}
