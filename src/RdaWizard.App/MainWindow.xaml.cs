using System.Windows;
using System.Windows.Input;
using RdaWizard.Core.Navigation;
using RdaWizard.Core.ViewModels;

namespace RdaWizard.App;

/// <summary>
/// Main window (og: DrwMainWindow): custom frameless chrome — title-bar
/// window controls (min/max/close + hamburger), sidebar + page host.
/// Keyboard: F5 refresh (landing), Alt+Left back, F1 About.
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private WindowState _lastRestoreState = WindowState.Normal;

    private void OnMinimizeClick(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void OnMaximizeClick(object sender, RoutedEventArgs e)
    {
        if (WindowState == WindowState.Maximized)
        {
            WindowState = _lastRestoreState == WindowState.Minimized ? WindowState.Normal : _lastRestoreState;
        }
        else
        {
            _lastRestoreState = WindowState;
            WindowState = WindowState.Maximized;
        }
    }

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();

    private void OnMenuClick(object sender, RoutedEventArgs e) => MenuPopup.IsOpen = true;

    private void OnSettingsClick(object sender, RoutedEventArgs e)
    {
        MenuPopup.IsOpen = false;
        if (DataContext is MainViewModel main)
        {
            main.TryOpenAuxiliary(PageKey.Settings);
        }
    }

    private void OnAboutClick(object sender, RoutedEventArgs e)
    {
        MenuPopup.IsOpen = false;
        if (DataContext is MainViewModel main)
        {
            main.TryOpenAuxiliary(PageKey.About);
        }
    }

    private void OnUpdatesClick(object sender, RoutedEventArgs e)
    {
        MenuPopup.IsOpen = false;
        // og menu item: Check for Updates. This build ships no updater — the
        // item is present at og parity with an honest unavailable notice.
        MessageBox.Show(
            this,
            "This build checks for updates nowhere: it is a portable development build.\nReleases are published on the project's GitHub releases page.",
            "Check for Updates",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        base.OnPreviewKeyDown(e);

        if (e.Key == Key.F5 && DataContext is MainViewModel { CurrentPage: PageKey.Landing } mainLanding)
        {
            _ = mainLanding.Landing.RefreshAsync(CancellationToken.None);
            e.Handled = true;
        }
        else if (e.Key == Key.System && e.SystemKey == Key.Left && Keyboard.Modifiers == ModifierKeys.Alt)
        {
            if (DataContext is MainViewModel main)
            {
                main.TryGoBack();
            }
            e.Handled = true;
        }
        else if (e.Key == Key.F1)
        {
            if (DataContext is MainViewModel main)
            {
                main.TryOpenAuxiliary(PageKey.About);
            }
            e.Handled = true;
        }
    }
}
