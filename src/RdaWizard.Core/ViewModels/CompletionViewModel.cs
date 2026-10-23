using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using RdaWizard.Core.Models;
using RdaWizard.Core.Services;

namespace RdaWizard.Core.ViewModels;

/// <summary>
/// Completion report (og: RecoverResultDlg — UI-MAP §11): success / partial /
/// failed with reasons, per-item outcomes, report export, next actions
/// (back to results / finish). No celebration upsell — a truthful summary.
/// </summary>
public partial class CompletionViewModel : ViewModelBase
{
    private readonly ILogger<CompletionViewModel> _logger;
    private readonly IDialogService _dialogs;

    [ObservableProperty] private RecoverySummary? _summary;
    [ObservableProperty] private string? _destinationDirectory;
    [ObservableProperty] private string _headline = string.Empty;
    [ObservableProperty] private string _detail = string.Empty;
    [ObservableProperty] private bool _hasIssues;
    [ObservableProperty] private bool _wasCancelled;

    /// <summary>Report path when exported (for the "open report" action).</summary>
    [ObservableProperty] private string? _reportPath;

    public CompletionViewModel(ILogger<CompletionViewModel> logger, IDialogService dialogs)
    {
        _logger = logger;
        _dialogs = dialogs;
    }

    /// <summary>Installs the recovery outcome and renders the truthful headline.</summary>
    public void Install(RecoverySummary summary, string destinationDirectory, string? reportPath)
    {
        Summary = summary;
        DestinationDirectory = destinationDirectory;
        ReportPath = reportPath;

        WasCancelled = summary.Outcome == RecoveryOutcome.Cancelled;
        HasIssues = summary.Outcome is RecoveryOutcome.Partial or RecoveryOutcome.Failed || summary.HashMismatch > 0;

        (Headline, Detail) = summary.Outcome switch
        {
            RecoveryOutcome.Success => (
                "Recovery complete",
                $"{summary.Recovered} file(s) recovered and verified to:\n{destinationDirectory}"),
            RecoveryOutcome.Partial => (
                "Recovery finished with issues",
                $"{summary.Recovered} file(s) recovered to:\n{destinationDirectory}\n\n" +
                $"{summary.Errors} could not be read, {summary.HashMismatch} failed verification, {summary.Refused} were refused (destination safety)."),
            RecoveryOutcome.Failed => (
                "Recovery failed",
                $"Nothing was written.\n{summary.Recovered} completed before the failure: {summary.Errors} error(s)."),
            RecoveryOutcome.Cancelled => (
                "Recovery cancelled",
                $"{summary.Recovered} file(s) completed before the cancel and remain in:\n{destinationDirectory}"),
            _ => ("Recovery finished", string.Empty),
        };

        _logger.LogInformation("Completion installed: outcome={Outcome} done={Done}", summary.Outcome, summary.Recovered);
    }

    /// <summary>Opens the destination folder in Explorer (App provides the launcher).</summary>
    public static event Action<string>? OpenDestinationRequested;

    [RelayCommand]
    public void OpenDestination()
    {
        if (DestinationDirectory is null) return;
        try
        {
            OpenDestinationRequested?.Invoke(DestinationDirectory);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Open destination failed for {Path}", DestinationDirectory);
        }
    }
}
