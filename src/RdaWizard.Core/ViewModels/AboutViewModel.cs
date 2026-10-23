using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using RdaWizard.Core.Models;
using RdaWizard.Core.Services;

namespace RdaWizard.Core.ViewModels;

/// <summary>
/// About page (H): product version, driver status (loaded/wire version/
/// direct mode), engine version (rrecov --version equivalent — resolved by
/// the integration layer), open-source licenses view.
/// </summary>
public partial class AboutViewModel : ViewModelBase
{
    private readonly ILogger<AboutViewModel> _logger;
    private readonly IDriverStatusService _driverStatus;
    private readonly IEngineInfoService _engineInfo;

    [ObservableProperty] private string _productVersion = ThisAssembly.Version;
    [ObservableProperty] private DriverStatus? _driver;
    [ObservableProperty] private EngineInfo? _engine;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _licensesText;

    public AboutViewModel(ILogger<AboutViewModel> logger, IDriverStatusService driverStatus, IEngineInfoService engineInfo)
    {
        _logger = logger;
        _driverStatus = driverStatus;
        _engineInfo = engineInfo;
    }

    [RelayCommand]
    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        IsLoading = true;
        try
        {
            Driver = await _driverStatus.ProbeAsync(cancellationToken).ConfigureAwait(true);
            Engine = await _engineInfo.GetEngineInfoAsync(cancellationToken).ConfigureAwait(true);
            _logger.LogInformation("About loaded: driver={Driver} engine={Engine}", Driver?.State, Engine?.Version ?? "unknown");
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>Opens the OSS licenses text (App renders it in a dialog).</summary>
    public static event Action? LicensesRequested;

    /// <summary>Requests the licenses view.</summary>
    [RelayCommand]
    public static void ShowLicenses() => LicensesRequested?.Invoke();
}

/// <summary>Engine (rrecov) presence + version info.</summary>
public sealed record EngineInfo
{
    public required bool Available { get; init; }
    public required string Version { get; init; }
    public required string ExecutablePath { get; init; }
}

/// <summary>Resolves engine presence/version for the About page + preflights.</summary>
public interface IEngineInfoService
{
    Task<EngineInfo> GetEngineInfoAsync(CancellationToken cancellationToken = default);
}

internal static class ThisAssembly
{
    public static string Version { get; } =
        typeof(ThisAssembly).Assembly.GetName().Version?.ToString(3) ?? "0.1.0";
}
