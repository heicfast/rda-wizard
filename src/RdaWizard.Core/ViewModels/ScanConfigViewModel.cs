using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using RdaWizard.Core.Models;
using RdaWizard.Core.Services;
using RdaWizard.Core.Settings;

namespace RdaWizard.Core.ViewModels;

/// <summary>
/// Scan configuration (UI-MAP §5: og picks the target first, then modes are
/// surfaced on the results/scan page; we give the step its own page per the
/// wizard stepper adaptation). Mode selection maps EXACTLY to rrecov flags.
/// Advanced panel is truthful: the engine exposes threads / name filter /
/// deleted-only — read-retry policy is engine-fixed, so it is *displayed as
/// information*, not as a fake control.
/// </summary>
public partial class ScanConfigViewModel : ViewModelBase
{
    private readonly ILogger<ScanConfigViewModel> _logger;
    private readonly ISettingsService _settings;

    [ObservableProperty] private ScanMode _mode = ScanMode.DeepPlusCarve;
    [ObservableProperty] private ScanTarget? _target;
    [ObservableProperty] private string _nameFilter = string.Empty;
    [ObservableProperty] private bool _deletedOnly;
    [ObservableProperty] private int _threads;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string? _refusalReason;

    /// <summary>Category chips are remembered and applied client-side when results appear (gap G-12 — core has no category query).</summary>
    [ObservableProperty] private IReadOnlySet<FileCategory> _selectedCategories = new HashSet<FileCategory>();

    /// <summary>Category pre-selection persisted to the results filter.</summary>
    public event EventHandler<IReadOnlySet<FileCategory>>? CategoriesConfirmed;

    /// <summary>Raised when the user starts the scan; the shell runs the ScanStarted flow.</summary>
    public event EventHandler<ScanOptions>? ScanStarted;

    public ScanConfigViewModel(ILogger<ScanConfigViewModel> logger, ISettingsService settings)
    {
        _logger = logger;
        _settings = settings;
        Mode = ParseDefaultMode(_settings.Current.DefaultScanMode);
    }

    /// <summary>Call when entering the page with a chosen target.</summary>
    public void Initialize(ScanTarget target)
    {
        Target = target;
        RefusalReason = null;
        _logger.LogInformation("Scan config for {Target}", target.Description);
    }

    public IReadOnlyList<FileCategory> AllCategories { get; } =
        [FileCategory.Photos, FileCategory.Videos, FileCategory.Documents, FileCategory.Audio, FileCategory.Archives, FileCategory.Email, FileCategory.Other];

    [RelayCommand]
    public void ToggleCategory(FileCategory category)
    {
        var next = new HashSet<FileCategory>(SelectedCategories);
        if (!next.Remove(category))
        {
            next.Add(category);
        }
        SelectedCategories = next;
    }

    /// <summary>Builds the engine options (flag mapping unit-tested in ScanModeExtensions).</summary>
    /// <param name="sessionPath">Where the engine writes the .crsf session.</param>
    /// <param name="stageLogPath">Where the engine writes the detailed stage log.</param>
    public ScanOptions BuildScanOptions(string sessionPath, string stageLogPath)
    {
        if (Target is null)
        {
            throw new InvalidOperationException("No scan target is selected.");
        }

        if (Threads is < 0 or > 64)
        {
            throw new InvalidOperationException("Threads must be between 1 and 64 (0 = engine default).");
        }

        return new ScanOptions
        {
            Target = Target,
            Mode = Mode,
            Threads = Threads,
            NameFilter = string.IsNullOrWhiteSpace(NameFilter) ? null : NameFilter.Trim(),
            DeletedOnly = DeletedOnly,
            SessionPath = sessionPath,
            StageLogPath = stageLogPath,
        };
    }

    /// <summary>Starts the scan: builds options, fires category confirmation + ScanStarted.</summary>
    public ScanOptions StartScan(string sessionPath, string stageLogPath)
    {
        var options = BuildScanOptions(sessionPath, stageLogPath);
        CategoriesConfirmed?.Invoke(this, SelectedCategories);
        _logger.LogInformation(
            "Scan start requested: target={Target} mode={Mode} threads={Threads} filter={Filter} deletedOnly={DeletedOnly}",
            options.Target.Description, Mode, Threads, options.NameFilter ?? "-", DeletedOnly);
        ScanStarted?.Invoke(this, options);
        return options;
    }

    private static ScanMode ParseDefaultMode(string value) => value switch
    {
        "quick" => ScanMode.Quick,
        "deep" => ScanMode.Deep,
        _ => ScanMode.DeepPlusCarve,
    };
}
