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

        /// <summary>User-stopped scan with partial results browsable (og:
        /// stopped scans keep their found files in the tree).</summary>
        Stopped,
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

    /// <summary>og CFilterBar visibility (toolbar Filter button toggles it).</summary>
    [ObservableProperty] private bool _filterBarVisible;

    /// <summary>Contextual bottom CTA (og: "Recover All ..." with no
    /// selection, "Recover ..." once rows are checked).</summary>
    [ObservableProperty] private string _recoverCtaLabel = "Recover All ...";

    /// <summary>Path scope from a folder scan target (og PushFolderFilter
    /// equivalence): the visible tree roots at the folder.</summary>
    private string? _pathScope;

    /// <summary>Preview-pane wrapper (og right pane: folder/file states).</summary>
    [ObservableProperty] private PreviewItemViewModel? _previewItem;

    /// <summary>Selected nodes (recovery input). Maintained by selection commands.</summary>
    public IReadOnlySet<ResultNode> SelectedNodes => _selected;

    private readonly HashSet<ResultNode> _selected = new();

    /// <summary>Tree node wrapper for hierarchical display (path tree):
    /// og root rows carry icon + name + file count ("Windows (C:) 1.1M").</summary>
    public sealed record TreeNodeItem(ResultNode Node)
    {
        public string Name => Node.Name;
        public string FullPath => Node.FullPath;
        public List<TreeNodeItem> Children { get; } = new();

        /// <summary>Files under this node (recursive, non-directory).
        /// Rolled up after the tree is built.</summary>
        public long FileCount { get; set; }

        /// <summary>og count format: "1.1M" / "4.3K" / "88".</summary>
        public string CountText => FileCount switch
        {
            >= 1_000_000 => $"{FileCount / 1_000_000.0:0.#}M",
            >= 1_000 => $"{FileCount / 1_000.0:0.#}K",
            0 => "0",
            _ => FileCount.ToString(System.Globalization.CultureInfo.InvariantCulture),
        };

        /// <summary>Icon resource key by origin (og root icons: volume /
        /// File Path Lost / Reconstructed).</summary>
        public string IconKey => Node.Origin switch
        {
            ResultOrigin.Lost => "Icon.Tree.FilePathLost",
            ResultOrigin.Carved => "Icon.Tree.Reconstructed",
            _ => "Icon.Tree.DirectoryIntact",
        };
    }

    /// <summary>Right preview pane item (og CRightContent states): folder
    /// (icon + name + size + Open) or file (icon + name + Recovery Chances
    /// + Size / Date Modified / Path / Status).</summary>
    public sealed record PreviewItemViewModel
    {
        public required ResultNode Node { get; init; }

        public string Name => Node.Name;

        public string Path => Node.FullPath;

        public bool IsFolder => Node.IsDirectory;

        public long Size => Node.LogicalSize;

        public DateTimeOffset? Date => Node.Timestamp;

        /// <summary>og "Status" row: Existing / Deleted / Reconstructed.</summary>
        public string Status => (Node.Origin, Node.DeletedState) switch
        {
            (ResultOrigin.Carved, _) => "Reconstructed",
            (_, DeletedState.Deleted) => "Deleted",
            (_, DeletedState.PartiallyOverwritten) => "Partially overwritten",
            (_, DeletedState.Damaged) => "Damaged",
            _ => "Existing",
        };

        /// <summary>og "Recovery Chances" row value.</summary>
        public string RecoveryChances => Node.Confidence switch
        {
            ConfidenceGrade.Excellent => "High",
            ConfidenceGrade.PartialRisk => "Medium",
            _ => "Unknown",
        };

        /// <summary>Design-system icon key by category (file) or folder.</summary>
        public string IconKey => Node.IsDirectory
            ? "Icon.Tree.DirectoryIntact"
            : PreviewIcons.ForExtension(Node.Extension);
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
        _pathScope = options.Target.PathPrefix;
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
            // Stop route (og): a stopped scan keeps its found files browsable.
            // Truth: the engine saves the session cooperatively (exit 3) and
            // auto-checkpoints at stage boundaries (G-13) — the stopped
            // session (last completed stage) is loadable when any stage
            // finished; before that there is nothing to show.
            _logger.LogInformation("Scan cancelled by user; checking for a stopped session");
            var stopped = await TryInstallStoppedSessionAsync(options).ConfigureAwait(true);
            if (stopped is not null)
            {
                Phase = ScanPhase.Stopped;
                UpdateStatusPrimary();
                ScanCompleted?.Invoke(this, stopped);
            }
            else
            {
                FailureDiagnostic = "The scan was stopped before any stage completed — no results were saved.";
                ScanFailed?.Invoke(this, FailureDiagnostic);
            }
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

    /// <summary>Loads the stopped scan's session (checkpoint or exit-3
    /// save): returns a summary when partial results are browsable.</summary>
    private async Task<ScanSummary?> TryInstallStoppedSessionAsync(ScanOptions options)
    {
        try
        {
            if (!File.Exists(options.SessionPath))
            {
                return null;
            }

            var loaded = await _sessions.LoadAsync(options.SessionPath).ConfigureAwait(true);
            if (!loaded.Success || loaded.Session is not { FileCount: > 0 } session)
            {
                return null;
            }

            InstallSession(session);
            return new ScanSummary
            {
                Files = (int)Math.Min(session.FileCount, int.MaxValue),
                Deleted = 0,
                Carved = session.CarvedNodes.Count,
                LostPartitions = session.LostNodes.Count,
                BytesScanned = 0,
                Elapsed = TimeSpan.Zero,
                SessionPath = options.SessionPath,
            };
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Stopped-session load failed");
            return null;
        }
    }

    private string PhaseLabel() => Phase switch
    {
        ScanPhase.QuickScanning => "Quick Scanning",
        ScanPhase.AdvancedScanning => "Advanced Scanning",
        ScanPhase.Paused => "Scan Paused",
        ScanPhase.Complete => "Scan Completed",
        ScanPhase.Stopped => "Scan Stopped",
        _ => string.Empty,
    };

    partial void OnPhaseChanged(ScanPhase value) => UpdateStatusPrimary();

    partial void OnTotalFileCountChanged(long value) => UpdateStatusPrimary();

    partial void OnFoundBytesChanged(long value) => UpdateStatusPrimary();

    private void UpdateStatusPrimary()
    {
        if (!IsScanRunning && Phase is not (ScanPhase.Complete or ScanPhase.Stopped))
        {
            StatusPrimary = string.Empty;
            return;
        }

        if (Phase is ScanPhase.Complete or ScanPhase.Stopped)
        {
            var label = Phase == ScanPhase.Stopped ? "Scan Stopped" : "Scan Completed";
            StatusPrimary = TotalFileCount > 0
                ? $"{label}, Files Found: {TotalFileCount:N0} ({FormatBytes(FoundBytes)})"
                : $"{label}, Files Found: {Summary?.Files ?? 0:N0}";
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
        if (_pathScope is { } scope)
        {
            // Folder target (og PushFolderFilter): the visible tree roots at
            // the scanned folder.
            filter = filter with { PathPrefix = scope };
        }
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
        UpdateRecoverCta();
        ApplyFilter();
        ResultsError = null;
        HasPartialResults = IsScanRunning;
        _logger.LogInformation("Results installed: {Total} files, {Carved} carved, {Lost} lost-partition nodes",
            session.FileCount, session.CarvedNodes.Count, session.LostNodes.Count);
    }

    private List<TreeNodeItem> BuildTreeRoots(LoadedSession session)
    {
        var byPath = new Dictionary<string, TreeNodeItem>(StringComparer.OrdinalIgnoreCase);
        var roots = new List<TreeNodeItem>();

        foreach (var node in session.Nodes)
        {
            if (_pathScope is { } scope && !Filters.FilterEvaluator.PathScopeMatches(node.FullPath, scope)
                && !string.Equals(node.FullPath.TrimEnd('/', '\\'), scope.TrimEnd('/', '\\'), StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

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

        // Roll file counts up the tree (og root counts: "1.11M/1.1M").
        foreach (var root in roots)
        {
            RollupCounts(root);
        }

        return roots;
    }

    private static long RollupCounts(TreeNodeItem item)
    {
        long count = item.Node.IsDirectory ? 0 : 1;
        foreach (var child in item.Children)
        {
            count += RollupCounts(child);
        }

        item.FileCount = count;
        return count;
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
        Filter = _pathScope is { } scope ? new ResultFilter { PathPrefix = scope } : ResultFilter.None;
        SearchText = string.Empty;
        ApplyFilter();
    }

    /// <summary>Toolbar Filter button: toggles the og CFilterBar row.</summary>
    [RelayCommand]
    public void ToggleFilterBar() => FilterBarVisible = !FilterBarVisible;

    /// <summary>Filter bar "File Type" pick (og category dropdown).</summary>
    [RelayCommand]
    public void FilterCategory(string? category)
    {
        var set = new HashSet<Models.FileCategory>();
        if (category is { Length: > 0 } && Enum.TryParse<Models.FileCategory>(category, ignoreCase: true, out var parsed))
        {
            set.Add(parsed);
        }
        Filter = Filter with { Categories = set };
        ApplyFilter();
    }

    /// <summary>Filter bar "Date Modified" pick (og date dropdown presets).</summary>
    [RelayCommand]
    public void FilterDateRange(string? preset)
    {
        var from = preset switch
        {
            "today" => _clock.UtcNow.AddDays(-1),
            "week" => _clock.UtcNow.AddDays(-7),
            "month" => _clock.UtcNow.AddMonths(-1),
            _ => (DateTimeOffset?)null,
        };
        Filter = Filter with { ModifiedFrom = from, ModifiedTo = null };
        ApplyFilter();
    }

    /// <summary>Filter bar "File Size" pick (og size dropdown presets).</summary>
    [RelayCommand]
    public void FilterSizeRange(string? preset)
    {
        long? min = null;
        long? max = null;
        switch (preset)
        {
            case "small":
                max = 512L * 1024;
                break;
            case "medium":
                min = 512L * 1024;
                max = 1024L * 1024 * 512;
                break;
            case "large":
                min = 1024L * 1024 * 512;
                break;
        }
        Filter = Filter with { MinSize = min, MaxSize = max };
        ApplyFilter();
    }

    /// <summary>Filter bar "Show" pick (og Existing/Deleted).</summary>
    [RelayCommand]
    public void FilterShow(string? preset)
    {
        Filter = Filter with { DeletedOnly = preset switch
        {
            "deleted" => true,
            "existing" => false,
            _ => null,
        } };
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
    public void Preview(ResultNode node)
    {
        PreviewNode = node;
        PreviewItem = node is null ? null : new PreviewItemViewModel { Node = node };
    }

    /// <summary>Context menu / preview "Open file location": scopes the list
    /// to the node's folder (og address-bar navigation over the flat list).</summary>
    [RelayCommand]
    public void OpenLocation(FileRowViewModel? row)
    {
        if (row is null)
        {
            return;
        }

        var parent = row.Node.ParentPath;
        Filter = Filter with { PathPrefix = parent ?? _pathScope };
        Breadcrumb = parent ?? TargetName;
        ApplyFilter();
    }

    /// <summary>Preview pane folder "Open": scopes the list to the folder
    /// (og navigates the middle list into the folder).</summary>
    [RelayCommand]
    public void OpenPreviewFolder(PreviewItemViewModel? item)
    {
        if (item is null || !item.IsFolder)
        {
            return;
        }

        Filter = Filter with { PathPrefix = item.Node.FullPath };
        Breadcrumb = item.Node.FullPath;
        ApplyFilter();
    }

    /// <summary>Tree selection: scopes the list to the selected folder
    /// (og tree→list navigation).</summary>
    public void ScopeToNode(ResultNode? node)
    {
        if (node is null)
        {
            return;
        }

        Filter = Filter with { PathPrefix = node.IsDirectory ? node.FullPath : node.ParentPath };
        Breadcrumb = node.IsDirectory ? node.FullPath : (node.ParentPath ?? TargetName);
        ApplyFilter();
    }

    /// <summary>Context menu "Select unrecovered files" (og: deleted-state
    /// rows among the visible list).</summary>
    [RelayCommand]
    public void SelectUnrecovered()
    {
        foreach (var row in VisibleFiles)
        {
            if (row.Node.DeletedState != DeletedState.Active)
            {
                row.IsSelected = true;
                _selected.Add(row.Node);
            }
        }
        UpdateSelectionCounts();
    }

    /// <summary>Context menu "Smart select best files" (og: high-confidence
    /// rows among the visible list).</summary>
    [RelayCommand]
    public void SelectBest()
    {
        foreach (var row in VisibleFiles)
        {
            if (row.Node.Confidence == ConfidenceGrade.Excellent)
            {
                row.IsSelected = true;
                _selected.Add(row.Node);
            }
        }
        UpdateSelectionCounts();
    }

    /// <summary>Bottom CTA (og contextual): "Recover All ..." with nothing
    /// checked, "Recover ..." with a selection — one command, contextual target.</summary>
    [RelayCommand]
    public void Recover() => RecoverRequested?.Invoke(this,
        _selected.Count > 0 ? RecoveryRequestKind.Selection : RecoveryRequestKind.All);

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
        UpdateRecoverCta();
    }

    private void UpdateRecoverCta() => RecoverCtaLabel = SelectedCount > 0 ? "Recover ..." : "Recover All ...";

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

    /// <summary>Row icon key (og rows: file-type icon / folder icon).</summary>
    public string IconKey => Node.IsDirectory
        ? "Icon.Tree.DirectoryIntact"
        : PreviewIcons.ForExtension(Node.Extension);

    /// <summary>og Type column: "File folder" / extension label.</summary>
    public string TypeText => Node.IsDirectory
        ? "File folder"
        : (Node.Extension.Length == 0 ? "File" : Node.Extension.ToUpperInvariant() + " File");
}

/// <summary>File-category → design-system icon key (og rows carry file-type icons).</summary>
internal static class PreviewIcons
{
    public static string ForExtension(string extension) => extension.ToLowerInvariant() switch
    {
        "jpg" or "jpeg" or "png" or "gif" or "bmp" or "tif" or "tiff" or "webp" or "heic" => "Icon.Category.Picture",
        "mp4" or "mov" or "avi" or "mkv" or "wmv" or "flv" or "webm" or "m4v" => "Icon.Category.Video",
        "mp3" or "wav" or "flac" or "aac" or "ogg" or "wma" or "m4a" => "Icon.Category.Audio",
        "zip" or "rar" or "7z" or "tar" or "gz" or "bz2" or "xz" => "Icon.Category.Archive",
        "eml" or "msg" or "pst" => "Icon.Category.Email",
        "doc" or "docx" or "pdf" or "txt" or "xls" or "xlsx" or "ppt" or "pptx" or "rtf" or "odt" => "Icon.Category.Document",
        _ => "Icon.Category.Other",
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
