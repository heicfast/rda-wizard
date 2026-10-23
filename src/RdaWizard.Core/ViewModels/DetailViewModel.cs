using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using RdaWizard.Core.Filters;
using RdaWizard.Core.Models;
using RdaWizard.Core.Services;

namespace RdaWizard.Core.ViewModels;

/// <summary>
/// The Detail page (og: CDetailPage, wizard stack index 3): scan progress AND
/// results on the same surface — "browse-while-scanning". The og bottom bar
/// (CBottomArea) shows the circular % gauge, Pause/Stop, "Files Found" status,
/// remaining time and the "Searching:" ticker while a scan runs; the toolbar
/// (CNavigateBar) + 3-pane results browser take over once results exist.
///
/// TRUTHFUL telemetry: the engine streams an overall weighted percentage
/// (monotonic; rrecov.cpp:81) via stdout; files-found appears when the engine
/// reports it (summary at phase end); read-error ranges stream live from the
/// stage log. Two-phase scan (quick → advanced) is wired by the shell.
/// </summary>
public partial class DetailViewModel : ViewModelBase
{
    private readonly ILogger<DetailViewModel> _logger;
    private readonly IScanService _scans;
    private readonly ISessionService _sessions;
    private readonly IDialogService _dialogs;
    private readonly IClock _clock;

    private CancellationTokenSource? _scanCancellation;

    // ---- OG CBottomArea: scan-phase state ----

    /// <summary>Scan phase shown in the bottom bar (og status formats).</summary>
    public enum ScanPhase
    {
        Idle,
        QuickScanning,
        AdvancedScanning,
        Paused,
        Complete,
    }

    [ObservableProperty] private ScanPhase _phase = ScanPhase.Idle;
    [ObservableProperty] private double _percent;
    [ObservableProperty] private string _elapsedText = "0:00";
    [ObservableProperty] private bool _isScanRunning;
    [ObservableProperty] private bool _isPaused;
    [ObservableProperty] private string? _failureDiagnostic;
    [ObservableProperty] private ScanSummary? _summary;
    [ObservableProperty] private string _targetName = string.Empty;
    [ObservableProperty] private long _readErrorCount;
    [ObservableProperty] private long _readErrorBytes;
    [ObservableProperty] private IReadOnlyList<ReadErrorRange> _readErrors = Array.Empty<ReadErrorRange>();

    /// <summary>og bottom-bar status line: "Advanced Scanning, Files Found:
    /// N (size)" while running (stage-boundary checkpoint truth), "Scan
    /// Completed, Files Found: N (size)" at the end — verbatim formats from
    /// CBottomArea (R-A PG-10).</summary>
    [ObservableProperty] private string _statusPrimary = string.Empty;

    /// <summary>og secondary line — "Remaining time: 07m43s" (ETA derived
    /// from permille + elapsed; no fabricated sector telemetry: the engine
    /// CLI streams only overall percent + seconds).</summary>
    [ObservableProperty] private string? _statusSecondary;

    /// <summary>True when partial results are viewable mid-scan (ADR-2:
    /// stage-boundary refresh from the engine's auto-checkpoint .crsf).</summary>
    [ObservableProperty] private bool _hasPartialResults;

    /// <summary>Sum of found file sizes (og status ": (size)" component).
    /// Computed at each session install — checkpoint or final.</summary>
    [ObservableProperty] private long _foundBytes;

    // ---- Results browser state ----

    [ObservableProperty] private bool _isLoadingResults;
    [ObservableProperty] private LoadedSession? _session;
    [ObservableProperty] private string? _resultsError;
    [ObservableProperty] private ResultFilter _filter = ResultFilter.None;
    [ObservableProperty] private IReadOnlyList<FileRowViewModel> _visibleFiles = Array.Empty<FileRowViewModel>();
    [ObservableProperty] private long _visibleFileCount;
    [ObservableProperty] private long _totalFileCount;
    [ObservableProperty] private long _selectedCount;
    [ObservableProperty] private long _selectedBytes;
    [ObservableProperty] private ResultNode? _previewNode;
    [ObservableProperty] private ResultsViewMode _viewMode = ResultsViewMode.Detail;
    [ObservableProperty] private ResultsTreeMode _treeMode = ResultsTreeMode.Path;
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private string _breadcrumb = string.Empty;
    [ObservableProperty] private IReadOnlyList<TreeNodeItem> _treeRoots = Array.Empty<TreeNodeItem>();

    /// <summary>Selected nodes (recovery input). Maintained by selection commands.</summary>
    public IReadOnlySet<ResultNode> SelectedNodes => _selected;

    private readonly HashSet<ResultNode> _selected = new();

    /// <summary>Tree node wrapper for hierarchical display (path tree).</summary>
    public sealed record TreeNodeItem(ResultNode Node)
    {
        public string Name => Node.Name;
        public string FullPath => Node.FullPath;
        public List<TreeNodeItem> Children { get; } = new();
    }

    public DetailViewModel(ILogger<DetailViewModel> logger, IScanService scans, ISessionService sessions, IDialogService dialogs, IClock clock)
    {
        _logger = logger;
        _scans = scans;
        _sessions = sessions;
        _dialogs = dialogs;
        _clock = clock;
    }

    /// <summary>True while any scan is executing — Pause/Stop availability.</summary>
    public bool IsScanActive => IsScanRunning;

    /// <summary>Gauge label: "21%" (og ring text).</summary>
    public string PercentLabel => $"{Percent:0}%";

    partial void OnPercentChanged(double value) => OnPropertyChanged(nameof(PercentLabel));

    /// <summary>Raised when a scan completed successfully with a saved session.</summary>
    public event EventHandler<ScanSummary>? ScanCompleted;

    /// <summary>Raised when the scan failed or was cancelled (shell returns to the landing).</summary>
    public event EventHandler<string>? ScanFailed;

    /// <summary>Raised when the user requests recovery (Recover All / Recover selection).</summary>
    public event EventHandler<RecoveryRequestKind>? RecoverRequested;

    /// <summary>Runs the scan; called by the shell after the StartScan transition.</summary>
    public async Task RunScanAsync(ScanOptions options, CancellationToken externalToken)
    {
        if (IsScanRunning)
        {
            _logger.LogWarning("Scan already running; ignoring duplicate start");
            return;
        }

        TargetName = options.Target.Description;
        Breadcrumb = options.Target.Description;
        IsScanRunning = true;
        IsPaused = false;
        Phase = options.Mode == ScanMode.Quick ? ScanPhase.QuickScanning : ScanPhase.AdvancedScanning;
        Percent = 0;
        Summary = null;
        StatusPrimary = PhaseLabel();
        StatusSecondary = null;
        HasPartialResults = false;
        FailureDiagnostic = null;
        ReadErrors = Array.Empty<ReadErrorRange>();
        ReadErrorCount = 0;
        ReadErrorBytes = 0;
        _scanCancellation = CancellationTokenSource.CreateLinkedTokenSource(externalToken);

        // ADR-2 browse-while-scanning: the engine auto-checkpoints the .crsf
        // at stage boundaries (G-13); each size change on the session file is
        // a completed stage — reload the tree so the og's live-populating
        // results view is fed by checkpoint truth, never fabricated mid-scan
        // reads (G-02/G-03 forbid live-tree reads).
        var boundaryWatcher = WatchSessionBoundariesAsync(options.SessionPath, _scanCancellation.Token);

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
                    StatusSecondary = "Remaining time: " + FormatEta(TimeSpan.FromSeconds(etaSeconds));
                }
            }
            else
            {
                StatusSecondary = null;
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
            Phase = ScanPhase.Complete;
            UpdateStatusPrimary();
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
            _scanCancellation.Cancel(); // ends the boundary watcher
            IsScanRunning = false;
            _scanCancellation.Dispose();
            _scanCancellation = null;
            await boundaryWatcher.ConfigureAwait(true);
        }
    }

    /// <summary>Stage-boundary watcher: polls the session file size; every
    /// change is a completed-stage checkpoint — reload the partial tree.
    /// Mid-write parse races fail soft (skipped; the next boundary or the
    /// completion path self-heals).</summary>
    private async Task WatchSessionBoundariesAsync(string sessionPath, CancellationToken token)
    {
        var lastLength = -1L;
        try
        {
            while (!token.IsCancellationRequested)
            {
                await Task.Delay(2000, token).ConfigureAwait(true);

                long length;
                try
                {
                    if (!File.Exists(sessionPath))
                    {
                        continue;
                    }
                    length = new FileInfo(sessionPath).Length;
                }
                catch (IOException)
                {
                    continue;
                }

                if (length == lastLength)
                {
                    continue;
                }
                lastLength = length;

                var loaded = await _sessions.LoadAsync(sessionPath, token).ConfigureAwait(true);
                if (loaded.Success && loaded.Session is { } partial && partial.FileCount > 0)
                {
                    PostOnUi(() => InstallSession(partial, preserveState: true));
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Scan ended — the completion path installs the final session.
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Boundary refresh skipped (transient)");
        }
    }

    private string PhaseLabel() => Phase switch
    {
        ScanPhase.QuickScanning => "Quick Scanning",
        ScanPhase.AdvancedScanning => "Advanced Scanning",
        ScanPhase.Paused => "Scan Paused",
        ScanPhase.Complete => "Scan Completed",
        _ => string.Empty,
    };

    partial void OnPhaseChanged(ScanPhase value) => UpdateStatusPrimary();

    partial void OnTotalFileCountChanged(long value) => UpdateStatusPrimary();

    partial void OnFoundBytesChanged(long value) => UpdateStatusPrimary();

    private void UpdateStatusPrimary()
    {
        if (!IsScanRunning && Phase != ScanPhase.Complete)
        {
            StatusPrimary = string.Empty;
            return;
        }

        if (Phase == ScanPhase.Complete && Summary is { } summary)
        {
            StatusPrimary = TotalFileCount > 0
                ? $"Scan Completed, Files Found: {TotalFileCount:N0} ({FormatBytes(FoundBytes)})"
                : $"Scan Completed, Files Found: {summary.Files:N0}";
            return;
        }

        // Running (or completed-before-session-load): found-count from the
        // newest checkpoint when available — the og streams this live from
        // in-process engine callbacks; ours is checkpoint-boundary truth.
        StatusPrimary = TotalFileCount > 0
            ? $"{PhaseLabel()}, Files Found: {TotalFileCount:N0} ({FormatBytes(FoundBytes)})"
            : PhaseLabel();
    }

    /// <summary>Requests cancellation of the running scan (the shell confirms first — og stop-confirm dialog).</summary>
    public void CancelScan() => _scanCancellation?.Cancel();

    /// <summary>Installs a loaded session (fresh scan, import, or
    /// stage-boundary checkpoint refresh) and computes initial views.</summary>
    public void InstallSession(LoadedSession session, ResultFilter? prefilter = null, bool preserveState = false)
    {
        var filter = preserveState ? Filter : (prefilter ?? ResultFilter.None);
        Session = session;
        FoundBytes = session.Nodes.Where(n => !n.IsDirectory).Sum(n => n.LogicalSize);
        TotalFileCount = session.FileCount;
        TreeRoots = BuildTreeRoots(session);
        Filter = filter;
        if (!preserveState)
        {
            _selected.Clear();
        }
        UpdateSelectionCounts();
        ApplyFilter();
        ResultsError = null;
        HasPartialResults = IsScanRunning;
        _logger.LogInformation("Results installed: {Total} files, {Carved} carved, {Lost} lost-partition nodes",
            session.FileCount, session.CarvedNodes.Count, session.LostNodes.Count);
    }

    private static List<TreeNodeItem> BuildTreeRoots(LoadedSession session)
    {
        var byPath = new Dictionary<string, TreeNodeItem>(StringComparer.OrdinalIgnoreCase);
        var roots = new List<TreeNodeItem>();

        foreach (var node in session.Nodes)
        {
            var item = new TreeNodeItem(node);
            byPath[node.FullPath] = item;

            if (node.ParentPath is { } parentPath && byPath.TryGetValue(parentPath, out var parent))
            {
                parent.Children.Add(item);
            }
            else
            {
                roots.Add(item);
            }
        }

        return roots;
    }

    partial void OnSearchTextChanged(string value)
    {
        Filter = Filter with { NameContains = string.IsNullOrWhiteSpace(value) ? null : value.Trim() };
        ApplyFilter();
    }

    /// <summary>Applies the current filter to the snapshot (O(n), allocation-free predicates).</summary>
    [RelayCommand]
    public void ApplyFilter()
    {
        if (Session is null)
        {
            VisibleFiles = Array.Empty<FileRowViewModel>();
            VisibleFileCount = 0;
            return;
        }

        var files = Session.Nodes.Where(n => !n.IsDirectory).ToArray();
        var filtered = FilterEvaluator.Apply(files, Filter);
        VisibleFiles = filtered
            .Select(n => new FileRowViewModel { Node = n, IsSelected = _selected.Contains(n) })
            .ToArray();
        VisibleFileCount = filtered.Count;
        _logger.LogDebug("Filter applied: {Visible}/{Total} files", filtered.Count, files.Length);
    }

    [RelayCommand]
    public void ClearFilter()
    {
        Filter = ResultFilter.None;
        SearchText = string.Empty;
        ApplyFilter();
    }

    [RelayCommand]
    public void ToggleTreeMode()
    {
        TreeMode = TreeMode == ResultsTreeMode.Path ? ResultsTreeMode.Type : ResultsTreeMode.Path;
    }

    [RelayCommand]
    public void ToggleViewMode()
    {
        ViewMode = ViewMode == ResultsViewMode.Detail ? ResultsViewMode.LargeIcons : ResultsViewMode.Detail;
    }

    [RelayCommand]
    public void SelectNode(ResultNode node)
    {
        if (_selected.Add(node)) UpdateSelectionCounts();
    }

    /// <summary>Row checkbox toggle: flips the row visual + the selection set.</summary>
    [RelayCommand]
    public void ToggleRow(FileRowViewModel row)
    {
        row.IsSelected = !row.IsSelected;
        if (row.IsSelected)
        {
            _selected.Add(row.Node);
        }
        else
        {
            _selected.Remove(row.Node);
        }
        UpdateSelectionCounts();
    }

    [RelayCommand]
    public void DeselectNode(ResultNode node)
    {
        if (_selected.Remove(node)) UpdateSelectionCounts();
    }

    [RelayCommand]
    public void ToggleNode(ResultNode node)
    {
        if (!_selected.Remove(node)) _selected.Add(node);
        UpdateSelectionCounts();
    }

    [RelayCommand]
    public void SelectAllVisible()
    {
        foreach (var row in VisibleFiles)
        {
            row.IsSelected = true;
            _selected.Add(row.Node);
        }
        UpdateSelectionCounts();
    }

    [RelayCommand]
    public void ClearSelection()
    {
        foreach (var row in VisibleFiles)
        {
            row.IsSelected = false;
        }
        _selected.Clear();
        UpdateSelectionCounts();
    }

    [RelayCommand]
    public void Preview(ResultNode node) => PreviewNode = node;

    /// <summary>"Recover All" (og contextual CTA — fires the recovery funnel with the whole session).</summary>
    [RelayCommand]
    public void RecoverAll() => RecoverRequested?.Invoke(this, RecoveryRequestKind.All);

    /// <summary>"Recover ..." (selection CTA — fires the funnel with the checked rows).</summary>
    [RelayCommand]
    public void RecoverSelection()
    {
        if (_selected.Count == 0) return;
        RecoverRequested?.Invoke(this, RecoveryRequestKind.Selection);
    }

    /// <summary>Saves the current session copy to a user-chosen location.</summary>
    [RelayCommand]
    public async Task SaveSessionCopyAsync(CancellationToken cancellationToken)
    {
        if (Session is null) return;
        var target = await _dialogs.PickFolderAsync("Choose where to save the scan session").ConfigureAwait(true);
        if (target is null) return;
        try
        {
            var destination = Path.Combine(target, Path.GetFileName(Session.SessionPath));
            File.Copy(Session.SessionPath, destination, overwrite: true);
            _logger.LogInformation("Session copied to {Destination}", destination);
            await _dialogs.AlertAsync("Session saved", "A copy of the scan session was saved to:\n" + destination).ConfigureAwait(true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning(ex, "Session copy failed");
            await _dialogs.AlertAsync("Session", "Saving the session copy failed: " + ex.Message).ConfigureAwait(true);
        }
    }

    private void UpdateSelectionCounts()
    {
        SelectedCount = _selected.Count;
        SelectedBytes = _selected.Sum(n => n.LogicalSize);
    }

    private static string FormatElapsed(TimeSpan elapsed) =>
        elapsed.TotalHours >= 1
            ? $"{(int)elapsed.TotalHours}:{elapsed.Minutes:D2}:{elapsed.Seconds:D2}"
            : $"{elapsed.Minutes}:{elapsed.Seconds:D2}";

    /// <summary>og ETA format: "07m43s".</summary>
    private static string FormatEta(TimeSpan eta) =>
        eta.TotalHours >= 1
            ? $"{(int)eta.TotalHours}h{eta.Minutes:D2}m"
            : $"{eta.Minutes:D2}m{eta.Seconds:D2}s";

    private static string FormatBytes(long bytes) => bytes switch
    {
        >= 1_073_741_824 => $"{bytes / 1_073_741_824.0:F2} GB",
        >= 1_048_576 => $"{bytes / 1_048_576.0:F1} MB",
        >= 1024 => $"{bytes / 1024.0:F1} KB",
        _ => $"{bytes} B",
    };
}

/// <summary>One middle-list row (og CListControl row: checkbox + name +
/// date + size). Wraps the immutable ResultNode with observable selection.</summary>
public partial class FileRowViewModel : ObservableObject
{
    public required ResultNode Node { get; init; }

    public string Name => Node.Name;

    public DateTimeOffset? Timestamp => Node.Timestamp;

    public long LogicalSize => Node.LogicalSize;

    public bool IsDirectory => Node.IsDirectory;

    [ObservableProperty] private bool _isSelected;
}

/// <summary>Which set the user asked to recover (og Recover All vs contextual Recover).</summary>
public enum RecoveryRequestKind
{
    All,
    Selection,
}

public enum ResultsViewMode
{
    Detail,
    LargeIcons,
}

public enum ResultsTreeMode
{
    Path,
    Type,
}
