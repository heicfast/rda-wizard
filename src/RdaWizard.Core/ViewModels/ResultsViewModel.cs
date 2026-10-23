using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using RdaWizard.Core.Filters;
using RdaWizard.Core.Models;
using RdaWizard.Core.Services;

namespace RdaWizard.Core.ViewModels;

/// <summary>
/// Results explorer (UI-MAP §6/§9: path tree + type tree + raw groupings,
/// detail/icon view modes, filter bar, selection model, session save/load).
/// This ViewModel owns the LOADED SNAPSHOT + client-side queries (ADR-9) and
/// a VIRTUALIZED page source for the file list (data virtualization at 1M+
/// nodes is the Controls layer's job; here we own the paged abstraction).
/// </summary>
public partial class ResultsViewModel : ViewModelBase
{
    private readonly ILogger<ResultsViewModel> _logger;
    private readonly ISessionService _sessions;
    private readonly IDialogService _dialogs;

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private LoadedSession? _session;
    [ObservableProperty] private string? _errorDiagnostic;
    [ObservableProperty] private ResultFilter _filter = ResultFilter.None;
    [ObservableProperty] private IReadOnlyList<ResultNode> _visibleFiles = Array.Empty<ResultNode>();
    [ObservableProperty] private long _visibleFileCount;
    [ObservableProperty] private long _totalFileCount;
    [ObservableProperty] private long _selectedCount;
    [ObservableProperty] private long _selectedBytes;
    [ObservableProperty] private ResultNode? _previewNode;
    [ObservableProperty] private ResultsViewMode _viewMode = ResultsViewMode.Detail;
    [ObservableProperty] private ResultsTreeMode _treeMode = ResultsTreeMode.Path;

    /// <summary>Selected nodes (recovery input). Maintained by Select/SelectAll/Invert commands.</summary>
    public IReadOnlySet<ResultNode> SelectedNodes => _selected;

    private readonly HashSet<ResultNode> _selected = new();

    /// <summary>Category pre-selection carried over from scan config (gap G-12: client-side).</summary>
    [ObservableProperty] private IReadOnlySet<FileCategory> _categoryChips = new HashSet<FileCategory>();

    public ResultsViewModel(ILogger<ResultsViewModel> logger, ISessionService sessions, IDialogService dialogs)
    {
        _logger = logger;
        _sessions = sessions;
        _dialogs = dialogs;
    }

    /// <summary>Installs a loaded session (fresh scan or import) and computes initial views.</summary>
    public void InstallSession(LoadedSession session, IReadOnlySet<FileCategory>? preselectedCategories = null)
    {
        Session = session;
        TotalFileCount = session.FileCount;
        CategoryChips = preselectedCategories ?? new HashSet<FileCategory>();
        Filter = CategoryChips.Count > 0
            ? Filter with { Categories = CategoryChips }
            : ResultFilter.None;
        _selected.Clear();
        UpdateSelectionCounts();
        ApplyFilter();
        ErrorDiagnostic = null;
        _logger.LogInformation("Results installed: {Total} files, {Carved} carved, {Lost} lost-partition nodes",
            session.FileCount, session.CarvedNodes.Count, session.LostNodes.Count);
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
        ApplyFilter();
    }

    [RelayCommand]
    public void ToggleCategoryChip(FileCategory category)
    {
        var next = new HashSet<FileCategory>(CategoryChips);
        if (!next.Remove(category))
        {
            next.Add(category);
        }
        CategoryChips = next;
        Filter = Filter with { Categories = next };
        ApplyFilter();
    }

    [RelayCommand]
    public void SetSearchText(string? text)
    {
        Filter = Filter with { NameContains = string.IsNullOrWhiteSpace(text) ? null : text.Trim() };
        ApplyFilter();
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
    public void InvertSelection()
    {
        if (Session is null) return;
        var all = Session.Nodes.Where(n => !n.IsDirectory).ToHashSet();
        foreach (var node in VisibleFiles) all.Remove(node);
        foreach (var node in all) _selected.Add(node);
        foreach (var node in VisibleFiles) _selected.Remove(node);
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

    /// <summary>Saves the current session copy to a user-chosen location (CRSF export).</summary>
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
