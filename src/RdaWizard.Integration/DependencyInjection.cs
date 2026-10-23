using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using RdaWizard.Core.Services;
using RdaWizard.Core.ViewModels;
using RdaWizard.Integration.Cli;
using RdaWizard.Integration.Windows;

namespace RdaWizard.Integration;

/// <summary>
/// Integration-layer DI registrations (engine host + all engine services).
/// Called by the App after <c>AddRdaWizardCore()</c>.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddRdaWizardIntegration(this IServiceCollection services)
    {
        services.TryAddSingleton<IEngineLocator, EngineLocator>();
        services.TryAddSingleton<EngineProcessHost>();
        services.TryAddSingleton<IDeviceEnumerationService, DeviceEnumerationService>();
        services.TryAddSingleton<IScanService, ScanService>();
        services.TryAddSingleton<ISessionService, SessionService>();
        services.TryAddSingleton<IRecoveryService, RecoveryService>();
        services.TryAddSingleton<IHealthService, HealthService>();
        services.TryAddSingleton<IRepairService, RepairService>();
        services.TryAddSingleton<DriverStatusDetector>();
        services.TryAddSingleton<IDriverStatusService, DriverStatusService>();
        return services;
    }
}
