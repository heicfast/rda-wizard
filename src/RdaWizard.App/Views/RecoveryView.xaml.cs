using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using RdaWizard.App.Services;
using RdaWizard.Core.ViewModels;

namespace RdaWizard.App.Views;

/// <summary>Recovery destination + queue view — start/cancel/browse route through the orchestrator.</summary>
public partial class RecoveryView : UserControl
{
    private ShellOrchestrator Orchestrator => App.Services.GetRequiredService<ShellOrchestrator>();

    public RecoveryView()
    {
        InitializeComponent();
    }

    private void OnBrowseDestinationClick(object sender, RoutedEventArgs e)
    {
        // Destination pick + preflight go through the orchestrator so the
        // session context and the overlap gate run in one place.
        _ = Orchestrator.PickDestinationAsync();
    }

    private void OnStartRecoveryClick(object sender, RoutedEventArgs e) =>
        _ = Orchestrator.StartRecoveryAsync();

    private void OnCancelRecoveryClick(object sender, RoutedEventArgs e) =>
        _ = Orchestrator.CancelRecoveryAsync();
}
