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
    [ObservableProperty] private string? _etaText;
    [ObservableProperty] private bool _isScanRunning;
    [ObservableProperty] private bool _isPaused;
    [ObservableProperty] private string? _failureDiagnostic;
    [ObservableProperty] private ScanSummary? _summary;
    [ObservableProperty] private string _targetName = string.Empty;
    [ObservableProperty] private long _readErrorCount;
    [ObservableProperty] private long _readErrorBytes;
    [ObservableProperty] private IReadOnlyList<ReadErrorRange> _readErrors = Array.Empty<ReadErrorRange>();

    /// <summary>"Scan Completed, Files Found: 1,139,261 (147.04 GB)" — the og completion line.</summary>
    [ObservableProperty] private string? _completionText;

    // ---- Results browser state ----

    [ObservableProperty] private bool _isLoadingResults;
    [ObservableProperty] private LoadedSession? _session;
    [ObservableProperty] private string? _resultsError;
    [ObservableProperty] private ResultFilter _filter = ResultFilter.None;
    [ObservableProperty] private IReadOnlyList<ResultNode> _visibleFiles = Array.Empty<ResultNode>();
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

    public DetailViewModel(ILogger<DetailViewModel> logger, IScanService scans, IDialogService dialogs, IClock clock)
    {
        _logger = logger;
        _scans = scans;
        _dialogs = dialogs;
        _clock = clock;
    }

    /// <summary>True while any scan is executing — Pause/Stop availability.</summary>
    public bool IsScanActive => IsScanRunning;

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
        CompletionText = null;
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
            Phase = ScanPhase.Complete;
            CompletionText = $"Scan Completed, Files Found: {summary.Files:N0} ({FormatBytes(summary.BytesScanned)})";
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
            IsScanRunning = false;
            _scanCancellation.Dispose();
            _scanCancellation = null;
        }
    }

    /// <summary>Requests cancellation of the running scan (the shell confirms first — og stop-confirm dialog).</summary>
    public void CancelScan() => _scanCancellation?.Cancel();

    /// <summary>Installs a loaded session (fresh scan, import, or quick-phase results) and computes initial views.</summary>
    public void InstallSession(LoadedSession session, ResultFilter? prefilter = null)
    {
        Session = session;
        TotalFileCount = session.FileCount;
        TreeRoots = BuildTreeRoots(session);
        Filter = prefilter ?? ResultFilter.None;
        _selected.Clear();
        UpdateSelectionCounts();
        ApplyFilter();
        ResultsError = null;
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
            VisibleFiles = Array.Empty<ResultNode>();
            VisibleFileCount = 0;
            return;
        }

        var files = Session.Nodes.Where(n => !n.IsDirectory).ToArray();
        var filtered = FilterEvaluator.Apply(files, Filter);
        VisibleFiles = filtered;
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
        foreach (var node in VisibleFiles) _selected.Add(node);
        UpdateSelectionCounts();
    }

    [RelayCommand]
    public void ClearSelection()
    {
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

    private static string FormatBytes(long bytes) => bytes switch
    {
        >= 1_073_741_824 => $"{bytes / 1_073_741_824.0:F2} GB",
        >= 1_048_576 => $"{bytes / 1_048_576.0:F1} MB",
        >= 1024 => $"{bytes / 1024.0:F1} KB",
        _ => $"{bytes} B",
    };
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
