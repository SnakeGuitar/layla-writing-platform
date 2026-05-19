using Layla.Desktop.Services;
using Layla.Desktop.ViewModels.User;
using System.Windows.Controls;

namespace Layla.Desktop.Views.User;

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
