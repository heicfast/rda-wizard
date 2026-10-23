using System.Collections.Immutable;
using Microsoft.Extensions.Logging;

namespace RdaWizard.Core.Navigation;

/// <summary>
/// Navigation service implementation. Forward navigation is constrained by
/// the wizard state machine (a page for state X can only be shown in state
/// X — the shell never shows Results while Scanning, etc.). The state
/// machine itself is owned by <see cref="ViewModels.MainViewModel"/>; this
/// service enforces page-graph legality and keeps a back-stack.
/// </summary>
public sealed class NavigationService : INavigationService
{
    private readonly ILogger<NavigationService> _logger;

    /// <summary>Pages legal per wizard state. Anything else refuses navigation.</summary>
    private static readonly ImmutableDictionary<Wizard.WizardState, ImmutableHashSet<PageKey>> StatePages =
        new Dictionary<Wizard.WizardState, ImmutableHashSet<PageKey>>
        {
            [Wizard.WizardState.Select] = [PageKey.Home, PageKey.Health, PageKey.Repair, PageKey.Settings, PageKey.About],
            [Wizard.WizardState.Configure] = [PageKey.ScanConfig, PageKey.Settings, PageKey.About],
            [Wizard.WizardState.Scanning] = [PageKey.ScanProgress, PageKey.Settings, PageKey.About],
            [Wizard.WizardState.Results] = [PageKey.Results, PageKey.Health, PageKey.Settings, PageKey.About],
            [Wizard.WizardState.Recovering] = [PageKey.Recovery, PageKey.Settings, PageKey.About],
            [Wizard.WizardState.Complete] = [PageKey.Completion, PageKey.Results, PageKey.Settings, PageKey.About],
        }.ToImmutableDictionary();

    private readonly Stack<PageKey> _history = new();
    private PageKey _current = PageKey.Home;
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

        var target = _history.Pop();
        if (!StatePages[_stateGate].Contains(target))
        {
            // The wizard moved on; going back to an outdated page is refused
            // rather than showing a stale surface (truthful UI).
            _logger.LogInformation("Back refused: {Target} illegal in state {State}", target, _stateGate);
            return false;
        }

        var from = _current;
        _current = target;
        _logger.LogInformation("Navigate back: {From} -> {Target}", from, target);
        Navigated?.Invoke(this, new NavigationEventArgs(from, target, NavigationParameter.None, IsBack: true));
        return true;
    }

    public void ResetToHome()
    {
        _history.Clear();
        var from = _current;
        _current = PageKey.Home;
        _stateGate = Wizard.WizardState.Select;
        _logger.LogInformation("Navigation reset to Home (was {From})", from);
        Navigated?.Invoke(this, new NavigationEventArgs(from, PageKey.Home, NavigationParameter.None, IsBack: false));
    }
}
