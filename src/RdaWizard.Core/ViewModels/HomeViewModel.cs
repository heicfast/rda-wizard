using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using RdaWizard.Core.Models;
using RdaWizard.Core.Navigation;
using RdaWizard.Core.Services;

namespace RdaWizard.Core.ViewModels;

/// <summary>
/// Home / device selection (og: PeripheralHomeView — UI-MAP §3 P02).
/// States: Loading / Ready / Error / Empty, plus driver status and the
/// session-import entry. Selecting a disk/volume enables BeginScanSetup;
/// loading a session jumps straight to Results (ImportSession edge).
/// </summary>
public partial class HomeViewModel : ViewModelBase
{
    private readonly ILogger<HomeViewModel> _logger;
    private readonly IDeviceEnumerationService _devices;
    private readonly IDriverStatusService _driverStatus;
    private readonly ISessionService _sessions;
    private readonly IDialogService _dialogs;
    private readonly INavigationService _navigation;

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _hasDevices;
    [ObservableProperty] private string? _errorDiagnostic;
    [ObservableProperty] private IReadOnlyList<DiskInfo> _disks = Array.Empty<DiskInfo>();
    [ObservableProperty] private DriverStatus? _driver;
    [ObservableProperty] private ScanTarget? _selectedTarget;
    [ObservableProperty] private IReadOnlyList<SessionInfo> _recentSessions = Array.Empty<SessionInfo>();
    [ObservableProperty] private SessionInfo? _selectedRecentSession;

    public HomeViewModel(
        ILogger<HomeViewModel> logger,
        IDeviceEnumerationService devices,
        IDriverStatusService driverStatus,
        ISessionService sessions,
        IDialogService dialogs,
        INavigationService navigation)
    {
        _logger = logger;
        _devices = devices;
        _driverStatus = driverStatus;
        _sessions = sessions;
        _dialogs = dialogs;
        _navigation = navigation;
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

            Driver = await _driverStatus.ProbeAsync(cancellationToken).ConfigureAwait(true);
            RecentSessions = await _sessions.ListRecentAsync(cancellationToken).ConfigureAwait(true);
            SelectedRecentSession = null;

            _logger.LogInformation("Home refreshed: {Disks} disks, {Volumes} volumes, driver {Driver}",
                enumeration.Disks.Count, enumeration.Volumes.Count, Driver?.State.ToString() ?? "unknown");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Home refresh failed");
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

    /// <summary>Loads a recent or picked session — jumps straight to Results when it parses.</summary>
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
                // Corrupt/failed session: full, clear failure — never a partial tree (I. Edge states).
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

    /// <summary>Raised when a session was loaded and the shell should fire the ImportSession transition.</summary>
    public event EventHandler<LoadedSession>? SessionLoaded;
}
