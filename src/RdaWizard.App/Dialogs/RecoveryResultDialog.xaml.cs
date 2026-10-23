using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using RdaWizard.App.Services;
using RdaWizard.Core.Models;

namespace RdaWizard.App.Dialogs;

/// <summary>
/// Transitional recovery result dialog (og: RecoverResultDlg). Receives the
/// summary + destination; final actions route through the orchestrator.
/// </summary>
public partial class RecoveryResultDialog : Window
{
    private static ShellOrchestrator Orchestrator => App.Services.GetRequiredService<ShellOrchestrator>();

    public string DestinationDirectory { get; init; } = string.Empty;

    public RecoveryResultDialog(RecoverySummary summary, string destination)
    {
        DestinationDirectory = destination;
        InitializeComponent();
        DataContext = new ResultViewModel(summary, destination);
    }

    private void OnOpenDestinationClick(object sender, RoutedEventArgs e)
    {
        if (Directory.Exists(DestinationDirectory))
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = DestinationDirectory,
                UseShellExecute = true,
            });
        }
    }

    private void OnFinishClick(object sender, RoutedEventArgs e)
    {
        _ = Orchestrator.FinishAsync();
        Close();
    }

    /// <summary>Truthful outcome mapping (exit-code semantics → headlines).</summary>
    public sealed record ResultViewModel(string Headline, string Detail)
    {
        public ResultViewModel(RecoverySummary summary, string destination)
            : this(BuildHeadline(summary), BuildDetail(summary, destination))
        {
        }

        private static string BuildHeadline(RecoverySummary summary) => summary.Outcome switch
        {
            RecoveryOutcome.Success => "Recovery completed",
            RecoveryOutcome.Partial => "Recovery partially completed",
            RecoveryOutcome.Failed => "Recovery failed",
            RecoveryOutcome.Cancelled => "Recovery cancelled",
            _ => "Recovery finished",
        };

        private static string BuildDetail(RecoverySummary summary, string destination) =>
            $"{summary.Recovered:N0} files were written to {destination}." +
            (summary.Errors > 0
                ? $" {summary.Errors:N0} files could not be read (usually damaged areas of the disk — see the recovery report)."
                : string.Empty);
    }
}
