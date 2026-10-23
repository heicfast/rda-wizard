using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using RdaWizard.Core.Models;
using RdaWizard.Core.Services;

namespace RdaWizard.Core.ViewModels;

/// <summary>One bare "Specify a location" trio entry (Recycle Bin /
/// Desktop / Select Folder — og: bare icon + label, wrapped).</summary>
public partial class TrioItemViewModel : ObservableObject
{
    public required string Key { get; init; }

    public required string Label { get; init; }

    /// <summary>Design-system icon resource key (Icon.Device.*).</summary>
    public required string IconName { get; init; }

    [ObservableProperty] private bool _isSelected;
}

/// <summary>
/// One landing device tile (og: PeripheralHomeView volume card). The og tiles
/// render "Windows(C:)" + used/total capacity bar; selection expands the tile
/// (blue border + checkmark + capacity row) — compact when unselected.
/// </summary>
public partial class DriveTileViewModel : ObservableObject
{
    public required VolumeInfo Volume { get; init; }

    /// <summary>Owning disk is removable (USB) — routes the tile to the
    /// "External drives" group (og grouping by bus, not health).</summary>
    public required bool IsExternal { get; init; }

    [ObservableProperty] private bool _isSelected;

    public string Name => Volume.TileName;

    public string Filesystem => Volume.Filesystem;

    public long SizeBytes => Volume.SizeBytes;

    public long? UsedBytes => Volume.UsedBytes;

    public bool IsBitLocker => Volume.IsBitLocker;

    /// <summary>Fraction 0..1 for the capacity bar; null hides the bar
    /// (unmounted/BitLocker volumes report no used figure).</summary>
    public double? UsedFraction => Volume.UsedBytes is { } used && Volume.SizeBytes > 0
        ? Math.Clamp((double)used / Volume.SizeBytes, 0, 1)
        : null;

    /// <summary>og tile caption, e.g. "31.15 GB/149.45 GB" (used/total,
    /// no spaces around the slash); totals only when the OS pass reports
    /// no used figure.</summary>
    public string CapacityText => Volume.UsedBytes is { } used
        ? $"{FormatGib(used)}/{FormatGib(Volume.SizeBytes)}"
        : FormatGib(Volume.SizeBytes);

    private static string FormatGib(long bytes) => bytes switch
    {
        < 0 => "—",
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:0.#} KB",
        < 1024L * 1024 * 1024 => $"{bytes / (1024.0 * 1024):0.#} MB",
        < 1024L * 1024 * 1024 * 1024 => $"{bytes / (1024.0 * 1024 * 1024):0.##} GB",
        _ => $"{bytes / (1024.0 * 1024 * 1024 * 1024):0.##} TB",
    };
}

/// <summary>
/// Landing page (og: PeripheralHomeView, wizard stack index 2 — the default
/// start page). Volume tiles grouped Internal/External with live capacity
/// bars, the "Specify a location" trio (Recycle Bin / Desktop / Select
/// Folder — each a folder target over its containing volume, the og's
/// PushFolderFilter equivalent), and the "Search for Lost Data" CTA that
/// starts the scan immediately (the og has no scan-configuration step).
/// The "Continue last scanning" session banner (P41) loads the newest
/// session straight into the Detail results page.
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
    [ObservableProperty] private ScanTarget? _selectedTarget;
    [ObservableProperty] private IReadOnlyList<SessionInfo> _recentSessions = Array.Empty<SessionInfo>();

    /// <summary>Currently selected tile/trio entry (single selection across
    /// all groups — the og allows exactly one; re-clicking selects, clicking
    /// another moves the selection).</summary>
    [ObservableProperty] private string? _selectedTileKey;

    /// <summary>Trio selection key ("recycle" / "desktop" / "folder:&#60;path&#62;").</summary>
    [ObservableProperty] private string? _selectedTrioKey;

    /// <summary>True when a scan is running (sidebar returns to the landing
    /// mid-scan): the CTA becomes a "return to scan" affordance — the og
    /// runs one scan app-level; restarting would destroy it.</summary>
    [ObservableProperty] private bool _isScanRunning;

    public ObservableCollection<DriveTileViewModel> InternalDrives { get; } = [];

    public ObservableCollection<DriveTileViewModel> ExternalDrives { get; } = [];

    /// <summary>The og trio (Recycle Bin / Desktop / Select Folder).</summary>
    public ObservableCollection<TrioItemViewModel> LocationTrio { get; } =
    [
        new() { Key = "recycle", Label = "Recycle Bin", IconName = "Icon.Device.RecycleBin" },
        new() { Key = "desktop", Label = "Desktop", IconName = "Icon.Device.Desktop" },
        new() { Key = "folder", Label = "Select Folder", IconName = "Icon.Device.Folder" },
    ];

    [ObservableProperty] private string _internalHeader = "Internal drives (0)";

    [ObservableProperty] private string _externalHeader = "External drives (0)";

    /// <summary>CTA enabled iff a target is selected (og: tile selection
    /// gates the CTA).</summary>
    [ObservableProperty] private bool _hasSelection;

    /// <summary>og CTA copy; becomes the return-to-scan affordance while a
    /// scan runs.</summary>
    [ObservableProperty] private string _ctaLabel = "Search for Lost Data";

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

    partial void OnIsScanRunningChanged(bool value) => UpdateCta();

    partial void OnSelectedTargetChanged(ScanTarget? value)
    {
        HasSelection = value is not null;
        UpdateCta();
    }

    private void UpdateCta() => CtaLabel = IsScanRunning ? "Return to scan" : "Search for Lost Data";

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
            RebuildTiles(enumeration.Disks);
            HasDevices = InternalDrives.Count > 0 || ExternalDrives.Count > 0;
            ErrorDiagnostic = enumeration.Success ? null : (enumeration.Diagnostic ?? "The engine could not list disks.");

            // Selection survives refresh only when the target still exists (og
            // refresh rebuilds lists; a vanished selection drops cleanly).
            if (SelectedTarget is { } target && !TileExists(target))
            {
                SelectedTarget = null;
                SelectedTileKey = null;
                SelectedTrioKey = null;
            }

            RecentSessions = await _sessions.ListRecentAsync(cancellationToken).ConfigureAwait(true);

            _logger.LogInformation("Landing refreshed: {Internal} internal, {External} external tiles, {Sessions} recent sessions",
                InternalDrives.Count, ExternalDrives.Count, RecentSessions.Count);
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

    private void RebuildTiles(IReadOnlyList<DiskInfo> disks)
    {
        var previousKey = SelectedTileKey;
        InternalDrives.Clear();
        ExternalDrives.Clear();

        foreach (var disk in disks)
        {
            foreach (var volume in disk.Volumes)
            {
                var tile = new DriveTileViewModel
                {
                    Volume = volume,
                    IsExternal = disk.IsRemovable,
                };
                if (disk.IsRemovable)
                {
                    ExternalDrives.Add(tile);
                }
                else
                {
                    InternalDrives.Add(tile);
                }
            }
        }

        InternalHeader = $"Internal drives ({InternalDrives.Count})";
        ExternalHeader = $"External drives ({ExternalDrives.Count})";

        // Restore selection on the rebuilt tile with the same key.
        if (previousKey is { } key)
        {
            var match = InternalDrives.Concat(ExternalDrives).FirstOrDefault(t => KeyFor(t) == key);
            if (match is not null)
            {
                match.IsSelected = true;
            }
        }
    }

    private bool TileExists(ScanTarget target) => InternalDrives.Concat(ExternalDrives)
        .Any(t => t.Volume.OwningDiskIndex == target.DiskIndex && t.Volume.Letter == target.VolumeLetter);

    private static string KeyFor(DriveTileViewModel tile) => tile.Volume.Letter is { } letter
        ? $"vol:{letter}"
        : $"vol:{tile.Volume.OwningDiskIndex}:{tile.Volume.Filesystem}";

    /// <summary>Selects a volume tile (single selection — og behavior).</summary>
    [RelayCommand]
    public void SelectTile(DriveTileViewModel? tile)
    {
        if (tile is null)
        {
            return;
        }

        foreach (var other in InternalDrives.Concat(ExternalDrives))
        {
            other.IsSelected = ReferenceEquals(other, tile);
        }

        SelectedTrioKey = null;
        foreach (var trioItem in LocationTrio)
        {
            trioItem.IsSelected = false;
        }
        SelectedTileKey = KeyFor(tile);
        SelectedTarget = new ScanTarget
        {
            Kind = ScanTargetKind.Volume,
            DiskIndex = tile.Volume.OwningDiskIndex,
            VolumeLetter = tile.Volume.Letter,
            Description = tile.Volume.TileName,
            SizeBytes = tile.Volume.SizeBytes,
        };
        _logger.LogInformation("Target: volume {Name} on disk {Disk}", tile.Volume.TileName, tile.Volume.OwningDiskIndex);
    }

    /// <summary>Trio entry click: Recycle Bin / Desktop select immediately;
    /// Select Folder opens the native picker first.</summary>
    [RelayCommand]
    public async Task SelectTrioItemAsync(TrioItemViewModel? item, CancellationToken cancellationToken)
    {
        if (item is null)
        {
            return;
        }

        switch (item.Key)
        {
            case "recycle":
                SelectRecycleBin();
                break;
            case "desktop":
                SelectDesktop();
                break;
            case "folder":
                await SelectFolderAsync(cancellationToken).ConfigureAwait(true);
                break;
        }
    }

    /// <summary>Trio: Recycle Bin — folder target over the system volume's
    /// $RECYCLE.BIN (the og's type-33 recycle-bin tile).</summary>
    [RelayCommand]
    public void SelectRecycleBin()
    {
        var system = SystemVolume();
        if (system is null)
        {
            return;
        }

        SelectTrio("recycle", new ScanTarget
        {
            Kind = ScanTargetKind.Folder,
            DiskIndex = system.OwningDiskIndex,
            VolumeLetter = system.Letter,
            FolderPath = "$RECYCLE.BIN",
            PathPrefix = "$RECYCLE.BIN",
            Description = "Recycle Bin",
            SizeBytes = system.SizeBytes,
        });
    }

    /// <summary>Trio: Desktop — folder target over the user's desktop folder
    /// on its containing volume.</summary>
    [RelayCommand]
    public void SelectDesktop()
    {
        var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        if (TryFolderTarget(desktopPath, "Desktop", out var target))
        {
            SelectTrio("desktop", target);
        }
    }

    /// <summary>Trio: Select Folder — native folder picker, then folder
    /// target over the containing volume (og: Choose Location flow).</summary>
    [RelayCommand]
    public async Task SelectFolderAsync(CancellationToken cancellationToken)
    {
        var path = await _dialogs.PickFolderAsync("Select a folder to search").ConfigureAwait(true);
        if (path is null)
        {
            return;
        }

        if (TryFolderTarget(path, null, out var target))
        {
            SelectTrio($"folder:{path}", target);
        }
    }

    private void SelectTrio(string key, ScanTarget target)
    {
        ClearTileSelection();
        SelectedTileKey = null;
        SelectedTrioKey = key;
        foreach (var trioItem in LocationTrio)
        {
            trioItem.IsSelected = trioItem.Key == key
                || (trioItem.Key == "folder" && key.StartsWith("folder:", StringComparison.Ordinal));
        }
        SelectedTarget = target;
    }

    private void ClearTileSelection()
    {
        foreach (var other in InternalDrives.Concat(ExternalDrives))
        {
            other.IsSelected = false;
        }
    }

    private VolumeInfo? SystemVolume()
    {
        var systemRoot = Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.System));
        var letter = systemRoot is { Length: >= 2 } && systemRoot[1] == ':' ? systemRoot[0] : (char?)null;
        var enumerated = InternalDrives.Concat(ExternalDrives)
            .Select(t => t.Volume)
            .ToList();
        return enumerated.FirstOrDefault(v => v.Letter == letter && letter is not null)
            // Non-Windows hosts have no system drive letter — the first
            // internal volume is the stand-in (never reached on Windows,
            // where the system volume always enumerates).
            ?? enumerated.FirstOrDefault(v => v.Letter is not null && InternalDrives.Any(t => t.Volume == v));
    }

    /// <summary>Resolves a folder path to a folder target over its
    /// containing volume: the engine scans the volume; results scope to the
    /// folder path (og PushFolderFilter semantics).</summary>
    private bool TryFolderTarget(string path, string? displayName, out ScanTarget target)
    {
        target = null!;
        var root = Path.GetPathRoot(path);
        if (string.IsNullOrEmpty(root) || root.Length < 2 || root[1] != ':')
        {
            _logger.LogWarning("Folder target {Path} is not on a mounted volume — not selectable", path);
            return false;
        }

        var letter = root[0];
        var volume = InternalDrives.Concat(ExternalDrives).Select(t => t.Volume).FirstOrDefault(v => v.Letter == letter);
        if (volume is null)
        {
            _logger.LogWarning("Folder target {Path}: volume {Letter} not in the current enumeration", path, letter);
            return false;
        }

        // Tree scope: the session tree roots at the volume; sub-path uses
        // forward slashes (engine tree grammar).
        var relative = Path.GetRelativePath(root, path).Replace('\\', '/');
        var prefix = string.IsNullOrEmpty(relative) ? null : relative.TrimEnd('/') + "/";

        target = new ScanTarget
        {
            Kind = ScanTargetKind.Folder,
            DiskIndex = volume.OwningDiskIndex,
            VolumeLetter = letter,
            FolderPath = path,
            PathPrefix = prefix,
            Description = displayName ?? (prefix is null ? $"{letter}: volume" : Path.GetFileName(path)),
            SizeBytes = volume.SizeBytes,
        };
        return true;
    }

    /// <summary>The "Search for Lost Data" CTA — the og starts the scan
    /// immediately (scanClickedSlot). While a scan already runs (sidebar
    /// return), the CTA returns to the running scan instead of restarting
    /// it (one scan app-level in the og). Raises ScanRequested or
    /// ReturnToScanRequested; the shell fires the transition.</summary>
    [RelayCommand]
    public void StartScan()
    {
        if (IsScanRunning)
        {
            ReturnToScanRequested?.Invoke(this, EventArgs.Empty);
            return;
        }

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

    /// <summary>Raised when the CTA is pressed while a scan is already
    /// running — the shell navigates back to the Detail scan surface.</summary>
    public event EventHandler? ReturnToScanRequested;

    /// <summary>Raised when a session was loaded and the shell should fire the ImportSession transition.</summary>
    public event EventHandler<LoadedSession>? SessionLoaded;
}
