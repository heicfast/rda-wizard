using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using RdaWizard.App.Services;
using RdaWizard.Core.Navigation;
using RdaWizard.Core.ViewModels;

namespace RdaWizard.App.Views;

/// <summary>
/// Detail page (og: CDetailPage): scan progress + results on one surface.
/// Toolbar navigation + stop run through the shell (established pattern);
/// all data interaction is command-bound.
/// </summary>
public partial class DetailView : UserControl
{
    public DetailView() => InitializeComponent();

    private static MainViewModel Main => App.Services.GetRequiredService<MainViewModel>();

    private static ShellOrchestrator Orchestrator => App.Services.GetRequiredService<ShellOrchestrator>();

    /// <summary>og toolbar Home: returns to the landing (a running scan
    /// continues app-level in the background — the og runs CScanMgr
    /// outside the page).</summary>
    private void OnHomeClick(object sender, System.Windows.RoutedEventArgs e)
    {
        _ = Main.TryOpenAuxiliary(PageKey.Landing);
    }

    /// <summary>og toolbar Back: shell history back.</summary>
    private void OnBackClick(object sender, System.Windows.RoutedEventArgs e)
    {
        _ = Main.TryGoBack();
    }

    /// <summary>og bottom-bar Stop: routes to the shell's stop-confirm
    /// ("Stop Searching for Lost Data?" — Stop red / Continue blue).</summary>
    private async void OnStopClick(object sender, System.Windows.RoutedEventArgs e)
    {
        await Orchestrator.StopScanAsync().ConfigureAwait(true);
    }
}
