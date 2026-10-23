using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using RdaWizard.App.Services;
using RdaWizard.Core.Models;
using RdaWizard.Core.ViewModels;

namespace RdaWizard.App.Views;

/// <summary>
/// Home / device selection view. The "Search for lost data" CTA goes
/// through the ShellOrchestrator (guarded BeginScanSetup transition);
/// disk/volume selection is VM-command driven.
/// </summary>
public partial class HomeView : UserControl
{
    private static ShellOrchestrator Orchestrator => App.Services.GetRequiredService<ShellOrchestrator>();

    public HomeView()
    {
        InitializeComponent();
    }

    private void OnBeginScanClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not HomeViewModel home)
        {
            return;
        }

        if (home.SelectedTarget is null)
        {
            var main = App.Services.GetRequiredService<MainViewModel>();
            _ = ShowSelectionHintAsync(main);
            return;
        }

        _ = Orchestrator.BeginScanSetupAsync();
    }

    private static async Task ShowSelectionHintAsync(MainViewModel main)
    {
        var dialogs = App.Services.GetRequiredService<Core.Services.IDialogService>();
        await dialogs.AlertAsync(
            "Choose what to scan",
            "Select a disk or a drive from the list first — the wizard needs to know where to look.").ConfigureAwait(true);
        _ = main; // reserved for focus placement
    }
}
