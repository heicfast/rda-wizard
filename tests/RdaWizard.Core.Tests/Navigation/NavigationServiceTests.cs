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
    public void StartsAtHome()
    {
        var service = Create();
        Assert.Equal(PageKey.Home, service.Current);
    }

    [Fact]
    public void Home_InSelectState_NavigatesToHealth()
    {
        var service = Create(WizardState.Select);
        Assert.True(service.TryNavigate(PageKey.Health));
        Assert.Equal(PageKey.Health, service.Current);
    }

    [Fact]
    public void Results_IllegalInSelectState_Refused()
    {
        var service = Create(WizardState.Select);
        Assert.False(service.TryNavigate(PageKey.Results));
        Assert.Equal(PageKey.Home, service.Current);
    }

    [Fact]
    public void Home_IllegalDuringScanning_Refused()
    {
        var service = Create(WizardState.Select);
        service.TryNavigate(PageKey.Health); // moves Current off Home
        service.SetStateGate(WizardState.Scanning);
        // Home is illegal in Scanning; the state gate wins over re-activation paths.
        Assert.False(service.TryNavigate(PageKey.Home));
        Assert.Equal(PageKey.Health, service.Current);
    }

    [Fact]
    public void ScanProgress_LegalDuringScanning()
    {
        var service = Create(WizardState.Scanning);
        Assert.True(service.TryNavigate(PageKey.ScanProgress));
    }

    [Fact]
    public void Results_LegalInResultsState()
    {
        var service = Create(WizardState.Results);
        Assert.True(service.TryNavigate(PageKey.Results));
    }

    [Fact]
    public void Recovery_LegalOnlyInRecoveringState()
    {
        Assert.False(Create(WizardState.Results).TryNavigate(PageKey.Recovery));
        Assert.True(Create(WizardState.Recovering).TryNavigate(PageKey.Recovery));
    }

    [Fact]
    public void Back_ReturnsToPreviousPage_WhenStillLegal()
    {
        var service = Create(WizardState.Select);
        service.TryNavigate(PageKey.Health);
        Assert.True(service.TryGoBack());
        Assert.Equal(PageKey.Home, service.Current);
    }

    [Fact]
    public void Back_RefusedWhenTargetIllegalInNewState()
    {
        // Navigate Home -> Health in Select, then the wizard advances: back to
        // Health is illegal from ScanProgress.
        var service = Create(WizardState.Select);
        service.TryNavigate(PageKey.Health);
        service.SetStateGate(WizardState.Scanning);
        Assert.False(service.TryGoBack());
    }

    [Fact]
    public void ResetToHome_ClearsHistory_AndGate()
    {
        var service = Create(WizardState.Select);
        service.TryNavigate(PageKey.Settings);
        service.ResetToHome();
        Assert.Equal(PageKey.Home, service.Current);
        Assert.False(service.CanGoBack);
        // After reset the gate is Select: Settings must still be reachable.
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
        Assert.Equal(PageKey.Home, observed!.From);
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
        Assert.Equal(PageKey.Home, service.Current);
        Assert.False(service.CanGoBack);
    }
}
