using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using RdaWizard.App.Services;

namespace RdaWizard.App.Views;

/// <summary>Scan progress view — cancel routes through the orchestrator (confirm → engine cancel).</summary>
public partial class ScanProgressView : UserControl
{
    private ShellOrchestrator Orchestrator => App.Services.GetRequiredService<ShellOrchestrator>();

    public ScanProgressView()
    {
        InitializeComponent();
    }

    private void OnCancelScanClick(object sender, RoutedEventArgs e) =>
        _ = Orchestrator.CancelScanAsync();
}
