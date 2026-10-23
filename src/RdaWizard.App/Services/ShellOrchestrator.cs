using System.IO;
using System.Windows;
using Microsoft.Extensions.Logging;
using RdaWizard.Core.Models;
using RdaWizard.Core.Navigation;
using RdaWizard.Core.Services;
using RdaWizard.Core.ViewModels;
using RdaWizard.App.Dialogs;
using RdaWizard.Core.Wizard;

namespace RdaWizard.App.Services;

/// <summary>
/// The flow glue between page ViewModels and the wizard state machine,
/// mirroring the og wiring (DrwMainViewManagerWizard): the landing CTA fires
/// scanClickedSlot → StartScan transition → scan starts ON the Detail page;
/// scan completion installs the session into the Detail results browser;
/// recovery is a modal family launched from the Detail page's Recover CTA.
/// Session paths for each scan run are generated and tracked
/// (SessionSourceRegistry feeds the overlap gate's source-disk resolution).
/// </summary>
public sealed class ShellOrchestrator : IDisposable
{
    private readonly ILogger<ShellOrchestrator> _logger;
    private readonly ISessionService _sessions;
    private readonly IDialogService _dialogs;
    private readonly IDeviceEnumerationService _devices;

    private MainViewModel _main = null!;
    private CancellationTokenSource _flowCancellation = new();

    public ShellOrchestrator(
        ILogger<ShellOrchestrator> logger,
        ISessionService sessions,
        IDialogService dialogs,
        IDeviceEnumerationService devices)
    {
        _logger = logger;
        _sessions = sessions;
        _dialogs = dialogs;
        _devices = devices;
    }

    public void Wire(MainViewModel main)
    {
        _main = main;

        _main.Landing.ScanRequested += OnScanRequested;
        _main.Landing.ReturnToScanRequested += OnReturnToScanRequested;
        _main.Landing.SessionLoaded += OnSessionLoaded;
        _main.Detail.ScanCompleted += OnScanCompleted;
        _main.Detail.ScanFailed += OnScanFailed;
        _main.Detail.RecoverRequested += OnRecoverRequested;
        _main.Recovery.RecoveryCompleted += OnRecoveryCompleted;
        _main.Recovery.RecoveryCancelled += OnRecoveryCancelled;
        _main.MoreTools.ToolActivated += OnToolActivated;
        _main.FileRepair.VolumeRepairRequested += OnVolumeRepairRequested;
        _main.LostPartition.StartRequested += OnLostPartitionStartRequested;
        AboutViewModel.LicensesRequested += () => ShowLicenses();

        _logger.LogInformation("Shell orchestrator wired (og shell model)");
    }

    /// <summary>Landing CTA ("Search for Lost Data"): StartScan transition → scan starts immediately on the Detail page.</summary>
    public async Task StartScanAsync(ScanTarget target)
    {
        if (target is null)
        {
            await _dialogs.AlertAsync("Start a scan", "Select a location to search for lost data first.").ConfigureAwait(true);
            return;
        }

        var context = CurrentContext();
        var reason = _main.ExecuteTransition("StartScan", context);
        if (reason is not null)
        {
            await _dialogs.AlertAsync("Start a scan", reason).ConfigureAwait(true);
            return;
        }

        var sessionPath = await BuildSessionPathAsync(target).ConfigureAwait(true);
        var stageLogPath = Path.ChangeExtension(sessionPath, ".stage.log");

        // The og has no scan-configuration step: default mode (deep + carve),
        // engine-default threads, no engine-side name filter — all refinement
        // happens client-side in the results browser (og filter bar).
        var options = new ScanOptions
        {
            Target = target,
            Mode = ScanMode.DeepPlusCarve,
            SessionPath = sessionPath,
            StageLogPath = stageLogPath,
        };
        SessionSourceRegistry.Record(sessionPath, target.DiskIndex);

        _main.NavigateForState();

        _flowCancellation.Dispose();
        _flowCancellation = new CancellationTokenSource();
        _ = _main.Detail.RunScanAsync(options, _flowCancellation.Token);
    }

    /// <summary>Detail page stop request: og stop-confirm ("Stop Searching for Lost Data?") then StopScan transition.</summary>
    public async Task StopScanAsync()
    {
        var confirmed = await _dialogs.ConfirmAsync(
            "Stop Searching for Lost Data?",
            "Remaining data can be rescanned and recovered anytime later, but continuing now saves you time.",
            "Stop",
            destructive: true).ConfigureAwait(true);
        if (confirmed != DialogResult.Confirmed)
        {
            return;
        }

        _main.Detail.CancelScan();
        // The ScanFailed event fires with "cancelled"; the handler routes home.
    }

    /// <summary>Recovery funnel: preflight + StartRecovery transition + modal recovery run.</summary>
    public async Task StartRecoveryAsync(RecoveryRequestKind kind)
    {
        if (_main.Detail.Session is null)
        {
            await _dialogs.AlertAsync("Recover files", "Load or complete a scan first.").ConfigureAwait(true);
            return;
        }

        if (kind == RecoveryRequestKind.Selection && _main.Detail.SelectedNodes.Count == 0)
        {
            await _dialogs.AlertAsync("Recover files", "Select at least one file to recover.").ConfigureAwait(true);
            return;
        }

        if (string.IsNullOrWhiteSpace(_main.Recovery.DestinationDirectory))
        {
            await PickDestinationAsync().ConfigureAwait(true);
        }

        if (string.IsNullOrWhiteSpace(_main.Recovery.DestinationDirectory))
        {
            return; // user dismissed the destination dialog
        }

        // Preflight first: it computes the overlap gate the transition guard checks.
        await _main.Recovery.RunPreflightAsync(_main.Detail.Session, _flowCancellation.Token).ConfigureAwait(true);
        if (_main.Recovery.PreflightRefusals.Count > 0)
        {
            // Hard gate (ADR-7): refuse with the reasons before any transition.
            await _dialogs.AlertAsync(
                "Destination not safe",
                string.Join(Environment.NewLine, _main.Recovery.PreflightRefusals)).ConfigureAwait(true);
            return;
        }

        // og "Insufficient Disk Space" gate (F-C-4: needed-bytes +
        // continue-anyway — never a silent hard refusal).
        _main.Recovery.NeededBytes = RecoveryNeededBytes(kind);
        _main.Recovery.RefreshSpaceGate();
        if (_main.Recovery.InsufficientSpace)
        {
            var deficit = _main.Recovery.NeededBytes - (_main.Recovery.DestinationFreeBytes ?? 0);
            var proceed = await _dialogs.ConfirmAsync(
                "Insufficient Disk Space",
                $"Need an additional {deficit / (1024.0 * 1024 * 1024):0.##} GB of space. Please free up space or select another drive.",
                "Continue").ConfigureAwait(true);
            if (proceed != DialogResult.Confirmed)
            {
                await PickDestinationAsync().ConfigureAwait(true);
                if (string.IsNullOrWhiteSpace(_main.Recovery.DestinationDirectory))
                {
                    return;
                }
                _main.Recovery.RefreshSpaceGate();
                if (_main.Recovery.InsufficientSpace)
                {
                    return; // reselected destination is still short — stop honestly
                }
            }
        }

        var context = CurrentContext();
        var reason = _main.ExecuteTransition("StartRecovery", context);
        if (reason is not null)
        {
            await _dialogs.AlertAsync("Recover files", reason).ConfigureAwait(true);
            return;
        }

        ShowRecoveryProgress();
        _ = _main.Recovery.RecoverAsync(_flowCancellation.Token);
    }

    /// <summary>og ChooseDestinationPathDialog (volume rows with capacity +
    /// source-disk warning + "Choose folder..." row + Save gated on a
    /// selection) + immediate preflight re-run.</summary>
    public async Task PickDestinationAsync()
    {
        if (_main.Detail.Session is null)
        {
            await _dialogs.AlertAsync("Recover files", "Load or complete a scan first.").ConfigureAwait(true);
            return;
        }

        var enumeration = await _devices.EnumerateAsync(_flowCancellation.Token).ConfigureAwait(true);
        var sourceDisk = ResolveSourceDisk(_main.Detail.Session);
        var needed = RecoveryNeededBytes(RecoveryRequestKind.All);
        var fileCount = _main.Detail.TotalFileCount;

        var dialog = new ChooseDestinationDialog(enumeration.Volumes, sourceDisk, needed, fileCount);
        if (dialog.ShowDialog() != true || dialog.SelectedPath is null)
        {
            return;
        }

        _main.Recovery.DestinationDirectory = dialog.SelectedPath;
        await _main.Recovery.RunPreflightAsync(_main.Detail.Session, _flowCancellation.Token).ConfigureAwait(true);
    }

    /// <summary>Bytes the recovery funnel needs: the checked selection when
    /// present, the whole session otherwise.</summary>
    private long RecoveryNeededBytes(RecoveryRequestKind kind)
    {
        var selected = _main.Detail.SelectedNodes;
        return kind == RecoveryRequestKind.Selection && selected.Count > 0
            ? selected.Sum(n => n.LogicalSize)
            : _main.Detail.FoundBytes;
    }

    private static int ResolveSourceDisk(LoadedSession session)
    {
        // The scan recorded the source disk when it wrote the session
        // (SessionSourceRegistry); fall back to the session-path convention.
        return SessionSourceRegistry.ResolveDisk(session.SessionPath);
    }

    /// <summary>Recovering: cancel-with-confirmation.</summary>
    public async Task CancelRecoveryAsync()
    {
        var confirmed = await _dialogs.ConfirmAsync(
            "Cancel the recovery?",
            "Files already written remain in the destination folder. Cancel the remaining recovery?",
            "Cancel recovery",
            destructive: true).ConfigureAwait(true);
        if (confirmed != DialogResult.Confirmed)
        {
            return;
        }
        _main.Recovery.CancelRecovery();
    }

    /// <summary>Result dialog: Finish → reset wizard for a new run.</summary>
    public async Task FinishAsync()
    {
        _main.ResetWizard();
        await Task.CompletedTask.ConfigureAwait(true);
    }

    private async void OnScanRequested(object? sender, ScanTarget target)
    {
        await StartScanAsync(target).ConfigureAwait(true);
    }

    /// <summary>CTA pressed while a scan already runs (sidebar return):
    /// navigate back to the Detail scan surface — never restart the scan.</summary>
    private void OnReturnToScanRequested(object? sender, EventArgs e)
    {
        if (_main.WizardState == WizardState.Scanning)
        {
            _main.NavigateForState();
        }
    }

    private async void OnSessionLoaded(object? sender, LoadedSession session)
    {
        // ImportSession edge → Detail results directly (skips scan).
        var context = CurrentContext();
        var reason = _main.ExecuteTransition("ImportSession", context);
        if (reason is not null)
        {
            await _dialogs.AlertAsync("Open session", reason).ConfigureAwait(true);
            return;
        }
        _main.Detail.InstallSession(session);
        _main.NavigateForState();
    }

    private async void OnScanCompleted(object? sender, ScanSummary summary)
    {
        var reason = _main.ExecuteTransition("ScanCompleted", CurrentContext());
        if (reason is not null)
        {
            await _dialogs.AlertAsync("Scan finished", reason).ConfigureAwait(true);
            return;
        }

        // Load the freshly written session into the Detail results browser.
        var loaded = await _sessions.LoadAsync(summary.SessionPath ?? string.Empty).ConfigureAwait(true);
        if (loaded.Success && loaded.Session is { } session)
        {
            _main.Detail.InstallSession(session);
        }
        else
        {
            _logger.LogWarning("Post-scan session load failed: {Diagnostic}", loaded.Diagnostic);
            await _dialogs.AlertAsync(
                "Scan finished",
                "The scan finished but reading its results failed: " + loaded.Diagnostic).ConfigureAwait(true);
            _main.ExecuteTransition("BackToSelect", CurrentContext());
        }
        _main.NavigateForState();
    }

    private async void OnScanFailed(object? sender, string diagnostic)
    {
        // Stop route: state → Select; failures also return to the landing.
        var context = CurrentContext() with { CancelConfirmed = true };
        _main.ExecuteTransition("StopScan", context);
        _main.NavigateForState();
        await _dialogs.AlertAsync("Scan stopped", diagnostic).ConfigureAwait(true);
    }

    private async void OnRecoverRequested(object? sender, RecoveryRequestKind kind)
    {
        await StartRecoveryAsync(kind).ConfigureAwait(true);
    }

    private async void OnRecoveryCompleted(object? sender, RecoverySummary summary)
    {
        CloseRecoveryProgress();
        var reason = _main.ExecuteTransition("RecoveryFinished", CurrentContext());
        if (reason is not null)
        {
            await _dialogs.AlertAsync("Recovery finished", reason).ConfigureAwait(true);
            return;
        }
        _main.NavigateForState();
        ShowRecoveryResult(summary);
    }

    private async void OnRecoveryCancelled(object? sender, RecoverySummary summary)
    {
        CloseRecoveryProgress();
        var context = CurrentContext() with { CancelConfirmed = true };
        var reason = _main.ExecuteTransition("CancelRecovery", context);
        if (reason is not null)
        {
            await _dialogs.AlertAsync("Recovery cancelled", reason).ConfigureAwait(true);
            return;
        }
        _main.NavigateForState();
        ShowRecoveryResult(summary);
    }

    private void ShowRecoveryProgress()
    {
        PostOnUi(() =>
        {
            CloseRecoveryProgress();
            _recoveryProgressDialog = new Dialogs.RecoveryProgressDialog
            {
                Owner = System.Windows.Application.Current?.MainWindow,
                DataContext = _main.Recovery,
            };
            _recoveryProgressDialog.Show();
        });
    }

    private void CloseRecoveryProgress()
    {
        PostOnUi(() =>
        {
            _recoveryProgressDialog?.Close();
            _recoveryProgressDialog = null;
        });
    }

    private void ShowRecoveryResult(RecoverySummary summary)
    {
        PostOnUi(() =>
        {
            var dialog = new Dialogs.RecoveryResultDialog(summary, _main.Recovery.DestinationDirectory ?? string.Empty)
            {
                Owner = System.Windows.Application.Current?.MainWindow,
            };
            dialog.ShowDialog();
        });
    }

    private Dialogs.RecoveryProgressDialog? _recoveryProgressDialog;

    private static void PostOnUi(Action action)
    {
        var dispatcher = System.Windows.Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.CheckAccess())
        {
            action();
        }
        else
        {
            dispatcher.Invoke(action);
        }
    }

    private void OnToolActivated(object? sender, MoreToolsViewModel.ToolCard tool)
    {
        if (tool.IsAvailableInThisBuild && tool.Title == "Disk Health")
        {
            // Opens with physical disk 0; the disk selector arrives with the
            // Health page rebuild (og "Which disk do you care about?").
            _ = _main.Health.OpenAsync(0, CancellationToken.None);
            _main.TryOpenAuxiliary(PageKey.Health);
        }
    }

    private void OnVolumeRepairRequested(object? sender, EventArgs e)
    {
        _main.TryOpenAuxiliary(PageKey.Repair);
    }

    private void OnLostPartitionStartRequested(object? sender, EventArgs e)
    {
        // The lost-partition flow starts from a whole-disk deep scan: route to
        // the landing with the disk list (the user picks the disk to scan).
        _main.TryOpenAuxiliary(PageKey.Landing);
    }

    private WizardContext CurrentContext() => new()
    {
        HasScanTarget = _main.Landing.SelectedTarget is not null,
        HasLoadedSession = _main.Detail.Session is not null,
        HasRecoverySelection = _main.Detail.SelectedNodes.Count > 0,
        DestinationOverlapCheckPassed = _main.Recovery.PreflightRefusals.Count == 0 &&
                                        _main.Recovery.DestinationDirectory is not null,
        CancelConfirmed = false,
    };

    private async Task<string> BuildSessionPathAsync(ScanTarget target)
    {
        var sessionsDirectory = _sessions.SessionsDirectory;
        var stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss", System.Globalization.CultureInfo.InvariantCulture);
        var safeDescription = string.Join("_", target.Description.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries))
            .TrimEnd('.');
        var name = $"scan-disk{target.DiskIndex}-{safeDescription}-{stamp}.crsf";
        var path = Path.Combine(sessionsDirectory, name);
        _logger.LogInformation("Session path allocated: {Path}", path);
        await Task.CompletedTask.ConfigureAwait(true);
        return path;
    }

    private static void ShowLicenses()
    {
        var licenses = LicenseText.Build();
        System.Windows.MessageBox.Show(licenses, "Open-source licenses", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    public void Dispose()
    {
        _flowCancellation.Dispose();
    }
}

/// <summary>Static license text (OSS inventory of our dependency set).</summary>
public static class LicenseText
{
    public static string Build() => """
        RDA Data Recovery Wizard uses the following open-source components:

        - CommunityToolkit.Mvvm 8.4.2 — MIT License
        - Microsoft.Extensions.* 10.0.x — MIT License
        - Serilog 4.4.0 + Sinks.File 7.0.0 — Apache License 2.0
        - Docnet.Core 2.6.0 (PDF preview) — MIT License
        - FlaUI 5.0.0 (test automation only) — MIT License

        This application does not include telemetry of any kind. Logs are
        stored locally under %LOCALAPPDATA%\RdaWizard\logs.
        """;
}
