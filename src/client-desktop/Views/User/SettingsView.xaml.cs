using Layla.Desktop.Services;
using Layla.Desktop.ViewModels.User;
using System;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace Layla.Desktop.Views.User;

/// <summary>
/// Converts a URL or base-64 data-URI string into a WPF BitmapImage.
/// Returns null (no image) when the value is null or empty.
/// </summary>
[ValueConversion(typeof(string), typeof(BitmapImage))]
public class AvatarUrlConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not string url || string.IsNullOrWhiteSpace(url)) return null;
        try
        {
            if (url.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                var comma = url.IndexOf(',');
                if (comma < 0) return null;
                var base64 = url[(comma + 1)..];
                var bytes = System.Convert.FromBase64String(base64);
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.StreamSource = new MemoryStream(bytes);
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.EndInit();
                return bmp;
            }
            else
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.UriSource = new Uri(url, UriKind.Absolute);
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.EndInit();
                return bmp;
            }
        }
        catch { return null; }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

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
