namespace RdaWizard.Core.Wizard;

/// <summary>
/// The wizard transition table, mirroring the OG user flow (RE corpus
/// userflow-state-machine + dynamic ground truth):
/// <code>
///            ┌──────── session import (.crsf) ──────────┐
///            ▼                                         │
///  Select ──► Scanning ──► Results ──► Recovering ──► Results
///    ▲          │  ▲          │  ▲         │  ▲           (result dialog)
///    │          │  └─ quick pass done (two-phase: results visible,
///    │          │     advanced continues — state stays Scanning)  │
///    │          │                │        │ cancel     │
///    │          └── stop ─────────┘        └────────────┘
///    └──────────── new scan / finish ──────────────────┘
/// </code>
/// The OG has no scan-configuration step: "Search for Lost Data" starts the
/// scan immediately on the Detail page (two-phase: quick pass → advanced).
/// Recovery is a modal dialog family launched from the Detail page.
/// </summary>
public sealed class WizardStateMachine
{
    /// <summary>Guard receives the current context; returns null to allow, or a refusal reason.</summary>
    public delegate string? Guard(WizardContext context);

    private sealed record Edge(WizardState From, WizardState To, string Name, Guard? Guard);

    private readonly List<Edge> _edges = new()
    {
        // "Search for Lost Data" CTA → scan starts immediately (OG scanClickedSlot).
        new(WizardState.Select, WizardState.Scanning, "StartScan", static c =>
            c.HasScanTarget ? null : "Select a location to search for lost data first."),

        // Landing "Continue last scanning" (P41) / explicit session import.
        new(WizardState.Select, WizardState.Results, "ImportSession", static c =>
            c.HasLoadedSession ? null : "Load a scan session (.crsf) first."),

        // Stop-scan confirmed (OG CScanCommonPopDlg "Stop Searching for Lost Data?").
        new(WizardState.Scanning, WizardState.Select, "StopScan", static c =>
            c.CancelConfirmed ? null : "Confirm the stop: results found so far are discarded."),

        // Advanced scan finished → results (OG celebration → "Go to Recover").
        new(WizardState.Scanning, WizardState.Results, "ScanCompleted", static c =>
            c.HasLoadedSession ? null : "Scan has not produced a session yet."),

        // Cancel from results → landing (the engine saved the session; landing
        // banner offers to continue it).
        new(WizardState.Results, WizardState.Select, "BackToSelect", static _ => null),

        // Recovery funnel launched from the Detail page (OG ChooseDestinationPathDialog).
        new(WizardState.Results, WizardState.Recovering, "StartRecovery", static c =>
        {
            if (!c.HasLoadedSession) return "Load or complete a scan first.";
            if (!c.HasRecoverySelection) return "Select at least one file to recover.";
            if (!c.DestinationOverlapCheckPassed) return "The destination overlaps the source disk — choose another location.";
            return null;
        }),

        new(WizardState.Recovering, WizardState.Results, "RecoveryFinished", static _ => null),

        new(WizardState.Recovering, WizardState.Results, "CancelRecovery", static c =>
            c.CancelConfirmed ? null : "Confirm the cancel: files already written remain in the destination."),
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
        var anyNameMatch = false;
        foreach (var edge in _edges)
        {
            if (!string.Equals(edge.Name, edgeName, StringComparison.Ordinal)) continue;
            anyNameMatch = true;
            if (edge.From != _state) continue;

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
        return anyNameMatch
            ? $"This action is not available right now (state: {_state})."
            : $"Unknown action '{edgeName}'.";
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
