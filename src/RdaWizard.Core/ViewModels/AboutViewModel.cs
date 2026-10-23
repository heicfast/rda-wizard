using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace RdaWizard.Core.ViewModels;

/// <summary>
/// About page (P33): product name + version + open-source licenses.
/// OG parity: the original About carries product/version/OEM branding only.
/// </summary>
public partial class AboutViewModel : ViewModelBase
{
    private readonly ILogger<AboutViewModel> _logger;

    [ObservableProperty] private string _productVersion = ThisAssembly.Version;

    public AboutViewModel(ILogger<AboutViewModel> logger)
    {
        _logger = logger;
    }

    /// <summary>Opens the OSS licenses text (App renders it in a dialog).</summary>
    public static event Action? LicensesRequested;

    /// <summary>Requests the licenses view.</summary>
    [RelayCommand]
    public static void ShowLicenses() => LicensesRequested?.Invoke();
}

internal static class ThisAssembly
{
    public static string Version { get; } =
        typeof(ThisAssembly).Assembly.GetName().Version?.ToString(3) ?? "0.1.0";
}
