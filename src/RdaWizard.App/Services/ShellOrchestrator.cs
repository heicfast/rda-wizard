using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RdaWizard.Core.Models;
using RdaWizard.Core.Navigation;
using RdaWizard.Core.Services;
using RdaWizard.Core.ViewModels;
using RdaWizard.Core.Wizard;

namespace RdaWizard.App.Services;

/// <summary>
/// The flow glue between page ViewModels and the wizard state machine:
/// VM events (session loaded, scan started/completed, recovery finished)
/// become guarded state transitions + navigation. Session paths for each
/// scan run are generated and tracked (SessionSourceRegistry feeds the
/// overlap gate's source-disk resolution).
/// </summary>
public sealed class ShellOrchestrator
{
    private readonly ILogger<ShellOrchestrator> _logger;
    private readonly IServiceProvider _services;
    private readonly ISessionService _sessions;
    private readonly IDialogService _dialogs;

    private MainViewModel _main = null!;
    private CancellationTokenSource _flowCancellation = new();
    private ScanOptions? _activeScanOptions;

    public ShellOrchestrator(
        ILogger<ShellOrchestrator> logger,
        IServiceProvider services,
        ISessionService sessions,
        IDialogService dialogs)
    {
        _logger = logger;
        _services = services;
        _sessions = sessions;
        _dialogs = dialogs;
    }

    public void Wire(MainViewModel main)
    {
        _main = main;

        _main.Home.SessionLoaded += OnSessionLoaded;
        _main.ScanConfig.ScanStarted += OnScanStarted;
        _main.ScanProgress.ScanCompleted += OnScanCompleted;
        _main.ScanProgress.ScanFailed += OnScanFailed;
        _main.Recovery.RecoveryCompleted += OnRecoveryCompleted;
        _main.Recovery.RecoveryCancelled += OnRecoveryCancelled;
        CompletionViewModel.OpenDestinationRequested += path => OpenInExplorer(path);
        AboutViewModel.LicensesRequested += () => ShowLicenses();

        _logger.LogInformation("Shell orchestrator wired");
    }

    /// <summary>Home: BeginScanSetup transition + navigate to config.</summary>
    public async Task BeginScanSetupAsync()
    {
        var context = CurrentContext();
        var reason = _main.ExecuteTransition("BeginScanSetup", context);
        if (reason is not null)
        {
            await _dialogs.AlertAsync("Start a scan", reason).ConfigureAwait(true);
            return;
        }
        _main.ScanConfig.Initialize(_main.Home.SelectedTarget!);
        _main.NavigateForState();
    }

    /// <summary>Config: StartScan transition + launch the engine scan.</summary>
    public async Task StartScanAsync()
    {
        var sessionPath = await BuildSessionPathAsync(_main.ScanConfig.Target!).ConfigureAwait(true);
        var stageLogPath = Path.ChangeExtension(sessionPath, ".stage.log");

        var options = _main.ScanConfig.StartScan(sessionPath, stageLogPath);
        SessionSourceRegistry.Record(sessionPath, options.Target.DiskIndex);

        var reason = _main.ExecuteTransition("StartScan", CurrentContext());
        if (reason is not null)
        {
            await _dialogs.AlertAsync("Start a scan", reason).ConfigureAwait(true);
            return;
        }

        _main.NavigateForState();
        _activeScanOptions = options;

        _flowCancellation.Dispose();
        _flowCancellation = new CancellationTokenSource();
        _ = _main.ScanProgress.RunScanAsync(options, _flowCancellation.Token);
    }

    /// <summary>Progress page: cancel-with-confirmation then CancelScan transition.</summary>
    public async Task CancelScanAsync()
    {
        var confirmed = await _main.ScanProgress.ConfirmCancelAsync().ConfigureAwait(true);
        if (!confirmed)
        {
            return;
        }

        _main.ScanProgress.CancelScan();
        // The ScanFailed event fires with "cancelled"; the handler routes home.
    }

    /// <summary>Results: StartRecovery transition with the overlap gate context.</summary>
    public async Task StartRecoveryAsync()
    {
        if (_main.Results.Session is null)
        {
            await _dialogs.AlertAsync("Recover files", "Load or complete a scan first.").ConfigureAwait(true);
            return;
        }

        if (string.IsNullOrWhiteSpace(_main.Recovery.DestinationDirectory))
        {
            await PickDestinationAsync().ConfigureAwait(true);
        }

        // Preflight first: it computes the overlap gate the transition guard checks.
        await _main.Recovery.RunPreflightAsync(_main.Results.Session, _flowCancellation.Token).ConfigureAwait(true);
        if (_main.Recovery.PreflightRefusals.Count > 0)
        {
            // Hard gate (ADR-7): refuse with the reasons before any transition.
            await _dialogs.AlertAsync(
                "Destination not safe",
                string.Join(Environment.NewLine, _main.Recovery.PreflightRefusals)).ConfigureAwait(true);
            return;
        }

        var context = CurrentContext();
        var reason = _main.ExecuteTransition("StartRecovery", context);
        if (reason is not null)
        {
            await _dialogs.AlertAsync("Recover files", reason).ConfigureAwait(true);
            return;
        }

        _main.NavigateForState();
        _ = _main.Recovery.RecoverAsync(_flowCancellation.Token);
    }

    /// <summary>Destination picker + immediate preflight re-run (browse button path).</summary>
    public async Task PickDestinationAsync()
    {
        if (_main.Results.Session is null)
        {
            await _dialogs.AlertAsync("Recover files", "Load or complete a scan first.").ConfigureAwait(true);
            return;
        }

        var picked = await _dialogs.PickFolderAsync("Choose a recovery destination (must be on a different disk than the source)").ConfigureAwait(true);
        if (picked is null)
        {
            return;
        }

        _main.Recovery.DestinationDirectory = picked;
        await _main.Recovery.RunPreflightAsync(_main.Results.Session, _flowCancellation.Token).ConfigureAwait(true);
    }

    /// <summary>Recovering: cancel-with-confirmation.</summary>
    public async Task CancelRecoveryAsync()
    {
        var confirmed = await _dialogs.ConfirmAsync(
            "Cancel the recovery?",
            "Files already written remain in the destination folder. Cancel the remaining recovery?",
            "Cancel recovery",
            destructive: true).ConfigureAwait(true);
        if (!confirmed)
        {
            return;
        }
        _main.Recovery.CancelRecovery();
    }

    /// <summary>Completion: Finish → reset wizard for a new run.</summary>
    public async Task FinishAsync()
    {
        var reason = _main.ExecuteTransition("Finish", CurrentContext());
        if (reason is not null)
        {
            await _dialogs.AlertAsync("Finish", reason).ConfigureAwait(true);
            return;
        }
        _main.ResetWizard();
    }

    /// <summary>Completion: back to results to recover more files.</summary>
    public async Task BackToResultsAsync()
    {
        var reason = _main.ExecuteTransition("BackToResults", CurrentContext());
        if (reason is not null)
        {
            await _dialogs.AlertAsync("Back to results", reason).ConfigureAwait(true);
            return;
        }
        _main.NavigateForState();
    }

    private async void OnSessionLoaded(object? sender, LoadedSession session)
    {
        // ImportSession edge → Results directly (skips scan).
        var context = CurrentContext();
        var reason = _main.ExecuteTransition("ImportSession", context);
        if (reason is not null)
        {
            await _dialogs.AlertAsync("Open session", reason).ConfigureAwait(true);
            return;
        }
        _main.Results.InstallSession(session);
        _main.NavigateForState();
    }

    private async void OnScanStarted(object? sender, ScanOptions options)
    {
        // ScanConfig fires ScanStarted through StartScan(); orchestrator
        // handles transitions itself — this handler is a safety no-op when
        // invoked via StartScanAsync.
        if (ReferenceEquals(options, _activeScanOptions))
        {
            return;
        }
        await StartScanAsync().ConfigureAwait(true);
    }

    private async void OnScanCompleted(object? sender, ScanSummary summary)
    {
        var reason = _main.ExecuteTransition("ScanCompleted", CurrentContext());
        if (reason is not null)
        {
            await _dialogs.AlertAsync("Scan finished", reason).ConfigureAwait(true);
            return;
        }

        // Load the freshly written session into the results VM.
        var loaded = await _sessions.LoadAsync(summary.SessionPath ?? string.Empty).ConfigureAwait(true);
        if (loaded.Success && loaded.Session is { } session)
        {
            _main.Results.InstallSession(session, _main.ScanConfig.SelectedCategories);
        }
        else
        {
            _logger.LogWarning("Post-scan session load failed: {Diagnostic}", loaded.Diagnostic);
            await _dialogs.AlertAsync(
                "Scan finished",
                "The scan finished but reading its results failed: " + loaded.Diagnostic).ConfigureAwait(true);
            _main.ExecuteTransition("CancelScan", CurrentContext() with { CancelConfirmed = true });
        }
        _main.NavigateForState();
    }

    private async void OnScanFailed(object? sender, string diagnostic)
    {
        // Cancel route: state → Select; failures also return Home.
        var context = CurrentContext() with { CancelConfirmed = true };
        _main.ExecuteTransition("CancelScan", context);
        _main.NavigateForState();
        await _dialogs.AlertAsync("Scan stopped", diagnostic).ConfigureAwait(true);
    }

    private async void OnRecoveryCompleted(object? sender, RecoverySummary summary)
    {
        var reason = _main.ExecuteTransition("RecoveryFinished", CurrentContext());
        if (reason is not null)
        {
            await _dialogs.AlertAsync("Recovery finished", reason).ConfigureAwait(true);
            return;
        }

        var reportPath = Path.Combine(_main.Recovery.DestinationDirectory ?? ".", "recovery-report.txt");
        _main.Completion.Install(summary, _main.Recovery.DestinationDirectory ?? string.Empty, reportPath);
        _main.NavigateForState();
    }

    private async void OnRecoveryCancelled(object? sender, RecoverySummary summary)
    {
        var context = CurrentContext() with { CancelConfirmed = true };
        var reason = _main.ExecuteTransition("CancelRecovery", context);
        if (reason is not null)
        {
            await _dialogs.AlertAsync("Recovery cancelled", reason).ConfigureAwait(true);
            return;
        }
        _main.Completion.Install(summary, _main.Recovery.DestinationDirectory ?? string.Empty, null);
        _main.NavigateForState();
    }

    private WizardContext CurrentContext() => new()
    {
        HasScanTarget = _main.Home.SelectedTarget is not null,
        HasLoadedSession = _main.Results.Session is not null,
        HasRecoverySelection = _main.Results.SelectedNodes.Count > 0,
        DestinationOverlapCheckPassed = _main.Recovery.PreflightRefusals.Count == 0 &&
                                        _main.Recovery.DestinationDirectory is not null,
        CancelConfirmed = false,
    };

    private async Task<string> BuildSessionPathAsync(ScanTarget target)
    {
        var sessionsDirectory = _sessions.SessionsDirectory;
        var stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        var safeDescription = string.Join("_", target.Description.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries))
            .TrimEnd('.');
        var name = $"scan-disk{target.DiskIndex}-{safeDescription}-{stamp}.crsf";
        var path = Path.Combine(sessionsDirectory, name);
        _logger.LogInformation("Session path allocated: {Path}", path);
        await Task.CompletedTask.ConfigureAwait(true);
        return path;
    }

    private static void OpenInExplorer(string path)
    {
        if (!Directory.Exists(path))
        {
            return;
        }
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true,
        });
    }

    private void ShowLicenses()
    {
        var licenses = LicenseText.Build();
        System.Windows.MessageBox.Show(licenses, "Open-source licenses", MessageBoxButton.OK, MessageBoxImage.Information);
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
