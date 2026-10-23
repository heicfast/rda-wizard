namespace RdaWizard.Core.Wizard;

/// <summary>
/// The wizard transition table. Every legal edge is declared with an
/// optional guard; anything not declared is refused with a reason string so
/// the UI can *explain* unavailability instead of shipping dead buttons
/// (standing rule: truthful degradation).
///
/// Graph (docs/UI-MAP.md §4, clean-room reduction):
/// <code>
///            ┌──────────── session import (.crsf) ────────────┐
///            ▼                                               │
///  Select ──► Configure ──► Scanning ──► Results ──► Recovering ──► Complete
///    ▲          │    ▲          │  │        │  ▲        │           │
///    │          │    └── back ──┘  │        │  └─back───┘           │
///    │          │                  │        │           │ cancel    │
///    │          └────── back ──────┘   cancel│           │ (partial) │
///    └──────────── new scan / finish ─────────┴───────────┴──────────┘
/// </code>
/// Cancel from Scanning returns to Select (results are discarded — core has
/// no save-on-cancel, gap G-07). Cancel from Recovering returns to Results
/// with a partial-recovery report (files already written stay on disk).
/// </summary>
public sealed class WizardStateMachine
{
    /// <summary>Guard receives the current context; returns null to allow, or a refusal reason.</summary>
    public delegate string? Guard(WizardContext context);

    private sealed record Edge(WizardState From, WizardState To, string Name, Guard? Guard);

    private readonly List<Edge> _edges = new()
    {
        new(WizardState.Select, WizardState.Configure, "BeginScanSetup", static c =>
            c.HasScanTarget ? null : "Select a disk, volume, or a saved scan session first."),

        new(WizardState.Select, WizardState.Results, "ImportSession", static c =>
            c.HasLoadedSession ? null : "Load a scan session (.crsf) first."),

        new(WizardState.Configure, WizardState.Select, "BackToSelect", static _ => null),

        new(WizardState.Configure, WizardState.Scanning, "StartScan", static c =>
            c.HasScanTarget ? null : "No scan target selected."),

        new(WizardState.Scanning, WizardState.Results, "ScanCompleted", static c =>
            c.HasLoadedSession ? null : "Scan has not produced a session yet."),

        new(WizardState.Scanning, WizardState.Select, "CancelScan", static c =>
            c.CancelConfirmed ? null : "Confirm the cancel: results found so far are lost (the engine cannot save a partial scan)."),

        new(WizardState.Results, WizardState.Configure, "ChangeScanSetup", static _ => null),

        new(WizardState.Results, WizardState.Select, "BackToSelect", static _ => null),

        new(WizardState.Results, WizardState.Recovering, "StartRecovery", static c =>
        {
            if (!c.HasLoadedSession) return "Load or complete a scan first.";
            if (!c.HasRecoverySelection) return "Select at least one file to recover.";
            if (!c.DestinationOverlapCheckPassed) return "The destination overlaps the source disk — choose another location.";
            return null;
        }),

        new(WizardState.Recovering, WizardState.Complete, "RecoveryFinished", static _ => null),

        new(WizardState.Recovering, WizardState.Results, "CancelRecovery", static c =>
            c.CancelConfirmed ? null : "Confirm the cancel: files already written remain in the destination."),

        new(WizardState.Complete, WizardState.Results, "BackToResults", static _ => null),

        new(WizardState.Complete, WizardState.Select, "Finish", static _ => null),
    };

    private WizardState _state = WizardState.Select;

    /// <summary>Current state. Changed only via <see cref="TryTransition"/>.</summary>
    public WizardState State => _state;

    /// <summary>Raised after a successful transition (from, to, edge name).</summary>
    public event Action<WizardState, WizardState, string>? Transitioned;

    /// <summary>Raised when a transition is refused (edge name, reason).</summary>
    public event Action<string, string>? Refused;

    /// <summary>Resets to <see cref="WizardState.Select"/> (new wizard run).</summary>
    public void Reset() => _state = WizardState.Select;

    /// <summary>Lists the names of edges currently legal (guards satisfied).</summary>
    public IReadOnlyList<string> AvailableTransitions(WizardContext context)
    {
        var names = new List<string>(4);
        foreach (var edge in _edges)
        {
            if (edge.From != _state) continue;
            if (edge.Guard?.Invoke(context) is null) names.Add(edge.Name);
        }
        return names;
    }

    /// <summary>Attempts a named transition. Returns the refusal reason on failure, null on success.</summary>
    public string? TryTransition(string edgeName, WizardContext context)
    {
        foreach (var edge in _edges)
        {
            if (!string.Equals(edge.Name, edgeName, StringComparison.Ordinal)) continue;
            if (edge.From != _state)
                return $"This action is not available right now (state: {_state}).";

            var reason = edge.Guard?.Invoke(context);
            if (reason is not null)
            {
                Refused?.Invoke(edge.Name, reason);
                return reason;
            }

            var from = _state;
            _state = edge.To;
            Transitioned?.Invoke(from, _state, edge.Name);
            return null;
        }
        return $"Unknown action '{edgeName}'.";
    }
}

/// <summary>
/// Everything the guards may need to know. The MainViewModel keeps one of
/// these current; it is the single source of truth for "can the user do X
/// right now" — including the hard destination-overlap gate (ADR-7).
/// </summary>
public sealed record WizardContext
{
    /// <summary>A disk, volume, or lost-partition target is chosen for scanning.</summary>
    public required bool HasScanTarget { get; init; }

    /// <summary>A session (.crsf) is loaded in memory — from a completed scan or an import.</summary>
    public required bool HasLoadedSession { get; init; }

    /// <summary>At least one result row selected for recovery.</summary>
    public required bool HasRecoverySelection { get; init; }

    /// <summary>Destination overlap gate result (hard refusal when false — ADR-7).</summary>
    public required bool DestinationOverlapCheckPassed { get; init; }

    /// <summary>User explicitly confirmed the destructive/behavioral consequences of cancel.</summary>
    public required bool CancelConfirmed { get; init; }

    public static WizardContext Empty { get; } = new()
    {
        HasScanTarget = false,
        HasLoadedSession = false,
        HasRecoverySelection = false,
        DestinationOverlapCheckPassed = false,
        CancelConfirmed = false,
    };
}
