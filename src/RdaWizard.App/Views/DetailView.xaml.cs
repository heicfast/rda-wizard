using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using RdaWizard.App.Services;
using RdaWizard.Core.Models;
using RdaWizard.Core.Navigation;
using RdaWizard.Core.ViewModels;

namespace RdaWizard.App.Views;

/// <summary>
/// Detail page (og: CDetailPage): scan progress + results on one surface.
/// Toolbar navigation, stop, and filter-bar picks run through the shell/VM
/// (established pattern); all data interaction is command-bound.
/// </summary>
public partial class DetailView : UserControl
{
    public DetailView() => InitializeComponent();

    private static MainViewModel Main => App.Services.GetRequiredService<MainViewModel>();

    private static ShellOrchestrator Orchestrator => App.Services.GetRequiredService<ShellOrchestrator>();

    private static DetailViewModel Detail => App.Services.GetRequiredService<DetailViewModel>();

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

    // ---- Filter bar (og CFilterBar dropdown picks) ----

    private void OnShowPick(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (Pick(sender) is { } preset)
        {
            Detail.FilterShow(preset);
        }
    }

    private void OnCategoryPick(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (Pick(sender) is { } preset)
        {
            Detail.FilterCategory(preset);
        }
    }

    private void OnDatePick(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (Pick(sender) is { } preset)
        {
            Detail.FilterDateRange(preset);
        }
    }

    private void OnSizePick(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (Pick(sender) is { } preset)
        {
            Detail.FilterSizeRange(preset);
        }
    }

    private static string? Pick(object sender) =>
        sender is ComboBox { SelectedItem: ComboBoxItem item } ? item.Content?.ToString() : null;

    // ---- Tree → list navigation (og tree selection scopes the list) ----

    private void OnTreeItemSelected(object sender, System.Windows.RoutedPropertyChangedEventArgs<object> e)
    {
        if (e.NewValue is DetailViewModel.TreeNodeItem { Node: { } node })
        {
            Detail.ScopeToNode(node);
        }
    }
}
