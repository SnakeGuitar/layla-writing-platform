using Layla.Desktop.Services;
using Layla.Desktop.ViewModels.User;
using Layla.Desktop.Views.Projects;
using System.Windows;
using System.Windows.Controls;

namespace Layla.Desktop.Views.User;

public partial class LoginView : Page
{
    private readonly LoginViewModel _viewModel;

    public LoginView()
    {
        InitializeComponent();
        _viewModel = ServiceLocator.GetService<LoginViewModel>() ?? throw new InvalidOperationException("ViewModel not found");
        DataContext = _viewModel;
        _viewModel.OnLoginSuccess += OnLoginSuccess;
    }

    private void OnLoginSuccess(object? sender, EventArgs e) =>
        NavigationService.Navigate(new ProjectListView());


    private void NavigateToSignUp_Click(object sender, RoutedEventArgs e) =>
        NavigationService.Navigate(new SignUpView());


    private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is LoginViewModel viewModel)
            viewModel.Password = ((PasswordBox)sender).Password;

    }
}

