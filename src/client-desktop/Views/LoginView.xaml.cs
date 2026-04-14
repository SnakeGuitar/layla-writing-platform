using Layla.Desktop.ViewModels;
using System.Windows.Controls;
using System.Windows;
using System;

namespace Layla.Desktop.Views
{
    public partial class LoginView : Page
    {
        private readonly LoginViewModel _viewModel;

        public LoginView()
        {
            InitializeComponent();
            _viewModel = Services.ServiceLocator.GetService<LoginViewModel>() ?? throw new InvalidOperationException("ViewModel not found");
            DataContext = _viewModel;
            _viewModel.OnLoginSuccess += OnLoginSuccess;
        }

        private void OnLoginSuccess(object? sender, EventArgs e)
        {
            NavigationService.Navigate(new ProjectListView());
        }

        private void NavigateToSignUp_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            NavigationService.Navigate(new SignUpView());
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is LoginViewModel viewModel)
            {
                viewModel.Password = ((PasswordBox)sender).Password;
            }
        }
    }
}
