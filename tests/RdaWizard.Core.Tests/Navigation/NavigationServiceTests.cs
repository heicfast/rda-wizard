using Microsoft.Extensions.Logging.Abstractions;
using RdaWizard.Core.Navigation;
using RdaWizard.Core.Wizard;
using Xunit;

namespace RdaWizard.Core.Tests.Navigation;

public class NavigationServiceTests
{
    private static NavigationService Create(WizardState gate = WizardState.Select)
    {
        var service = new NavigationService(NullLogger<NavigationService>.Instance);
        service.SetStateGate(gate);
        return service;
    }

    [Fact]
    public void StartsAtLanding()
    {
        var service = Create();
        Assert.Equal(PageKey.Landing, service.Current);
    }

    [Fact]
    public void Landing_InSelectState_NavigatesToHealth()
    {
        var service = Create(WizardState.Select);
        Assert.True(service.TryNavigate(PageKey.Health));
        Assert.Equal(PageKey.Health, service.Current);
    }

    [Fact]
    public void Detail_IllegalInSelectState_Refused()
    {
        var service = Create(WizardState.Select);
        Assert.False(service.TryNavigate(PageKey.Detail));
        Assert.Equal(PageKey.Landing, service.Current);
    }

    [Fact]
    public void SidebarPages_LegalDuringScanning()
    {
        // The og runs the scan app-level: sidebar destination pages stay
        // reachable while a scan executes.
        var service = Create(WizardState.Scanning);
        Assert.True(service.TryNavigate(PageKey.Landing));
        Assert.True(service.TryNavigate(PageKey.SdCard));
        Assert.True(service.TryNavigate(PageKey.MoreTools));
        Assert.True(service.TryNavigate(PageKey.FileRepair));
        Assert.True(service.TryNavigate(PageKey.LostPartition));
        Assert.True(service.TryNavigate(PageKey.ChronoSnap));
        Assert.True(service.TryNavigate(PageKey.Settings));
        Assert.True(service.TryNavigate(PageKey.About));
    }

    [Fact]
    public void Detail_LegalDuringScanning()
    {
        var service = Create(WizardState.Scanning);
        Assert.True(service.TryNavigate(PageKey.Detail));
    }

    [Fact]
    public void Detail_LegalInResultsState()
    {
        var service = Create(WizardState.Results);
        Assert.True(service.TryNavigate(PageKey.Detail));
    }

    [Fact]
    public void Navigation_FrozenWhileRecovering()
    {
        // The og recovery is a modal family: only the Detail page is legal.
        Assert.False(Create(WizardState.Recovering).TryNavigate(PageKey.Landing));
        Assert.False(Create(WizardState.Recovering).TryNavigate(PageKey.Settings));
        Assert.True(Create(WizardState.Recovering).TryNavigate(PageKey.Detail));
    }

    [Fact]
    public void Health_IllegalWhileScanning_Refused()
    {
        var service = Create(WizardState.Scanning);
        Assert.False(service.TryNavigate(PageKey.Health));
    }

    [Fact]
    public void Back_ReturnsToPreviousPage_WhenStillLegal()
    {
        var service = Create(WizardState.Select);
        service.TryNavigate(PageKey.Health);
        Assert.True(service.TryGoBack());
        Assert.Equal(PageKey.Landing, service.Current);
    }

    [Fact]
    public void Back_DropsIllegalEntries_AndLandsOnFirstLegal()
    {
        // Landing -> Health in Select, then the wizard advances to Scanning:
        // Health is illegal while scanning, but the og allows returning to
        // the landing (scan continues app-level) — back drops the illegal
        // entry and lands on Landing.
        var service = Create(WizardState.Select);
        service.TryNavigate(PageKey.Health);
        service.SetStateGate(WizardState.Scanning);
        Assert.True(service.TryGoBack());
        Assert.Equal(PageKey.Landing, service.Current);
    }

    [Fact]
    public void Back_RefusedWhenAllEntriesIllegal()
    {
        var service = Create(WizardState.Select);
        service.TryNavigate(PageKey.Health);
        service.SetStateGate(WizardState.Recovering);
        Assert.False(service.TryGoBack());
        Assert.Equal(PageKey.Health, service.Current);
    }

    [Fact]
    public void ResetToLanding_ClearsHistory()
    {
        var service = Create(WizardState.Select);
        service.TryNavigate(PageKey.Settings);
        service.ResetToLanding();
        Assert.Equal(PageKey.Landing, service.Current);
        Assert.False(service.CanGoBack);
        // After reset the gate is unchanged by the service (the shell resets
        // it): Settings must still be reachable.
        Assert.True(service.TryNavigate(PageKey.Settings));
    }

    [Fact]
    public void NavigatedEvent_FiresWithFromAndTo()
    {
        var service = Create(WizardState.Select);
        NavigationEventArgs? observed = null;
        service.Navigated += (_, e) => observed = e;

        service.TryNavigate(PageKey.About);
        Assert.NotNull(observed);
        Assert.Equal(PageKey.Landing, observed!.From);
        Assert.Equal(PageKey.About, observed.To);
    }

    [Fact]
    public void ReNavigate_CurrentPage_Reactivates_WithoutDoubleHistory()
    {
        var service = Create(WizardState.Select);
        Assert.True(service.TryNavigate(PageKey.Settings));
        // First navigation pushed one history entry.
        Assert.True(service.CanGoBack);

        Assert.True(service.TryNavigate(PageKey.Settings));
        // Re-navigation adds nothing: exactly one back step remains.
        Assert.True(service.CanGoBack);
        Assert.True(service.TryGoBack());
        Assert.Equal(PageKey.Landing, service.Current);
        Assert.False(service.CanGoBack);
    }
}
