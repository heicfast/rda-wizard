using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using RdaWizard.App.Services;
using RdaWizard.Core.Models;
using RdaWizard.Core.ViewModels;

namespace RdaWizard.App.Views;

/// <summary>
/// Results explorer view. Selection changes flow into the VM's selection
/// set (recovery input); the recover CTA routes through the orchestrator.
/// </summary>
public partial class ResultsView : UserControl
{
    private static ShellOrchestrator Orchestrator => App.Services.GetRequiredService<ShellOrchestrator>();

    public ResultsView()
    {
        InitializeComponent();
    }

    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not ResultsViewModel results)
        {
            return;
        }

        foreach (ResultNode added in e.AddedItems)
        {
            results.SelectNode(added);
        }
        foreach (ResultNode removed in e.RemovedItems)
        {
            results.DeselectNode(removed);
        }
    }

    private void OnRecoverClick(object sender, RoutedEventArgs e) =>
        _ = Orchestrator.StartRecoveryAsync();
}
