using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using RdaWizard.Core.Models;
using RdaWizard.Core.Services;
using RdaWizard.Core.Settings;

namespace RdaWizard.Core.ViewModels;

/// <summary>
/// Recovery destination + queue (UI-MAP §11 recovery funnel, clean-room):
/// destination picker with the HARD overlap gate (ADR-7), pre-flight disk
/// space check, collision policy, folder-structure option, per-file queue
/// states, progress, and the report export. The engine writes; the UI
/// pre-creates directories (G-08) and renders the report (G-09).
/// </summary>
public partial class RecoveryViewModel : ViewModelBase
{
    private readonly ILogger<RecoveryViewModel> _logger;
    private readonly IRecoveryService _recovery;
    private readonly ISessionService _sessions;
    private readonly IDialogService _dialogs;
    private readonly ISettingsService _settings;

    [ObservableProperty] private string? _destinationDirectory;

    /// <summary>Bytes the pending recovery needs (selection or whole
    /// session) — drives the og insufficient-space gate (F-C-4: needed-bytes
    /// + continue-anyway, NOT a hard refusal).</summary>
    [ObservableProperty] private long _neededBytes;

    /// <summary>Free bytes at the current destination root; null when the
    /// root is unmappable.</summary>
    [ObservableProperty] private long? _destinationFreeBytes;

    /// <summary>True when free space cannot cover the needed bytes — the og
    /// "Insufficient Disk Space" gate state (soft: Continue-anyway /
    /// Reselect).</summary>
    [ObservableProperty] private bool _insufficientSpace;

    [ObservableProperty] private CollisionPolicy _collisionPolicy = CollisionPolicy.Rename;
    [ObservableProperty] private bool _preserveFolderStructure = true;
    [ObservableProperty] private bool _isPreflightRunning;
    [ObservableProperty] private bool _isRecovering;
    [ObservableProperty] private IReadOnlyList<string> _preflightRefusals = Array.Empty<string>();
    [ObservableProperty] private double _percent;
    [ObservableProperty] private long _filesDone;
    [ObservableProperty] private string? _failureDiagnostic;
    [ObservableProperty] private RecoverySummary? _summary;

    private RecoveryRequest? _pendingRequest;
    private CancellationTokenSource? _recoveryCancellation;

    /// <summary>Raised when recovery completes (shell → Complete transition).</summary>
    public event EventHandler<RecoverySummary>? RecoveryCompleted;

    /// <summary>Raised when recovery is cancelled (shell → back to Results).</summary>
    public event EventHandler<RecoverySummary>? RecoveryCancelled;

    public RecoveryViewModel(
        ILogger<RecoveryViewModel> logger,
        IRecoveryService recovery,
        ISessionService sessions,
        IDialogService dialogs,
        ISettingsService settings)
    {
        _logger = logger;
        _recovery = recovery;
        _sessions = sessions;
        _dialogs = dialogs;
        _settings = settings;

        CollisionPolicy = ParseCollisionPolicy(_settings.Current.CollisionPolicy);
        PreserveFolderStructure = _settings.Current.PreserveFolderStructure;
        DestinationDirectory = _settings.Current.DefaultRecoveryDestination;
    }

    /// <summary>Refreshes the og space-gate state from the destination
    /// root's free space vs the needed bytes (soft gate — never refuses).</summary>
    public void RefreshSpaceGate()
    {
        DestinationFreeBytes = null;
        InsufficientSpace = false;
        if (string.IsNullOrWhiteSpace(DestinationDirectory))
        {
            return;
        }

        try
        {
            var root = Path.GetPathRoot(Path.GetFullPath(DestinationDirectory));
            if (string.IsNullOrEmpty(root))
            {
                return; // relative oddity — no truthful root
            }

            var drive = new DriveInfo(root); // volume letter (Windows) or filesystem root (Unix hosts)
            if (!drive.IsReady)
            {
                return;
            }

            DestinationFreeBytes = drive.AvailableFreeSpace;
            InsufficientSpace = drive.AvailableFreeSpace < NeededBytes;
        }
        catch (ArgumentException)
        {
            // Unmappable root — no truthful free-space figure.
        }
    }

    /// <summary>Prepares the request from the current session + selection. Returns refusals (empty = GO).</summary>
    [RelayCommand]
    public async Task RunPreflightAsync(LoadedSession session, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(DestinationDirectory))
        {
            var picked = await _dialogs.PickFolderAsync("Choose a recovery destination").ConfigureAwait(true);
            if (picked is null) return;
            DestinationDirectory = picked;
        }

        IsPreflightRunning = true;
        PreflightRefusals = Array.Empty<string>();
        FailureDiagnostic = null;
        try
        {
            _pendingRequest = new RecoveryRequest
            {
                SessionPath = session.SessionPath,
                SourceDiskIndex = ResolveSourceDisk(session),
                DestinationDirectory = DestinationDirectory!,
                // Selection-wide recovery is implemented by the engine's
                // full-session queue plus UI-side subset filters; a single
                // selected-file subset uses --filter with the exact name.
                NameFilter = null,
                DeletedOnly = false,
            };

            PreflightRefusals = await _recovery.PreFlightAsync(_pendingRequest, cancellationToken).ConfigureAwait(true);
            _logger.LogInformation("Recovery preflight: {Refusals} refusal(s)", PreflightRefusals.Count);
        }
        finally
        {
            IsPreflightRunning = false;
        }
    }

    /// <summary>Executes the recovery; progress ticks per engine progress lines.</summary>
    [RelayCommand]
    public async Task RecoverAsync(CancellationToken externalToken)
    {
        if (_pendingRequest is null)
        {
            FailureDiagnostic = "Run the destination check first.";
            return;
        }

        if (PreflightRefusals.Count > 0)
        {
            // Hard gate: never start when preflight refused (ADR-7).
            _logger.LogWarning("Recovery refused by preflight: {Reasons}", string.Join("; ", PreflightRefusals));
            return;
        }

        IsRecovering = true;
        Percent = 0;
        FilesDone = 0;
        FailureDiagnostic = null;
        _recoveryCancellation = CancellationTokenSource.CreateLinkedTokenSource(externalToken);

        var progress = new Progress<RecoveryProgress>(p =>
        {
            FilesDone = p.FilesDone;
            ElapsedText = FormatElapsed(p.Elapsed);
        });

        try
        {
            Summary = await _recovery.RecoverAsync(_pendingRequest, progress, _recoveryCancellation.Token).ConfigureAwait(true);
            Percent = 100;
            _logger.LogInformation(
                "Recovery finished: {Done} done, {Refused} refused, {Errors} errors, {Mismatch} hash mismatch, outcome={Outcome}",
                Summary.Recovered, Summary.Refused, Summary.Errors, Summary.HashMismatch, Summary.Outcome);
            RecoveryCompleted?.Invoke(this, Summary);
        }
        catch (OperationCanceledException) when (_recoveryCancellation.IsCancellationRequested)
        {
            _logger.LogInformation("Recovery cancelled by user");
            var partial = new RecoverySummary
            {
                Recovered = FilesDone,
                Refused = 0,
                Errors = 0,
                HashMismatch = 0,
                Outcome = RecoveryOutcome.Cancelled,
                Elapsed = TimeSpan.Zero,
            };
            Summary = partial;
            RecoveryCancelled?.Invoke(this, partial);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Recovery failed");
            FailureDiagnostic = "Recovery failed: " + ex.Message;
        }
        finally
        {
            IsRecovering = false;
            _recoveryCancellation.Dispose();
            _recoveryCancellation = null;
        }
    }

    [RelayCommand]
    public void CancelRecovery() => _recoveryCancellation?.Cancel();

    /// <summary>Exports the completion report next to the destination (UI-owned — G-09).</summary>
    [RelayCommand]
    public async Task ExportReportAsync(CancellationToken cancellationToken)
    {
        if (Summary is null || _pendingRequest is null) return;
        var reportPath = Path.Combine(_pendingRequest.DestinationDirectory, "recovery-report.txt");
        try
        {
            var written = await _recovery.ExportReportAsync(Summary, _pendingRequest, reportPath, cancellationToken).ConfigureAwait(true);
            await _dialogs.AlertAsync("Report saved", "The recovery report was saved to:\n" + written).ConfigureAwait(true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            await _dialogs.AlertAsync("Report", "Saving the report failed: " + ex.Message).ConfigureAwait(true);
        }
    }

    [ObservableProperty] private string _elapsedText = "0:00";

    private static int ResolveSourceDisk(LoadedSession session)
    {
        // Sessions remember their source disk in our session registry (G-14:
        // engine VolumeInfo is sparse). The shell passes the disk via the
        // wizard context; the registry provides it here.
        return SessionSourceRegistry.ResolveDisk(session.SessionPath);
    }

    private static CollisionPolicy ParseCollisionPolicy(string value) => value switch
    {
        "skip" => CollisionPolicy.Skip,
        "ask" => CollisionPolicy.Ask,
        _ => CollisionPolicy.Rename,
    };

    private static string FormatElapsed(TimeSpan elapsed) =>
        elapsed.TotalHours >= 1
            ? $"{(int)elapsed.TotalHours}:{elapsed.Minutes:D2}:{elapsed.Seconds:D2}"
            : $"{elapsed.Minutes}:{elapsed.Seconds:D2}";
}

/// <summary>
/// UI-side registry mapping session files to their source disk (the engine's
/// session records are sparse — gap G-14). Written when a scan starts;
/// consulted when a session is loaded for recovery.
/// </summary>
public static class SessionSourceRegistry
{
    private static readonly Dictionary<string, int> Map = new(StringComparer.OrdinalIgnoreCase);

    public static void Record(string sessionPath, int diskIndex) => Map[sessionPath] = diskIndex;

    public static int ResolveDisk(string sessionPath) =>
        Map.TryGetValue(sessionPath, out var disk) ? disk : -1;

    public static void Clear() => Map.Clear();
}
