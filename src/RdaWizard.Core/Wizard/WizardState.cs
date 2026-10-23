namespace RdaWizard.Core.Wizard;

/// <summary>
/// Wizard states — mirroring the OG page-flow states (RE corpus
/// userflow-state-machine): the scan runs ON the Detail page (scan + results
/// are one surface — "browse-while-scanning"), recovery is a modal dialog
/// family. No Configure step: the OG starts scanning immediately from the
/// landing CTA.
/// </summary>
public enum WizardState
{
    /// <summary>Landing (og: PeripheralHomeView) + sidebar destination pages.</summary>
    Select,

    /// <summary>Scan executing on the Detail page (quick → advanced, two-phase).
    /// Results become visible after the quick pass while advanced continues.</summary>
    Scanning,

    /// <summary>Results browsing on the Detail page (session loaded).</summary>
    Results,

    /// <summary>Recovery dialog family in flight (modal).</summary>
    Recovering,
}
