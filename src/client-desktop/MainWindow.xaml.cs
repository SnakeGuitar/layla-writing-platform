using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Layla.Desktop;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    public void Navigate(object page) => MainFrame.Navigate(page);

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.Source is System.Windows.Controls.Button) return;
        if (e.ClickCount == 2)
        {
            WindowState = WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
            return;
        }
        DragMove();
    }

    private void Window_StateChanged(object sender, EventArgs e)
    {
        if (WindowState == WindowState.Maximized)
        {
            OuterGrid.Margin = new Thickness(0);
            ContentBorder.CornerRadius = new CornerRadius(0);
            ShadowBorder.CornerRadius = new CornerRadius(0);
        }
        else
        {
            OuterGrid.Margin = new Thickness(12);
            ContentBorder.CornerRadius = new CornerRadius(16);
            ShadowBorder.CornerRadius = new CornerRadius(16);
        }
    }

    private void ContentBorder_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        ContentBorder.Clip = new RectangleGeometry(
            new Rect(0, 0, ContentBorder.ActualWidth, ContentBorder.ActualHeight),
            16, 16);
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e) =>
        WindowState = WindowState.Minimized;

    private void MaxRestoreButton_Click(object sender, RoutedEventArgs e) =>
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;

    private void CloseButton_Click(object sender, RoutedEventArgs e) =>
        Close();
}
