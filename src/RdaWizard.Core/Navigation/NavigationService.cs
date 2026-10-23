using System.Collections.Immutable;
using Microsoft.Extensions.Logging;

namespace RdaWizard.Core.Navigation;

/// <summary>
/// Navigation service implementation. Forward navigation is constrained by
/// the wizard state machine. The OG model: sidebar destination pages are
/// always reachable (the scan runs app-level in the background); the Detail
/// page (scan + results) is reachable while a scan runs or a session is
/// loaded; navigation is frozen while a recovery is in flight (modal).
/// </summary>
public sealed class NavigationService : INavigationService
{
    private readonly ILogger<NavigationService> _logger;

    /// <summary>Sidebar destination pages — legal in every non-modal state.</summary>
    private static readonly ImmutableHashSet<PageKey> SidebarPages =
        [PageKey.Landing, PageKey.SdCard, PageKey.MoreTools, PageKey.FileRepair,
         PageKey.LostPartition, PageKey.ChronoSnap, PageKey.Settings, PageKey.About];

    /// <summary>Pages legal per wizard state.</summary>
    private static readonly ImmutableDictionary<Wizard.WizardState, ImmutableHashSet<PageKey>> StatePages =
        new Dictionary<Wizard.WizardState, ImmutableHashSet<PageKey>>
        {
            [Wizard.WizardState.Select] = [.. SidebarPages, PageKey.Repair, PageKey.Health],
            [Wizard.WizardState.Scanning] = [.. SidebarPages, PageKey.Detail],
            [Wizard.WizardState.Results] = [.. SidebarPages, PageKey.Detail, PageKey.Repair, PageKey.Health],
            [Wizard.WizardState.Recovering] = [PageKey.Detail],
        }.ToImmutableDictionary();

    private readonly Stack<PageKey> _history = new();
    private PageKey _current = PageKey.Landing;
    private Wizard.WizardState _stateGate = Wizard.WizardState.Select;

    public NavigationService(ILogger<NavigationService> logger) => _logger = logger;

    /// <summary>
    /// The shell updates this whenever the wizard state changes; navigation
    /// to pages illegal in the current state is refused with a reason.
    /// </summary>
    public void SetStateGate(Wizard.WizardState state)
    {
        if (_stateGate == state) return;
        _stateGate = state;
        _logger.LogDebug("Navigation state gate -> {State}", state);
    }

    public PageKey Current => _current;

    public event EventHandler<NavigationEventArgs>? Navigated;

    public bool CanGoBack => _history.Count > 0;

    public bool TryNavigate(PageKey target, NavigationParameter? parameter = null, string? reasonOverride = null)
    {
        parameter ??= NavigationParameter.None;

        // Legality first: the state gate wins even over re-activation of the
        // current page (the shell must never re-show a page the wizard state
        // has invalidated).
        if (!StatePages[_stateGate].Contains(target))
        {
            var reason = reasonOverride ?? $"The {target} page is not available during the current step ({_stateGate}).";
            _logger.LogInformation("Navigation refused: {From} -> {Target} in state {State}: {Reason}",
                _current, target, _stateGate, reason);
            return false;
        }

        if (target == _current)
        {
            _logger.LogDebug("Re-activating current page {Page}", target);
            Navigated?.Invoke(this, new NavigationEventArgs(_current, target, parameter, IsBack: false));
            return true;
        }

        _history.Push(_current);
        var from = _current;
        _current = target;
        _logger.LogInformation("Navigate: {From} -> {Target} (state {State})", from, target, _stateGate);
        Navigated?.Invoke(this, new NavigationEventArgs(from, target, parameter, IsBack: false));
        return true;
    }

    public bool TryGoBack()
    {
        if (_history.Count == 0)
        {
            _logger.LogDebug("Back refused: history empty");
            return false;
        }

        // Pop first; if the popped page is illegal in the current state the
        // entry is dropped and back is refused (the wizard moved on).
        while (_history.Count > 0)
        {
            var target = _history.Pop();
            if (StatePages[_stateGate].Contains(target))
            {
                var from = _current;
                _current = target;
                _logger.LogInformation("Navigate back: {From} -> {Target}", from, target);
                Navigated?.Invoke(this, new NavigationEventArgs(from, target, NavigationParameter.None, IsBack: true));
                return true;
            }

            _logger.LogInformation("Back target {Target} illegal in state {State}; dropping history entry", target, _stateGate);
        }

        _logger.LogInformation("Back refused: no legal history entries in state {State}", _stateGate);
        return false;
    }

    public void ResetToLanding()
    {
        _history.Clear();
        var from = _current;
        _current = PageKey.Landing;
        _logger.LogInformation("Navigation reset to Landing (was {From})", from);
        Navigated?.Invoke(this, new NavigationEventArgs(from, PageKey.Landing, NavigationParameter.None, IsBack: false));
    }
}
