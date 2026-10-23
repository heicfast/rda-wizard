using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using RdaWizard.App.Services;
using RdaWizard.App.Views;

namespace RdaWizard.App.Views;

/// <summary>Completion view — final actions route through the orchestrator.</summary>
public partial class CompletionView : UserControl
{
    private ShellOrchestrator Orchestrator => App.Services.GetRequiredService<ShellOrchestrator>();

    public CompletionView()
    {
        InitializeComponent();
    }

    private void OnBackToResultsClick(object sender, RoutedEventArgs e) =>
        _ = Orchestrator.BackToResultsAsync();

    private void OnFinishClick(object sender, RoutedEventArgs e) =>
        _ = Orchestrator.FinishAsync();
}
