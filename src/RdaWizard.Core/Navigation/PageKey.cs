namespace RdaWizard.Core.Navigation;

/// <summary>
/// Wizard page keys — one per navigable page. Pages are ViewModels
/// (resolved from DI); the App layer maps them to Views via DataTemplates.
/// Keys exist only for pages we actually ship (no dead keys).
/// </summary>
public enum PageKey
{
    /// <summary>Home / device selection.</summary>
    Home,

    /// <summary>Scan configuration.</summary>
    ScanConfig,

    /// <summary>Scan progress.</summary>
    ScanProgress,

    /// <summary>Results explorer.</summary>
    Results,

    /// <summary>Recovery queue + destination.</summary>
    Recovery,

    /// <summary>Completion report.</summary>
    Completion,

    /// <summary>Disk health (SMART) — reachable from Home.</summary>
    Health,

    /// <summary>RawFixer transactional repair — reachable from Home.</summary>
    Repair,

    /// <summary>Settings — modal-style page reachable from the shell.</summary>
    Settings,

    /// <summary>About: versions, driver/engine status, licenses.</summary>
    About,
}

/// <summary>Optional navigation parameters (e.g. the disk a health page was opened for).</summary>
public sealed record NavigationParameter
{
    public object? Value { get; }

    public NavigationParameter(object? value) => Value = value;

    public static NavigationParameter None { get; } = new(value: null);
}

/// <summary>Events raised on every navigation for logging + analytics-free diagnostics.</summary>
public sealed record NavigationEventArgs(PageKey From, PageKey To, NavigationParameter Parameter, bool IsBack);

/// <summary>
/// Typed, VM-first navigation owned by the shell. Deliberately hand-rolled:
/// the page graph is fixed (see WizardStateMachine), so a region framework
/// would be over-engineering (AUDIT-UI ADR-11).
/// </summary>
public interface INavigationService
{
    /// <summary>Current page key. Starts at <see cref="PageKey.Home"/>.</summary>
    PageKey Current { get; }

    /// <summary>Raised after the current page changed.</summary>
    event EventHandler<NavigationEventArgs>? Navigated;

    /// <summary>Navigates forward to a page (or re-activates it). Returns false + reason if refused.</summary>
    bool TryNavigate(PageKey target, NavigationParameter? parameter = null, string? reasonOverride = null);

    /// <summary>Navigates back in the local history stack, if possible.</summary>
    bool TryGoBack();

    /// <summary>Whether the history stack allows going back.</summary>
    bool CanGoBack { get; }

    /// <summary>Clears history and returns to Home (used by "new scan" / Finish).</summary>
    void ResetToHome();
}
