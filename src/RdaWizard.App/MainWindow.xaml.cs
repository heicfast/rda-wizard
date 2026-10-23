using System.Windows;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using RdaWizard.App.Services;
using RdaWizard.Core.Navigation;
using RdaWizard.Core.ViewModels;

namespace RdaWizard.App;

/// <summary>
/// Shell window: stepper + page region + status bar. Keyboard navigation:
/// Alt+Left/Right = wizard back/forward context, F5 = refresh (Home), F1 = About.
/// </summary>
public partial class MainWindow : Window
{
    private static ShellOrchestrator Orchestrator => App.Services.GetRequiredService<ShellOrchestrator>();

    public MainWindow()
    {
        InitializeComponent();
        CommandBindings.Add(new CommandBinding(NavigationCommands.Refresh, OnRefresh));
        KeyDown += OnKeyDown;
    }

    private void OnRefresh(object sender, ExecutedRoutedEventArgs e)
    {
        if (DataContext is MainViewModel main && main.CurrentPage is PageKey.Home)
        {
            _ = main.Home.RefreshAsync(CancellationToken.None);
        }
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not MainViewModel main)
        {
            return;
        }

        if (e.Key == Key.System)
        {
            if (e.SystemKey == Key.Left && Keyboard.Modifiers == ModifierKeys.Alt && main.TryGoBack())
            {
                e.Handled = true;
            }
        }
        else if (e.Key == Key.F1)
        {
            if (main.TryOpenAuxiliary(PageKey.About))
            {
                e.Handled = true;
            }
        }
    }

    private void OnSettingsClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel main)
        {
            main.TryOpenAuxiliary(PageKey.Settings);
        }
    }

    private void OnAboutClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel main)
        {
            main.TryOpenAuxiliary(PageKey.About);
        }
    }
}
