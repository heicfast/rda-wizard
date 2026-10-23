namespace RdaWizard.Core.Navigation;

/// <summary>
/// Wizard page keys — one per navigable page, mirroring the OG page inventory
/// (RE corpus: wizard QStackedWidget = DrwHomePage/DrwLostDiskPage/
/// PeripheralHomeView/CDetailPage + 5 sidebar destination pages + dialogs).
/// Keys exist only for pages we actually ship (no dead keys).
/// </summary>
public enum PageKey
{
    /// <summary>Landing (PeripheralHomeView, stack idx 2 — the OG default):
    /// device tiles, location trio, "Search for Lost Data" CTA, session banner.</summary>
    Landing,

    /// <summary>Detail page (CDetailPage, stack idx 3): scan progress AND
    /// results on the same surface (browse-while-scanning, two-phase scan).</summary>
    Detail,

    /// <summary>Sidebar destination: SD Card coaching + removable device list.</summary>
    SdCard,

    /// <summary>Sidebar destination: 6 tool cards (Disk Health routes here).</summary>
    MoreTools,

    /// <summary>Sidebar destination: 3 repair options + Start.</summary>
    FileRepair,

    /// <summary>Sidebar destination: 3 lost-partition scenario cards + Start.</summary>
    LostPartition,

    /// <summary>Sidebar destination: ChronoSnap backup coaching page.</summary>
    ChronoSnap,

    /// <summary>Disk health (SMART) — reachable from More Tools → Disk Health.</summary>
    Health,

    /// <summary>Volume repair (RawFixer) — reachable from the File Repair page.</summary>
    Repair,

    /// <summary>Settings — reachable from the hamburger menu.</summary>
    Settings,

    /// <summary>About: product, version, licenses — reachable from the hamburger menu.</summary>
    About,
}

/// <summary>Optional navigation parameters (e.g. the disk a health page was opened for).</summary>
public sealed record NavigationParameter
{
    public object? Value { get; }

    public NavigationParameter(object? value) => Value = value;

    public static NavigationParameter None { get; } = new(value: null);
}

/// <summary>Events raised on every navigation for logging + diagnostics.</summary>
public sealed record NavigationEventArgs(PageKey From, PageKey To, NavigationParameter Parameter, bool IsBack);

/// <summary>
/// Typed, VM-first navigation owned by the shell (ADR-11: hand-rolled — the
/// page graph is fixed). Legality per wizard state is enforced by
/// <see cref="NavigationService"/>.
/// </summary>
public interface INavigationService
{
    /// <summary>Current page key. Starts at <see cref="PageKey.Landing"/>.</summary>
    PageKey Current { get; }

    /// <summary>Raised after the current page changed.</summary>
    event EventHandler<NavigationEventArgs>? Navigated;

    /// <summary>Navigates forward to a page (or re-activates it). Returns false + reason if refused.</summary>
    bool TryNavigate(PageKey target, NavigationParameter? parameter = null, string? reasonOverride = null);

    /// <summary>Navigates back in the local history stack, if possible.</summary>
    bool TryGoBack();

    /// <summary>Whether the history stack allows going back.</summary>
    bool CanGoBack { get; }

    /// <summary>Clears history and returns to the Landing page (new scan / Finish).</summary>
    void ResetToLanding();
}
