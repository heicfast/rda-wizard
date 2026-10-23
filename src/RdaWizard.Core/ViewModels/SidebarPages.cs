using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace RdaWizard.Core.ViewModels;

/// <summary>
/// Base for the og sidebar destination pages (PG-20..PG-24): coaching pages
/// reached from the 6-entry sidebar. Each page is a marketing/education
/// surface with a primary CTA that routes into a real flow.
/// </summary>
public abstract partial class SidebarPageViewModel : ViewModelBase
{
    [ObservableProperty] private string _title = string.Empty;
    [ObservableProperty] private string _subtitle = string.Empty;
}

/// <summary>
/// SD Card page (PG-20): "Connect Your Memory Card to Recover Lost Photos and
/// Videos" coaching + removable-device list. The og shows 3 feature tiles
/// (Fix SD card access / Photos that open / Video that play) + "Show all
/// external devices" + "Get Help" links.
/// </summary>
public partial class SdCardViewModel : SidebarPageViewModel
{
    private readonly ILogger<SdCardViewModel> _logger;

    public SdCardViewModel(ILogger<SdCardViewModel> logger)
    {
        _logger = logger;
        Title = "Connect Your Memory Card to Recover Lost Photos & Videos";
        Subtitle = "Show all external devices";
    }

    /// <summary>"Show all external devices" — returns to the landing device list.</summary>
    [RelayCommand]
    public static void ShowAllDevices()
    {
        // Navigation handled by the shell (sidebar Hardware Disk entry).
    }
}

/// <summary>
/// More Recovery Tools page (PG-21): 6 tool cards — Crashed PC Rescue · NAS
/// and Linux · RAID Recovery · Cloud Recovery · Image Recovery · Disk Health.
/// Disk Health routes to the Health page (engine `health` command); the other
/// cards render at og parity with honest boundary routing (scope: this build
/// recovers local disks).
/// </summary>
public partial class MoreToolsViewModel : SidebarPageViewModel
{
    public sealed record ToolCard(string Title, string Description, string IconKey, bool IsAvailableInThisBuild);

    public IReadOnlyList<ToolCard> Tools { get; } =
    [
        new("Crashed PC Rescue", "Recover data from a PC that fails to start", "Tool.CrashedPc", IsAvailableInThisBuild: false),
        new("NAS and Linux", "Recover data from NAS servers and Linux systems", "Tool.Nas", IsAvailableInThisBuild: false),
        new("RAID Recovery", "Reconstruct data from RAID arrays", "Tool.Raid", IsAvailableInThisBuild: false),
        new("Cloud Recovery", "Recover data from cloud drives", "Tool.Cloud", IsAvailableInThisBuild: false),
        new("Image Recovery", "Recover data from a disk image file", "Tool.Image", IsAvailableInThisBuild: false),
        new("Disk Health", "Check disk health and predict failures", "Tool.DiskHealth", IsAvailableInThisBuild: true),
    ];

    /// <summary>Raised when a tool card is activated (the shell routes per-card).</summary>
    public event EventHandler<ToolCard>? ToolActivated;

    [RelayCommand]
    public void ActivateTool(ToolCard tool)
    {
        ToolActivated?.Invoke(this, tool);
    }
}

/// <summary>
/// File Repair page (PG-22): 3 repair options — "Repair corrupted or
/// unplayable videos" / "Repair damaged or partially missing photos" /
/// "Repair damaged or cannot be opened documents" + Start CTA. This build's
/// engine provides volume-level repair (RawFixer) — the volume-repair flow is
/// reachable from here; per-file media repair is out of scope for the engine.
/// </summary>
public partial class FileRepairPageViewModel : SidebarPageViewModel
{
    public sealed record RepairOption(string Title, string Description, string IconKey);

    public IReadOnlyList<RepairOption> Options { get; } =
    [
        new("Repair corrupted or unplayable videos", "Fix video files that fail to play", "Repair.Video"),
        new("Repair damaged or partially missing photos", "Fix photos with corrupted data", "Repair.Photo"),
        new("Repair damaged or cannot be opened documents", "Fix documents that fail to open", "Repair.Document"),
    ];

    /// <summary>Raised when the user activates volume repair (this build's repair flow).</summary>
    public event EventHandler? VolumeRepairRequested;

    [RelayCommand]
    public void Start()
    {
        VolumeRepairRequested?.Invoke(this, EventArgs.Empty);
    }
}

/// <summary>
/// Lost Partition Rescue page (PG-23): 3 scenario cards — Partition Missing /
/// Partition Cannot Be Opened / BitLocker Partition Missing-Damaged + Start
/// CTA. Start routes to the whole-disk deep scan (lost partitions surface as
/// [lost] roots in the results tree).
/// </summary>
public partial class LostPartitionViewModel : SidebarPageViewModel
{
    public sealed record ScenarioCard(string Title, string Description, string IconKey);

    public IReadOnlyList<ScenarioCard> Scenarios { get; } =
    [
        new("Partition Missing", "The partition was deleted or lost from the disk", "Partition.Missing"),
        new("Partition Cannot Be Opened", "The partition exists but its file system is damaged", "Partition.Damaged"),
        new("BitLocker Partition Missing/Damaged", "An encrypted partition is missing or damaged", "Partition.Bitlocker"),
    ];

    /// <summary>Raised when the user presses Start (the shell opens the whole-disk scan flow).</summary>
    public event EventHandler? StartRequested;

    [RelayCommand]
    public void Start()
    {
        StartRequested?.Invoke(this, EventArgs.Empty);
    }
}

/// <summary>
/// ChronoSnap Backup page (PG-24): "Time Machine for Windows" coaching —
/// Automatic Protection / Point-in-Time Recovery / Powerful and Independent
/// feature cards + Start CTA. Backup is out of scope for this build (this
/// page renders at og visual parity as a destination surface).
/// </summary>
public partial class ChronoSnapViewModel : SidebarPageViewModel
{
    public sealed record FeatureCard(string Title, string Description, string IconKey);

    public IReadOnlyList<FeatureCard> Features { get; } =
    [
        new("Automatic Protection", "Snapshots are captured automatically as you work", "Snap.Auto"),
        new("Point-in-Time Recovery", "Go back to any point in time to find your files", "Snap.TimeTravel"),
        new("Powerful and Independent", "Works independently of Windows file history", "Snap.Independent"),
    ];

    public ChronoSnapViewModel()
    {
        Title = "Time Machine for Windows";
    }
}
