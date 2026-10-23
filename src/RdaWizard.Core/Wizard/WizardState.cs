namespace RdaWizard.Core.Wizard;

/// <summary>
/// Wizard states — the clean-room reconstruction of the DRW page graph
/// (docs/UI-MAP.md §4) reduced to the surfaces we ship: no retention gates,
/// no monetization, no cloud/NAS/RAID chains.
/// The og runs scan *inside* its results page; we keep an explicit Scanning
/// state because our progress page owns truthful telemetry (ADR-4) and a
/// destructive-cancel confirmation (ADR-3).
/// </summary>
public enum WizardState
{
    /// <summary>Home / device selection (og: PeripheralHomeView).</summary>
    Select,

    /// <summary>Scan configuration: mode, filters, advanced read policy.</summary>
    Configure,

    /// <summary>Scan executing (progress surface; cancel is destructive).</summary>
    Scanning,

    /// <summary>Results explorer: tree + list + preview + filters.</summary>
    Results,

    /// <summary>Recovery queue executing (per-file progress, verification).</summary>
    Recovering,

    /// <summary>Completion report (success / partial / failed with reasons).</summary>
    Complete,
}

/// <summary>
/// The three rail phases shown by the WizardStepper (a documented
/// improvement — the og had no stepper; see AUDIT-UI ADR-12).
/// </summary>
public enum WizardPhase
{
    Select,
    Scan,
    Recover,
}

public static class WizardStateExtensions
{
    /// <summary>Maps a state to the stepper rail phase it belongs to.</summary>
    public static WizardPhase Phase(this WizardState state) => state switch
    {
        WizardState.Select or WizardState.Configure => WizardPhase.Select,
        WizardState.Scanning => WizardPhase.Scan,
        WizardState.Results or WizardState.Recovering or WizardState.Complete => WizardPhase.Recover,
        _ => throw new ArgumentOutOfRangeException(nameof(state), state, null),
    };
}
