using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using RdaWizard.Core.Models;
using RdaWizard.Core.Services;

namespace RdaWizard.Core.ViewModels;

/// <summary>
/// Scan progress surface (UI-MAP §8 deep-dive, clean-room). TRUTHFUL
/// telemetry: the engine streams an overall weighted percentage across
/// quick → deep → carve (monotonic; rrecov.cpp:81), read-error ranges
/// stream live from the stage log, files-found appears at completion.
/// ETA is a linear estimate shown as approximate. No Pause button (no
/// resume API — gap G-07): Cancel asks for confirmation with the real
/// consequence — results found so far are lost.
/// </summary>
public partial class ScanProgressViewModel : ViewModelBase
{
    private readonly ILogger<ScanProgressViewModel> _logger;
    private readonly IScanService _scans;
    private readonly IDialogService _dialogs;
    private readonly IClock _clock;

    private CancellationTokenSource? _scanCancellation;

    [ObservableProperty] private bool _isRunning;
    [ObservableProperty] private double _percent;
    [ObservableProperty] private string _elapsedText = "0:00";
    [ObservableProperty] private string? _etaText;
    [ObservableProperty] private long _readErrorCount;
    [ObservableProperty] private long _readErrorBytes;
    [ObservableProperty] private string? _transportLabel;
    [ObservableProperty] private string? _failureDiagnostic;
    [ObservableProperty] private IReadOnlyList<ReadErrorRange> _readErrors = Array.Empty<ReadErrorRange>();

    /// <summary>Files-found appears only when the engine reports it (completion).</summary>
    [ObservableProperty] private ScanSummary? _summary;

    /// <summary>Raised when the scan completed successfully with a saved session.</summary>
    public event EventHandler<ScanSummary>? ScanCompleted;

    /// <summary>Raised when the scan failed or was cancelled (shell returns to Home).</summary>
    public event EventHandler<string>? ScanFailed;

    public ScanProgressViewModel(ILogger<ScanProgressViewModel> logger, IScanService scans, IDialogService dialogs, IClock clock)
    {
        _logger = logger;
        _scans = scans;
        _dialogs = dialogs;
        _clock = clock;
    }

    /// <summary>True while a scan is executing — the Cancel command's availability.</summary>
    public bool IsScanActive => IsRunning;

    /// <summary>Runs the scan; called by the shell after the StartScan transition.</summary>
    public async Task RunScanAsync(ScanOptions options, CancellationToken externalToken)
    {
        if (IsRunning)
        {
            _logger.LogWarning("Scan already running; ignoring duplicate start");
            return;
        }

        IsRunning = true;
        Percent = 0;
        Summary = null;
        FailureDiagnostic = null;
        ReadErrors = Array.Empty<ReadErrorRange>();
        ReadErrorCount = 0;
        ReadErrorBytes = 0;
        _scanCancellation = CancellationTokenSource.CreateLinkedTokenSource(externalToken);

        var progress = new Progress<ScanProgressSnapshot>(snapshot =>
        {
            // Progress<T> marshals to the captured sync context (UI thread in the app).
            if (snapshot.EngineFraction is { } fraction)
            {
                Percent = Math.Clamp(fraction * 100.0, 0.0, 100.0);
                var elapsedSeconds = snapshot.Elapsed.TotalSeconds;
                if (fraction > 0.02)
                {
                    var etaSeconds = elapsedSeconds * (1.0 - fraction) / fraction;
                    EtaText = "≈ " + FormatElapsed(TimeSpan.FromSeconds(etaSeconds));
                }
            }
            else
            {
                EtaText = null;
            }

            ElapsedText = FormatElapsed(snapshot.Elapsed);
            ReadErrors = snapshot.ReadErrors;
            ReadErrorCount = snapshot.ReadErrors.Count;
            ReadErrorBytes = snapshot.ReadErrorBytes;
        });

        try
        {
            var summary = await _scans.RunScanAsync(options, progress, _scanCancellation.Token).ConfigureAwait(true);
            Summary = summary;
            Percent = 100;
            _logger.LogInformation(
                "Scan completed: files={Files} deleted={Deleted} carved={Carved} lost={Lost} elapsed={Elapsed:F0}s session={Session}",
                summary.Files, summary.Deleted, summary.Carved, summary.LostPartitions, summary.Elapsed.TotalSeconds, summary.SessionPath ?? "none");
            ScanCompleted?.Invoke(this, summary);
        }
        catch (OperationCanceledException) when (_scanCancellation.IsCancellationRequested)
        {
            _logger.LogInformation("Scan cancelled by user");
            FailureDiagnostic = "The scan was cancelled. Results found so far were not saved.";
            ScanFailed?.Invoke(this, FailureDiagnostic);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Scan failed");
            FailureDiagnostic = "The scan stopped: " + ex.Message;
            ScanFailed?.Invoke(this, FailureDiagnostic);
        }
        finally
        {
            TransportLabel = _scans.LastTransportLabel;
            IsRunning = false;
            _scanCancellation.Dispose();
            _scanCancellation = null;
        }
    }

    /// <summary>Requests cancellation of the running scan (the shell confirms first).</summary>
    public void CancelScan() => _scanCancellation?.Cancel();

    private static string FormatElapsed(TimeSpan elapsed) =>
        elapsed.TotalHours >= 1
            ? $"{(int)elapsed.TotalHours}:{elapsed.Minutes:D2}:{elapsed.Seconds:D2}"
            : $"{elapsed.Minutes}:{elapsed.Seconds:D2}";
}
