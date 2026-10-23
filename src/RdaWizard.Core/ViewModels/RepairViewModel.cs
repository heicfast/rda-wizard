using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using RdaWizard.Core.Models;
using RdaWizard.Core.Services;

namespace RdaWizard.Core.ViewModels;

/// <summary>
/// RawFixer transactional repair surface (UI-MAP §12 repair flows,
/// clean-room, AUDIT-UI ADR table "maximum gravity"):
/// 1. Analyze (dry-run — the engine writes NOTHING by default),
/// 2. Plan review with per-action detail,
/// 3. EXPLICIT safety acknowledgment (typed confirmation gate),
/// 4. Apply (undo log written alongside),
/// 5. Undo surface (replay).
/// This page writes to damaged media — every control is deliberately
/// slow-path and confirmation-gated.
/// </summary>
public partial class RepairViewModel : ViewModelBase
{
    private readonly ILogger<RepairViewModel> _logger;
    private readonly IRepairService _repair;
    private readonly IDialogService _dialogs;

    [ObservableProperty] private int _diskIndex = -1;
    [ObservableProperty] private bool _isAnalyzing;
    [ObservableProperty] private bool _isApplying;
    [ObservableProperty] private RepairResult? _result;
    [ObservableProperty] private string? _errorDiagnostic;

    /// <summary>The safety gate: user must type ACKNOWLEDGE_PHRASE to unlock Apply.</summary>
    [ObservableProperty] private string _acknowledgmentText = string.Empty;

    /// <summary>Phrase the user must type (localization renders the same rule).</summary>
    public const string AcknowledgePhrase = "repair";

    public bool AcknowledgmentValid =>
        string.Equals(AcknowledgmentText.Trim(), AcknowledgePhrase, StringComparison.OrdinalIgnoreCase);

    public RepairViewModel(ILogger<RepairViewModel> logger, IRepairService repair, IDialogService dialogs)
    {
        _logger = logger;
        _repair = repair;
        _dialogs = dialogs;
    }

    partial void OnAcknowledgmentTextChanged(string value) => OnPropertyChanged(nameof(AcknowledgmentValid));

    /// <summary>Opens with dry-run analysis of the whole disk (or a partition).</summary>
    public async Task OpenAsync(int diskIndex, int? partitionIndex, CancellationToken cancellationToken)
    {
        DiskIndex = diskIndex;
        await AnalyzeAsync(partitionIndex, cancellationToken).ConfigureAwait(true);
    }

    [RelayCommand]
    public async Task AnalyzeAsync(int? partitionIndex, CancellationToken cancellationToken)
    {
        if (DiskIndex < 0) return;
        IsAnalyzing = true;
        ErrorDiagnostic = null;
        AcknowledgmentText = string.Empty;
        try
        {
            Result = await _repair.AnalyzeAsync(DiskIndex, partitionIndex, cancellationToken).ConfigureAwait(true);
            _logger.LogInformation("Repair analyze: phase={Phase} actions={Actions} refusal={Refusal}",
                Result.Phase, Result.Actions.Count, Result.RefusalReason ?? "-");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Repair analyze failed for disk {Disk}", DiskIndex);
            ErrorDiagnostic = "Analysis failed: " + ex.Message;
        }
        finally
        {
            IsAnalyzing = false;
        }
    }

    /// <summary>Applies the repair — only valid after AcknowledgmentValid (the view enforces the gate too).</summary>
    [RelayCommand]
    public async Task ApplyAsync(CancellationToken cancellationToken)
    {
        if (Result is not { Phase: RepairPhase.DryRun or RepairPhase.AwaitingConsent } || Result.Actions.Count == 0)
        {
            return;
        }

        if (!AcknowledgmentValid)
        {
            await _dialogs.AlertAsync(
                "Safety acknowledgment required",
                "Type the acknowledgment word to confirm you understand this writes to the damaged disk.").ConfigureAwait(true);
            return;
        }

        var confirmed = await _dialogs.ConfirmAsync(
            "Write repairs to the disk?",
            $"{Result.Actions.Count} repair action(s) will be written to disk {DiskIndex} and verified. " +
            "An undo log will be saved next to the session folder.\nThis writes to the source media — continue?",
            "Apply repairs",
            destructive: true).ConfigureAwait(true);
        if (confirmed != DialogResult.Confirmed) return;

        IsApplying = true;
        ErrorDiagnostic = null;
        try
        {
            var undoPath = BuildUndoLogPath();
            Result = await _repair.ApplyAsync(DiskIndex, partitionIndex: null, undoPath, cancellationToken).ConfigureAwait(true);
            _logger.LogInformation("Repair applied: {Sectors} sector(s), undo log {Undo}",
                Result.SectorsWritten, Result.UndoLogPath ?? "-");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Repair apply failed for disk {Disk}", DiskIndex);
            ErrorDiagnostic = "Applying the repair failed: " + ex.Message + " — verify the disk state before retrying.";
        }
        finally
        {
            IsApplying = false;
        }
    }

    /// <summary>Replays an undo log (restores pre-repair sectors).</summary>
    [RelayCommand]
    public async Task UndoAsync(string undoLogPath, CancellationToken cancellationToken)
    {
        IsApplying = true;
        ErrorDiagnostic = null;
        try
        {
            Result = await _repair.UndoAsync(undoLogPath, cancellationToken).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Repair undo failed from {Path}", undoLogPath);
            ErrorDiagnostic = "Undo failed: " + ex.Message + " — verify the disk state.";
        }
        finally
        {
            IsApplying = false;
        }
    }

    private string BuildUndoLogPath()
    {
        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "RdaWizard", "repair");
        Directory.CreateDirectory(root);
        return Path.Combine(root, $"undo-disk{DiskIndex}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.bin");
    }
}
