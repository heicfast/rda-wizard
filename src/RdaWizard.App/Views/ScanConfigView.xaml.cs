using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using RdaWizard.App.Services;
using RdaWizard.Core.ViewModels;

namespace RdaWizard.App.Views;

/// <summary>Scan configuration view — start goes through the orchestrator (guarded transition).</summary>
public partial class ScanConfigView : UserControl
{
    private ShellOrchestrator Orchestrator => App.Services.GetRequiredService<ShellOrchestrator>();

    public ScanConfigView()
    {
        InitializeComponent();
    }

    private void OnStartScanClick(object sender, RoutedEventArgs e) =>
        _ = Orchestrator.StartScanAsync();
}
