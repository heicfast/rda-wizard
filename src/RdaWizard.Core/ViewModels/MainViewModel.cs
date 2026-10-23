using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using RdaWizard.Core.Navigation;
using RdaWizard.Core.Wizard;

namespace RdaWizard.Core.ViewModels;

/// <summary>
/// The shell: owns the wizard state machine + navigation, exposes the
/// stepper phases and per-page availability, and the status bar truth
/// (driver state, engine version). Every page ViewModel is a child created
/// by DI; the shell shows exactly one at a time.
/// </summary>
public partial class MainViewModel : ViewModelBase
{
    private readonly ILogger<MainViewModel> _logger;
    private readonly NavigationService _navigation;
    private WizardState _wizardState = WizardState.Select;
    private bool _isBusy;

    public MainViewModel(
        ILogger<MainViewModel> logger,
        NavigationService navigation,
        HomeViewModel home,
        ScanConfigViewModel scanConfig,
        ScanProgressViewModel scanProgress,
        ResultsViewModel results,
        RecoveryViewModel recovery,
        CompletionViewModel completion,
        HealthViewModel health,
        RepairViewModel repair,
        SettingsViewModel settings,
        AboutViewModel about)
    {
        _logger = logger;
        _navigation = navigation;

        Home = home;
        ScanConfig = scanConfig;
        ScanProgress = scanProgress;
        Results = results;
        Recovery = recovery;
        Completion = completion;
        Health = health;
        Repair = repair;
        Settings = settings;
        About = about;

        _navigation.Navigated += OnNavigated;
    }

    public WizardStateMachine Machine { get; } = new();

    // ---- Page ViewModels (one per PageKey) ----
    public HomeViewModel Home { get; }
    public ScanConfigViewModel ScanConfig { get; }
    public ScanProgressViewModel ScanProgress { get; }
    public ResultsViewModel Results { get; }
    public RecoveryViewModel Recovery { get; }
    public CompletionViewModel Completion { get; }
    public HealthViewModel Health { get; }
    public RepairViewModel Repair { get; }
    public SettingsViewModel Settings { get; }
    public AboutViewModel About { get; }

    /// <summary>Page key of the visible page (the App maps it to a View via DataTemplates).</summary>
    [ObservableProperty]
    private PageKey _currentPage = PageKey.Home;

    /// <summary>The ViewModel of the visible page — the shell binds its ContentControl here.</summary>
    public ViewModelBase CurrentPageViewModel => CurrentPage switch
    {
        PageKey.Home => Home,
        PageKey.ScanConfig => ScanConfig,
        PageKey.ScanProgress => ScanProgress,
        PageKey.Results => Results,
        PageKey.Recovery => Recovery,
        PageKey.Completion => Completion,
        PageKey.Health => Health,
        PageKey.Repair => Repair,
        PageKey.Settings => Settings,
        PageKey.About => About,
        _ => Home,
    };

    /// <summary>Current wizard state (drives the stepper + nav legality).</summary>
    public WizardState WizardState
    {
        get => _wizardState;
        private set
        {
            if (!SetProperty(ref _wizardState, value))
            {
                return;
            }

            OnPropertyChanged(nameof(Phase));
            OnPropertyChanged(nameof(StepIndex));
            _navigation.SetStateGate(value);
        }
    }

    /// <summary>Stepper rail phase (Select → Scan → Recover).</summary>
    public WizardPhase Phase => WizardState.Phase();

    /// <summary>Stepper index 0..2.</summary>
    public int StepIndex => (int)Phase;

    /// <summary>Global busy flag (modal operations in flight; blocks duplicate navigations).</summary>
    public bool IsBusy
    {
        get => _isBusy;
        private set => SetProperty(ref _isBusy, value);
    }

    /// <summary>Applies a state-machine transition; returns the refusal reason or null on success.</summary>
    public string? ExecuteTransition(string edgeName, WizardContext context)
    {
        if (IsBusy)
        {
            return "An operation is running — wait for it to finish.";
        }

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
            WizardState.Select => PageKey.Home,
            WizardState.Configure => PageKey.ScanConfig,
            WizardState.Scanning => PageKey.ScanProgress,
            WizardState.Results => PageKey.Results,
            WizardState.Recovering => PageKey.Recovery,
            WizardState.Complete => PageKey.Completion,
            _ => PageKey.Home,
        };

        if (!_navigation.TryNavigate(target))
        {
            _logger.LogWarning("State-navigation to {Page} refused; forcing reset to Home", target);
            _navigation.ResetToHome();
            CurrentPage = PageKey.Home;
        }
    }

    /// <summary>Opens an auxiliary page (Health/Repair/Settings/About) within state legality.</summary>
    public bool TryOpenAuxiliary(PageKey key) => _navigation.TryNavigate(key);

    public bool TryGoBack() => _navigation.TryGoBack();

    /// <summary>Finish → new wizard run: resets machine + navigation to Home.</summary>
    public void ResetWizard()
    {
        Machine.Reset();
        WizardState = WizardState.Select;
        _navigation.ResetToHome();
        CurrentPage = PageKey.Home;
    }

    /// <summary>Directly sets the page (navigation service raised it) — used by the nav event handler below.</summary>
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

    /// <summary>Sets busy state around a long-running shell operation.</summary>
    public async Task RunShellOperationAsync(Func<Task> operation)
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        try
        {
            await operation().ConfigureAwait(true);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
