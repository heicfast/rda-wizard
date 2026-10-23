using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using RdaWizard.Core.Models;
using RdaWizard.Core.Services;

namespace RdaWizard.Core.ViewModels;

/// <summary>
/// Disk health panel (og: DiskHealth.exe surface — UI-MAP §19): SMART
/// attribute table, aggregates, NVMe block, SSD heuristic, TRIM state.
/// Report-only (gap G-15); USB-bridge SMART degrades with a reason (G-16).
/// </summary>
public partial class HealthViewModel : ViewModelBase
{
    private readonly ILogger<HealthViewModel> _logger;
    private readonly IHealthService _health;
    private readonly IDialogService _dialogs;

    [ObservableProperty] private int _diskIndex = -1;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private HealthReport? _report;
    [ObservableProperty] private string? _errorDiagnostic;
    [ObservableProperty] private bool _smartUnavailable;
    [ObservableProperty] private bool _needsElevation;

    public HealthViewModel(ILogger<HealthViewModel> logger, IHealthService health, IDialogService dialogs)
    {
        _logger = logger;
        _health = health;
        _dialogs = dialogs;
    }

    /// <summary>Opens the panel for a disk (from Home or Results).</summary>
    public async Task OpenAsync(int diskIndex, CancellationToken cancellationToken)
    {
        DiskIndex = diskIndex;
        IsLoading = true;
        ErrorDiagnostic = null;
        SmartUnavailable = false;
        NeedsElevation = false;
        try
        {
            Report = await _health.GetHealthAsync(diskIndex, cancellationToken).ConfigureAwait(true);
            SmartUnavailable = Report.Attributes.Count == 0 && Report.Nvme is null;
            _logger.LogInformation("Health loaded for disk {Disk}: {Smart} SMART rows, warnings {Warnings}",
                diskIndex, Report.Attributes.Count, Report.Warnings.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Health read failed for disk {Disk}", diskIndex);
            ErrorDiagnostic = "Reading this disk's health failed: " + ex.Message;
            if (ex.Message.Contains("admin", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("cannot open", StringComparison.OrdinalIgnoreCase))
            {
                NeedsElevation = true;
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public Task RefreshAsync(CancellationToken cancellationToken) => OpenAsync(DiskIndex, cancellationToken);
}
