using Layla.Desktop.ViewModels;
using System.Windows.Controls;
using System;
using Layla.Desktop.Services;
using System.Windows;

namespace Layla.Desktop.Views
{
    public partial class SignUpView : Page
    {
        private readonly SignUpViewModel _viewModel;

        public SignUpView()
        {
            InitializeComponent();
            _viewModel = ServiceLocator.GetService<SignUpViewModel>() ?? throw new InvalidOperationException("ViewModel not found");
            DataContext = _viewModel;

            _viewModel.OnRegistrationSuccess += (s, e) => NavigationService?.Navigate(new ProjectListView());
            _viewModel.OnNavigateToLogin += (s, e) => NavigationService?.Navigate(new LoginView());
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is SignUpViewModel viewModel)
            {
                viewModel.Password = ((PasswordBox)sender).Password;
            }
        }

        private void NavigateToLogin_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            _viewModel.NavigateToLoginCommand.Execute(null);
        }
    }
}
