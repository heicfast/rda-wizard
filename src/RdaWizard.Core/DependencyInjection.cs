using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using RdaWizard.Core.Navigation;
using RdaWizard.Core.Services;
using RdaWizard.Core.Settings;
using RdaWizard.Core.ViewModels;

namespace RdaWizard.Core;

/// <summary>
/// Core DI registrations. The App layer calls <see cref="AddRdaWizardCore"/>
/// inside its Generic Host setup; the Integration layer adds the engine
/// services in its own project (RdaWizard.Integration.DependencyInjection).
/// </summary>
public static class DependencyInjection
{
    /// <summary>Registers navigation, settings, localization, clock, and all page ViewModels.</summary>
    public static IServiceCollection AddRdaWizardCore(this IServiceCollection services)
    {
        // Infrastructure
        services.TryAddSingleton<IClock, SystemClock>();
        services.TryAddSingleton<ISettingsService, SettingsService>();
        services.TryAddSingleton<Localization.ILocalizationService, Localization.LocalizationService>();

        // Navigation + shell
        services.TryAddSingleton<NavigationService>();
        services.TryAddSingleton<INavigationService>(static sp => sp.GetRequiredService<NavigationService>());

        // Page ViewModels (shell owns them; transient aux dialogs come later)
        services.TryAddSingleton<HomeViewModel>();
        services.TryAddSingleton<ScanConfigViewModel>();
        services.TryAddSingleton<ScanProgressViewModel>();
        services.TryAddSingleton<ResultsViewModel>();
        services.TryAddSingleton<RecoveryViewModel>();
        services.TryAddSingleton<CompletionViewModel>();
        services.TryAddSingleton<HealthViewModel>();
        services.TryAddSingleton<RepairViewModel>();
        services.TryAddSingleton<SettingsViewModel>();
        services.TryAddSingleton<AboutViewModel>();
        services.TryAddSingleton<MainViewModel>();

        return services;
    }
}

/// <summary>Real clock (tests substitute their own IClock).</summary>
public sealed class SystemClock : IClock
{
    /// <summary>Current UTC time.</summary>
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;

    /// <summary>Stopwatch timestamp frequency (ticks per second).</summary>
    public long TimestampFrequency => Stopwatch.Frequency;

    /// <summary>Current high-resolution timestamp.</summary>
    public long Timestamp => Stopwatch.GetTimestamp();
}
