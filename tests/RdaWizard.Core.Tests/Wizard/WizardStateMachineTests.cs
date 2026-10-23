using RdaWizard.Core.Wizard;
using Xunit;

namespace RdaWizard.Core.Tests.Wizard;

public class WizardStateMachineTests
{
    private static WizardContext Context(
        bool target = false,
        bool session = false,
        bool selection = false,
        bool overlapPassed = false,
        bool cancelConfirmed = false) => new()
        {
            HasScanTarget = target,
            HasLoadedSession = session,
            HasRecoverySelection = selection,
            DestinationOverlapCheckPassed = overlapPassed,
            CancelConfirmed = cancelConfirmed,
        };

    [Fact]
    public void StartsInSelectState()
    {
        var machine = new WizardStateMachine();
        Assert.Equal(WizardState.Select, machine.State);
    }

    [Fact]
    public void HappyPath_Select_Scanning_Results_Recovering_Results()
    {
        // The og flow: the landing CTA starts the scan immediately (no
        // Configure step); completion lands in Results; recovery is a modal
        // that returns to Results.
        var machine = new WizardStateMachine();

        Assert.Null(machine.TryTransition("StartScan", Context(target: true)));
        Assert.Equal(WizardState.Scanning, machine.State);

        Assert.Null(machine.TryTransition("ScanCompleted", Context(session: true)));
        Assert.Equal(WizardState.Results, machine.State);

        Assert.Null(machine.TryTransition("StartRecovery", Context(session: true, selection: true, overlapPassed: true)));
        Assert.Equal(WizardState.Recovering, machine.State);

        Assert.Null(machine.TryTransition("RecoveryFinished", Context()));
        Assert.Equal(WizardState.Results, machine.State);
    }

    [Fact]
    public void StartScan_RefusedWithoutTarget_WithReason()
    {
        var machine = new WizardStateMachine();
        var reason = machine.TryTransition("StartScan", Context(target: false));

        Assert.NotNull(reason);
        Assert.Contains("Select a location", reason, StringComparison.Ordinal);
        Assert.Equal(WizardState.Select, machine.State);
    }

    [Fact]
    public void StartRecovery_RefusedWithoutSelection()
    {
        var machine = new WizardStateMachine();
        machine.TryTransition("StartScan", Context(target: true));
        machine.TryTransition("ScanCompleted", Context(session: true));

        var reason = machine.TryTransition("StartRecovery", Context(session: true, selection: false));
        Assert.NotNull(reason);
        Assert.Contains("Select at least one file", reason, StringComparison.Ordinal);
        Assert.Equal(WizardState.Results, machine.State);
    }

    [Fact]
    public void StartRecovery_HardRefused_WhenDestinationOverlapsSource()
    {
        var machine = new WizardStateMachine();
        machine.TryTransition("StartScan", Context(target: true));
        machine.TryTransition("ScanCompleted", Context(session: true));

        var reason = machine.TryTransition("StartRecovery",
            Context(session: true, selection: true, overlapPassed: false));
        Assert.NotNull(reason);
        Assert.Contains("overlaps the source", reason, StringComparison.Ordinal);
        Assert.Equal(WizardState.Results, machine.State);
    }

    [Fact]
    public void StopScan_RefusedUntilConfirmed()
    {
        var machine = new WizardStateMachine();
        machine.TryTransition("StartScan", Context(target: true));

        var reason = machine.TryTransition("StopScan", Context(cancelConfirmed: false));
        Assert.NotNull(reason);
        Assert.Contains("Confirm the stop", reason, StringComparison.Ordinal);
        Assert.Equal(WizardState.Scanning, machine.State);

        Assert.Null(machine.TryTransition("StopScan", Context(cancelConfirmed: true)));
        Assert.Equal(WizardState.Select, machine.State);
    }

    [Fact]
    public void ScanCompleted_RefusedWithoutSession()
    {
        var machine = new WizardStateMachine();
        machine.TryTransition("StartScan", Context(target: true));

        var reason = machine.TryTransition("ScanCompleted", Context(session: false));
        Assert.NotNull(reason);
        Assert.Equal(WizardState.Scanning, machine.State);
    }

    [Fact]
    public void ImportSession_JumpsFromSelectToResults()
    {
        var machine = new WizardStateMachine();
        Assert.Null(machine.TryTransition("ImportSession", Context(session: true)));
        Assert.Equal(WizardState.Results, machine.State);
    }

    [Fact]
    public void UnknownTransition_ReturnsReason()
    {
        var machine = new WizardStateMachine();
        var reason = machine.TryTransition("DoesNotExist", Context());
        Assert.NotNull(reason);
        Assert.Contains("Unknown action", reason, StringComparison.Ordinal);
    }

    [Fact]
    public void WrongStateTransition_ReturnsReason()
    {
        var machine = new WizardStateMachine();
        // RecoveryFinished from Select is illegal.
        var reason = machine.TryTransition("RecoveryFinished", Context());
        Assert.NotNull(reason);
        Assert.Contains("not available right now", reason, StringComparison.Ordinal);
    }

    [Fact]
    public void BackToSelect_FromResults()
    {
        var machine = new WizardStateMachine();
        machine.TryTransition("StartScan", Context(target: true));
        machine.TryTransition("ScanCompleted", Context(session: true));

        Assert.Null(machine.TryTransition("BackToSelect", Context()));
        Assert.Equal(WizardState.Select, machine.State);
    }

    [Fact]
    public void CancelRecovery_ReturnsToResults_OnlyWhenConfirmed()
    {
        var machine = new WizardStateMachine();
        machine.TryTransition("StartScan", Context(target: true));
        machine.TryTransition("ScanCompleted", Context(session: true));
        machine.TryTransition("StartRecovery", Context(session: true, selection: true, overlapPassed: true));

        var reason = machine.TryTransition("CancelRecovery", Context(cancelConfirmed: false));
        Assert.NotNull(reason);

        Assert.Null(machine.TryTransition("CancelRecovery", Context(cancelConfirmed: true)));
        Assert.Equal(WizardState.Results, machine.State);
    }

    [Fact]
    public void AvailableTransitions_ReflectGuards()
    {
        var machine = new WizardStateMachine();
        var withoutTarget = machine.AvailableTransitions(Context());
        Assert.DoesNotContain("StartScan", withoutTarget);

        var withTarget = machine.AvailableTransitions(Context(target: true));
        Assert.Contains("StartScan", withTarget);
        Assert.DoesNotContain("ImportSession", withTarget);
    }

    [Fact]
    public void Reset_ReturnsToSelect()
    {
        var machine = new WizardStateMachine();
        machine.TryTransition("StartScan", Context(target: true));
        machine.Reset();
        Assert.Equal(WizardState.Select, machine.State);
    }
}
