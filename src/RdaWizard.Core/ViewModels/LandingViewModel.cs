using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using RdaWizard.Core.Models;
using RdaWizard.Core.Services;

namespace RdaWizard.Core.ViewModels;

/// <summary>
/// Landing page (og: PeripheralHomeView, wizard stack index 2 — the default
/// start page). Device tiles with capacity bars, the "Specify a location"
/// trio, the "Search for Lost Data" CTA (starts the scan immediately — the
/// og has no scan-configuration step), and the "Continue last scanning"
/// session banner (P41). Selecting a tile enables the CTA; loading a session
/// jumps straight to the Detail results page (ImportSession edge).
/// </summary>
public partial class LandingViewModel : ViewModelBase
{
    private readonly ILogger<LandingViewModel> _logger;
    private readonly IDeviceEnumerationService _devices;
    private readonly ISessionService _sessions;
    private readonly IDialogService _dialogs;

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _hasDevices;
    [ObservableProperty] private string? _errorDiagnostic;
    [ObservableProperty] private IReadOnlyList<DiskInfo> _disks = Array.Empty<DiskInfo>();
    [ObservableProperty] private ScanTarget? _selectedTarget;
    [ObservableProperty] private IReadOnlyList<SessionInfo> _recentSessions = Array.Empty<SessionInfo>();

    /// <summary>True when a scan is running (sidebar returns to the landing
    /// mid-scan): the CTA becomes a "return to scan" affordance.</summary>
    [ObservableProperty] private bool _isScanRunning;

    public LandingViewModel(
        ILogger<LandingViewModel> logger,
        IDeviceEnumerationService devices,
        ISessionService sessions,
        IDialogService dialogs)
    {
        _logger = logger;
        _devices = devices;
        _sessions = sessions;
        _dialogs = dialogs;
    }

    /// <summary>Shell calls this on first show and on explicit refresh.</summary>
    [RelayCommand]
    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        if (IsLoading)
        {
            return;
        }

        IsLoading = true;
        ErrorDiagnostic = null;
        try
        {
            var enumeration = await _devices.EnumerateAsync(cancellationToken).ConfigureAwait(true);
            Disks = enumeration.Disks;
            HasDevices = enumeration.Disks.Count > 0;
            ErrorDiagnostic = enumeration.Success ? null : (enumeration.Diagnostic ?? "The engine could not list disks.");
            SelectedTarget = null;

            RecentSessions = await _sessions.ListRecentAsync(cancellationToken).ConfigureAwait(true);

            _logger.LogInformation("Landing refreshed: {Disks} disks, {Volumes} volumes, {Sessions} recent sessions",
                enumeration.Disks.Count, enumeration.Volumes.Count, RecentSessions.Count);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Landing refresh failed");
            ErrorDiagnostic = "Refreshing the device list failed: " + ex.Message;
            HasDevices = false;
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>Selects a whole physical disk as the scan target.</summary>
    [RelayCommand]
    public void SelectDisk(DiskInfo disk)
    {
        SelectedTarget = new ScanTarget
        {
            Kind = ScanTargetKind.WholeDisk,
            DiskIndex = disk.Index,
            Description = disk.DisplayName,
            SizeBytes = disk.SizeBytes,
        };
        _logger.LogInformation("Target: whole disk {Index} ({Description})", disk.Index, disk.DisplayName);
    }

    /// <summary>Selects a volume (engine scans its extent over the owning disk — G-21).</summary>
    [RelayCommand]
    public void SelectVolume(VolumeInfo volume)
    {
        SelectedTarget = new ScanTarget
        {
            Kind = ScanTargetKind.Volume,
            DiskIndex = volume.OwningDiskIndex,
            VolumeLetter = volume.Letter,
            Description = volume.DisplayName,
            SizeBytes = volume.SizeBytes,
        };
        _logger.LogInformation("Target: volume {Letter} on disk {Disk}", volume.Letter, volume.OwningDiskIndex);
    }

    /// <summary>The "Search for Lost Data" CTA — the og starts the scan
    /// immediately (scanClickedSlot). Raises ScanRequested; the shell fires
    /// the StartScan transition and hands the target to the Detail page.</summary>
    [RelayCommand]
    public void StartScan()
    {
        if (SelectedTarget is null)
        {
            return;
        }
        ScanRequested?.Invoke(this, SelectedTarget);
    }

    /// <summary>Loads a recent session (P41 banner "Load") — jumps straight
    /// to the Detail results page when it parses.</summary>
    [RelayCommand]
    public async Task LoadSessionAsync(SessionInfo? session, CancellationToken cancellationToken)
    {
        if (session is null)
        {
            return;
        }

        IsLoading = true;
        ErrorDiagnostic = null;
        try
        {
            var loaded = await _sessions.LoadAsync(session.Path, cancellationToken).ConfigureAwait(true);
            if (loaded.Success && loaded.Session is { } result)
            {
                _logger.LogInformation("Session loaded: {Path} ({Files} files)", session.Path, result.FileCount);
                SessionLoaded?.Invoke(this, result);
            }
            else
            {
                // Corrupt/failed session: full, clear failure — never a partial tree.
                ErrorDiagnostic = "This scan session could not be opened: " + (loaded.Diagnostic ?? "the engine rejected the file.");
                _logger.LogWarning("Session load failed: {Path}: {Diagnostic}", session.Path, loaded.Diagnostic);
                await _dialogs.AlertAsync("Session", ErrorDiagnostic).ConfigureAwait(true);
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>Pick a session file from disk via the shell dialog.</summary>
    [RelayCommand]
    public async Task ImportSessionFileAsync(CancellationToken cancellationToken)
    {
        var path = await _dialogs.PickOpenFileAsync("Open scan session", "Scan sessions (*.crsf)|*.crsf|All files (*.*)|*.*").ConfigureAwait(true);
        if (path is null)
        {
            return;
        }

        await LoadSessionAsync(new SessionInfo
        {
            Path = path,
            SavedAtUtc = File.GetLastWriteTimeUtc(path),
            SizeBytes = new FileInfo(path).Length,
        }, cancellationToken).ConfigureAwait(true);
    }

    /// <summary>Raised when the user presses the scan CTA with a target selected.</summary>
    public event EventHandler<ScanTarget>? ScanRequested;

    /// <summary>Raised when a session was loaded and the shell should fire the ImportSession transition.</summary>
    public event EventHandler<LoadedSession>? SessionLoaded;
}
