using Layla.Desktop.Services.Logger;
using Layla.Desktop.Services.User;
using Layla.Desktop.ViewModels.User;
using Layla.Desktop.Views.Projects;
using Microsoft.Extensions.Logging;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Layla.Desktop.Views.User;

public partial class SignUpView : Page
{
    private readonly SignUpViewModel _viewModel;
    private readonly ILogger<SignUpView> _logger;

    public SignUpView()
    {
        InitializeComponent();
        _viewModel = ServiceLocator.GetService<SignUpViewModel>() ?? throw new InvalidOperationException("ViewModel not found");
        DataContext = _viewModel;

        _viewModel.OnRegistrationSuccess += (s, e) => NavigationService?.Navigate(new ProjectListView());
        _viewModel.OnNavigateToLogin += (s, e) => NavigationService?.Navigate(new LoginView());

        _logger = Log.For<SignUpView>();
    }

    private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        _logger.LogTrace("SignUpView - PasswordBox_PasswordChanged: Content changed.");
        if (DataContext is SignUpViewModel viewModel)
            viewModel.Password = ((PasswordBox)sender).Password;

    }

    private void NavigateToLogin_Click(object sender, MouseButtonEventArgs e)
    {
        _logger.LogTrace("SignUpView - NavigateToLogin_Click: Button clicked.");
        _viewModel.NavigateToLoginCommand.Execute(null);
    }
}
