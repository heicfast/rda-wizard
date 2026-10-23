using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using RdaWizard.Core.Navigation;
using RdaWizard.Core.Wizard;

namespace RdaWizard.Core.ViewModels;

/// <summary>
/// The shell (og: DrwMainWindow): custom title bar + 6-entry sidebar +
/// stacked page host. Owns the wizard state machine + navigation; every page
/// ViewModel is a child created by DI; the shell shows exactly one at a time.
/// The og has no stepper and no persistent status bar — removed per the
/// og-parity rebuild directive.
/// </summary>
public partial class MainViewModel : ViewModelBase
{
    private readonly ILogger<MainViewModel> _logger;
    private readonly NavigationService _navigation;
    private WizardState _wizardState = WizardState.Select;

    public MainViewModel(
        ILogger<MainViewModel> logger,
        NavigationService navigation,
        LandingViewModel landing,
        DetailViewModel detail,
        SdCardViewModel sdCard,
        MoreToolsViewModel moreTools,
        FileRepairPageViewModel fileRepair,
        LostPartitionViewModel lostPartition,
        ChronoSnapViewModel chronoSnap,
        HealthViewModel health,
        RepairViewModel repair,
        RecoveryViewModel recovery,
        SettingsViewModel settings,
        AboutViewModel about)
    {
        _logger = logger;
        _navigation = navigation;

        Landing = landing;
        Detail = detail;
        SdCard = sdCard;
        MoreTools = moreTools;
        FileRepair = fileRepair;
        LostPartition = lostPartition;
        ChronoSnap = chronoSnap;
        Health = health;
        Repair = repair;
        Recovery = recovery;
        Settings = settings;
        About = about;

        _navigation.Navigated += OnNavigated;
    }

    public WizardStateMachine Machine { get; } = new();

    // ---- Page ViewModels (one per PageKey) ----
    public LandingViewModel Landing { get; }
    public DetailViewModel Detail { get; }
    public SdCardViewModel SdCard { get; }
    public MoreToolsViewModel MoreTools { get; }
    public FileRepairPageViewModel FileRepair { get; }
    public LostPartitionViewModel LostPartition { get; }
    public ChronoSnapViewModel ChronoSnap { get; }
    public HealthViewModel Health { get; }
    public RepairViewModel Repair { get; }

    /// <summary>The recovery flow controller (destination dialog family +
    /// progress). Not a page: the og recovery is a modal over the Detail page.</summary>
    public RecoveryViewModel Recovery { get; }

    public SettingsViewModel Settings { get; }
    public AboutViewModel About { get; }

    /// <summary>Page key of the visible page (the App maps it to a View via DataTemplates).</summary>
    [ObservableProperty]
    private PageKey _currentPage = PageKey.Landing;

    /// <summary>The ViewModel of the visible page — the shell binds its ContentControl here.</summary>
    public ViewModelBase CurrentPageViewModel => CurrentPage switch
    {
        PageKey.Landing => Landing,
        PageKey.Detail => Detail,
        PageKey.SdCard => SdCard,
        PageKey.MoreTools => MoreTools,
        PageKey.FileRepair => FileRepair,
        PageKey.LostPartition => LostPartition,
        PageKey.ChronoSnap => ChronoSnap,
        PageKey.Health => Health,
        PageKey.Repair => Repair,
        PageKey.Settings => Settings,
        PageKey.About => About,
        _ => Landing,
    };

    /// <summary>Current wizard state (drives navigation legality + the landing's scan-running state).</summary>
    public WizardState WizardState
    {
        get => _wizardState;
        private set
        {
            if (!SetProperty(ref _wizardState, value))
            {
                return;
            }

            Landing.IsScanRunning = value == WizardState.Scanning;
            _navigation.SetStateGate(value);
        }
    }

    /// <summary>Applies a state-machine transition; returns the refusal reason or null on success.</summary>
    public string? ExecuteTransition(string edgeName, WizardContext context)
    {
        var reason = Machine.TryTransition(edgeName, context);
        if (reason is not null)
        {
            _logger.LogInformation("Wizard refused '{Edge}': {Reason}", edgeName, reason);
            return reason;
        }

        WizardState = Machine.State;
        return null;
    }

    /// <summary>Navigates to the page matching the current wizard state (call after transitions).</summary>
    public void NavigateForState()
    {
        var target = WizardState switch
        {
            WizardState.Select => PageKey.Landing,
            WizardState.Scanning => PageKey.Detail,
            WizardState.Results => PageKey.Detail,
            WizardState.Recovering => PageKey.Detail,
            _ => PageKey.Landing,
        };

        if (!_navigation.TryNavigate(target))
        {
            _logger.LogWarning("State-navigation to {Page} refused; forcing reset to Landing", target);
            _navigation.ResetToLanding();
            CurrentPage = PageKey.Landing;
        }
    }

    /// <summary>Opens a sidebar destination / auxiliary page within state legality.</summary>
    public bool TryOpenAuxiliary(PageKey key) => _navigation.TryNavigate(key);

    /// <summary>Sidebar navigation command (XAML string parameter → PageKey).</summary>
    [CommunityToolkit.Mvvm.Input.RelayCommand]
    private void Navigate(string pageName)
    {
        if (Enum.TryParse<PageKey>(pageName, ignoreCase: true, out var key))
        {
            _navigation.TryNavigate(key);
        }
        else
        {
            _logger.LogWarning("Sidebar navigation ignored: unknown page '{Page}'", pageName);
        }
    }

    public bool TryGoBack() => _navigation.TryGoBack();

    /// <summary>Finish → new wizard run: resets machine + navigation to the landing.</summary>
    public void ResetWizard()
    {
        Machine.Reset();
        WizardState = WizardState.Select;
        _navigation.ResetToLanding();
        CurrentPage = PageKey.Landing;
    }

    /// <summary>Navigation service raised it — update the visible page.</summary>
    private void OnNavigated(object? sender, NavigationEventArgs e)
    {
        PostOnUi(() =>
        {
            CurrentPage = e.To;
            OnPropertyChanged(nameof(CurrentPageViewModel));
            OnPropertyChanged(nameof(CanGoBack));
        });
    }

    public bool CanGoBack => _navigation.CanGoBack;
}
