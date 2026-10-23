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
/// services (<see cref="AddRdaWizardIntegration"/> in that project).
/// </summary>
public static class DependencyInjection
{
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
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    public long TimestampFrequency => Stopwatch.Frequency;
    public long Timestamp => Stopwatch.GetTimestamp();
}
