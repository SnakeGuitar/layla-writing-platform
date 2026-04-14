using Layla.Desktop.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System;
using Layla.Desktop.Services;

namespace Layla.Desktop.Views
{
    public partial class SettingsView : Page
    {
        private readonly SettingsViewModel _viewModel;

        public SettingsView()
        {
            InitializeComponent();
            _viewModel = ServiceLocator.GetService<SettingsViewModel>() ?? throw new InvalidOperationException("ViewModel not found");
            DataContext = _viewModel;
            _viewModel.OnRequestGoBack += (s, e) => 
            {
                if (NavigationService.CanGoBack)
                {
                    NavigationService.GoBack();
                }
            };
        }
    }
}
