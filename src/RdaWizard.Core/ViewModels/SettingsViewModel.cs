using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using RdaWizard.Core.Services;
using RdaWizard.Core.Settings;

namespace RdaWizard.Core.ViewModels;

/// <summary>Settings page (H): theme, language, defaults, log location, preview caps.</summary>
public partial class SettingsViewModel : ViewModelBase
{
    private readonly ILogger<SettingsViewModel> _logger;
    private readonly ISettingsService _settings;
    private readonly Localization.ILocalizationService _localization;

    [ObservableProperty] private string _theme = "system";
    [ObservableProperty] private string _language = "system";
    [ObservableProperty] private string _defaultScanMode = "deep+carve";
    [ObservableProperty] private string? _defaultRecoveryDestination;
    [ObservableProperty] private string _collisionPolicy = "rename";
    [ObservableProperty] private bool _preserveFolderStructure = true;
    [ObservableProperty] private string? _logDirectory;
    [ObservableProperty] private long _previewMaxBytes = 100L * 1024 * 1024;
    [ObservableProperty] private int _previewTimeoutSeconds = 15;
    [ObservableProperty] private string _logLevel = "Information";
    [ObservableProperty] private bool _isSaving;

    /// <summary>Raised when settings were saved (App re-applies theme/language live).</summary>
    public event EventHandler<AppSettings>? SettingsApplied;

    public SettingsViewModel(ILogger<SettingsViewModel> logger, ISettingsService settings, Localization.ILocalizationService localization)
    {
        _logger = logger;
        _settings = settings;
        _localization = localization;
        LoadFrom(_settings.Current);
    }

    public IReadOnlyList<string> ThemeOptions { get; } = ["system", "light", "dark"];
    public IReadOnlyList<string> LanguageOptions { get; } = ["system", "en", "zh-CN"];
    public IReadOnlyList<string> ScanModeOptions { get; } = ["quick", "deep", "deep+carve"];
    public IReadOnlyList<string> CollisionOptions { get; } = ["rename", "skip", "ask"];
    public IReadOnlyList<string> LogLevelOptions { get; } = ["Information", "Debug", "Warning"];

    /// <summary>String tables available to the language picker.</summary>
    public string CurrentCulture => _localization.CurrentCulture;

    [RelayCommand]
    public async Task SaveAsync(CancellationToken cancellationToken)
    {
        IsSaving = true;
        try
        {
            var settings = new AppSettings
            {
                Theme = Theme,
                Language = Language,
                DefaultScanMode = DefaultScanMode,
                DefaultRecoveryDestination = DefaultRecoveryDestination,
                CollisionPolicy = CollisionPolicy,
                PreserveFolderStructure = PreserveFolderStructure,
                LogDirectory = LogDirectory,
                PreviewMaxBytes = PreviewMaxBytes,
                PreviewTimeoutSeconds = PreviewTimeoutSeconds,
                LogLevel = LogLevel,
            };
            await _settings.SaveAsync(settings, cancellationToken).ConfigureAwait(true);
            _localization.SetCulture(settings.Language);
            SettingsApplied?.Invoke(this, settings);
            _logger.LogInformation("Settings saved: theme={Theme} language={Language} mode={Mode}", Theme, Language, DefaultScanMode);
        }
        finally
        {
            IsSaving = false;
        }
    }

    private void LoadFrom(AppSettings settings)
    {
        Theme = settings.Theme;
        Language = settings.Language;
        DefaultScanMode = settings.DefaultScanMode;
        DefaultRecoveryDestination = settings.DefaultRecoveryDestination;
        CollisionPolicy = settings.CollisionPolicy;
        PreserveFolderStructure = settings.PreserveFolderStructure;
        LogDirectory = settings.LogDirectory;
        PreviewMaxBytes = settings.PreviewMaxBytes;
        PreviewTimeoutSeconds = settings.PreviewTimeoutSeconds;
        LogLevel = settings.LogLevel;
    }
}
