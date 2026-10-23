using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using RdaWizard.Core.Models;
using RdaWizard.Core.Services;
using RdaWizard.Core.ViewModels;
using Xunit;

namespace RdaWizard.Core.Tests.ViewModels;

/// <summary>
/// Landing page (og PeripheralHomeView) behavior tests: tile grouping,
/// single-selection semantics across tiles and the location trio, folder
/// targets (Recycle Bin / Desktop / picked folder — og PushFolderFilter),
/// refresh survival, and the CTA state machine.
/// </summary>
public sealed class LandingViewModelTests
{
    private static readonly ILoggerFactory Nothing = NullLoggerFactory.Instance;

    private static VolumeInfo Vol(char? letter, string fs = "NTFS", string label = "Windows", long size = 149_450_000_000, long? used = 31_150_000_000, int disk = 0)
        => new()
        {
            Letter = letter,
            Filesystem = fs,
            Label = label,
            SizeBytes = size,
            UsedBytes = used,
            IsBitLocker = false,
            OwningDiskIndex = disk,
        };

    private static DiskInfo FixedDisk(params VolumeInfo[] volumes) => new()
    {
        Index = 0,
        SizeBytes = 500_000_000_000,
        Model = "Samsung SSD 970 EVO 500GB",
        Serial = "S4EX",
        IsRemovable = false,
        Table = PartitionTableKind.Gpt,
        Volumes = volumes,
    };

    private static DiskInfo UsbDisk(char letter) => new()
    {
        Index = 1,
        SizeBytes = 64_000_000_000,
        Model = "USB Drive",
        Serial = "USB1",
        IsRemovable = true,
        Table = PartitionTableKind.Mbr,
        Volumes = [Vol(letter, "FAT32", "TRAVEL", 64_000_000_000, 12_000_000_000, disk: 1)],
    };

    private sealed class FakeEnumeration(IReadOnlyList<DiskInfo> disks) : IDeviceEnumerationService
    {
        public EnumerationResult Result { get; set; } = new() { Success = true, Disks = disks };

        public Task<EnumerationResult> EnumerateAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(Result);
    }

    private sealed class FakeSessions : ISessionService
    {
        public IReadOnlyList<SessionInfo> Recent { get; set; } = [];

        public Task<SessionLoadResult> LoadAsync(string sessionPath, CancellationToken cancellationToken = default)
            => Task.FromResult(new SessionLoadResult { Success = true, Session = null, Diagnostic = null });

        public Task<IReadOnlyList<SessionInfo>> ListRecentAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(Recent);

        public string SessionsDirectory => "/tmp/sessions";
    }

    private sealed class FakeDialogs : IDialogService
    {
        public string? FolderPickResult { get; set; }

        public Task<DialogResult> ConfirmAsync(string title, string message, string confirmLabel, bool destructive = false)
            => Task.FromResult(DialogResult.Confirmed);

        public Task AlertAsync(string title, string message) => Task.CompletedTask;

        public Task<string?> PickFolderAsync(string title) => Task.FromResult(FolderPickResult);

        public Task<string?> PickOpenFileAsync(string title, string filter) => Task.FromResult((string?)null);
    }

    private static LandingViewModel Build(FakeEnumeration? enumeration = null, FakeDialogs? dialogs = null)
        => new(Nothing.CreateLogger<LandingViewModel>(),
            enumeration ?? new FakeEnumeration([FixedDisk(Vol('C'), Vol('D', label: "Data"))]),
            new FakeSessions(),
            dialogs ?? new FakeDialogs());

    [Fact]
    public async Task Refresh_GroupsTilesInternalAndExternal_WithOgeHeaderCounts()
    {
        var vm = Build(new FakeEnumeration([FixedDisk(Vol('C'), Vol('D')), UsbDisk('E')]));

        await vm.RefreshAsync(CancellationToken.None);

        Assert.Equal(2, vm.InternalDrives.Count);
        Assert.Single(vm.ExternalDrives);
        Assert.Equal("Internal drives (2)", vm.InternalHeader);
        Assert.Equal("External drives (1)", vm.ExternalHeader);
        Assert.True(vm.HasDevices);
    }

    [Fact]
    public async Task Refresh_NoDevices_LeavesEmptyGroupsAndSetsNoDevicesState()
    {
        var vm = Build(new FakeEnumeration([]));

        await vm.RefreshAsync(CancellationToken.None);

        Assert.Empty(vm.InternalDrives);
        Assert.Empty(vm.ExternalDrives);
        Assert.False(vm.HasDevices);
        Assert.Null(vm.SelectedTarget);
    }

    [Fact]
    public async Task SelectTile_IsSingleSelection_AndBuildsVolumeTarget()
    {
        var vm = Build();
        await vm.RefreshAsync(CancellationToken.None);

        vm.SelectTile(vm.InternalDrives[0]);
        Assert.True(vm.InternalDrives[0].IsSelected);
        Assert.False(vm.InternalDrives[1].IsSelected);
        Assert.Equal("Windows(C:)", vm.SelectedTarget!.Description);
        Assert.Equal(ScanTargetKind.Volume, vm.SelectedTarget.Kind);
        Assert.Equal('C', vm.SelectedTarget.VolumeLetter);
        Assert.True(vm.HasSelection);

        // Moving selection clears the previous tile.
        vm.SelectTile(vm.InternalDrives[1]);
        Assert.False(vm.InternalDrives[0].IsSelected);
        Assert.True(vm.InternalDrives[1].IsSelected);
        Assert.Equal('D', vm.SelectedTarget!.VolumeLetter);
    }

    [Fact]
    public async Task SelectTrioItem_ClearsTileSelection_AndSelectsTrioVisual()
    {
        var vm = Build();
        await vm.RefreshAsync(CancellationToken.None);

        vm.SelectTile(vm.InternalDrives[0]);
        vm.SelectTrioItemCommand.Execute(vm.LocationTrio[0]); // Recycle Bin

        Assert.False(vm.InternalDrives[0].IsSelected);
        Assert.True(vm.LocationTrio[0].IsSelected);
        Assert.False(vm.LocationTrio[1].IsSelected);
        Assert.Equal(ScanTargetKind.Folder, vm.SelectedTarget!.Kind);
        Assert.Equal("Recycle Bin", vm.SelectedTarget.Description);
        Assert.Equal("$RECYCLE.BIN", vm.SelectedTarget.PathPrefix);
        Assert.Equal('C', vm.SelectedTarget.VolumeLetter);
    }

    [Fact]
    public async Task SelectTile_ClearsTrioSelection()
    {
        var vm = Build();
        await vm.RefreshAsync(CancellationToken.None);

        vm.SelectTrioItemCommand.Execute(vm.LocationTrio[0]); // Recycle Bin
        Assert.True(vm.LocationTrio[0].IsSelected);

        vm.SelectTile(vm.InternalDrives[0]);
        Assert.False(vm.LocationTrio[0].IsSelected);
        Assert.Equal(ScanTargetKind.Volume, vm.SelectedTarget!.Kind);
    }

    [Fact]
    public async Task StartScan_NoSelection_RaisesNothing()
    {
        var vm = Build();
        await vm.RefreshAsync(CancellationToken.None);

        var raised = 0;
        vm.ScanRequested += (_, _) => raised++;
        vm.StartScan();

        Assert.Equal(0, raised);
        Assert.False(vm.HasSelection);
    }

    [Fact]
    public async Task StartScan_WithSelection_RaisesScanRequestedWithTarget()
    {
        var vm = Build();
        await vm.RefreshAsync(CancellationToken.None);
        vm.SelectTile(vm.InternalDrives[0]);

        ScanTarget? delivered = null;
        vm.ScanRequested += (_, target) => delivered = target;
        vm.StartScan();

        Assert.NotNull(delivered);
        Assert.Equal('C', delivered!.VolumeLetter);
    }

    [Fact]
    public async Task StartScan_WhileScanRunning_RaisesReturnToScanInstead()
    {
        var vm = Build();
        await vm.RefreshAsync(CancellationToken.None);
        vm.SelectTile(vm.InternalDrives[0]);

        vm.IsScanRunning = true;
        Assert.Equal("Return to scan", vm.CtaLabel);

        var scanRaised = 0;
        var returnRaised = 0;
        vm.ScanRequested += (_, _) => scanRaised++;
        vm.ReturnToScanRequested += (_, _) => returnRaised++;
        vm.StartScan();

        Assert.Equal(0, scanRaised);
        Assert.Equal(1, returnRaised);
    }

    [Fact]
    public async Task Refresh_SelectionSurvivesWhenVolumeStillExists()
    {
        var enumeration = new FakeEnumeration([FixedDisk(Vol('C'), Vol('D'))]);
        var vm = Build(enumeration);
        await vm.RefreshAsync(CancellationToken.None);
        vm.SelectTile(vm.InternalDrives[1]); // D:

        await vm.RefreshAsync(CancellationToken.None);

        Assert.True(vm.InternalDrives[1].IsSelected);
        Assert.Equal('D', vm.SelectedTarget!.VolumeLetter);
    }

    [Fact]
    public async Task Refresh_SelectionDropsWhenVolumeVanishes()
    {
        var enumeration = new FakeEnumeration([FixedDisk(Vol('C'), Vol('D'))]);
        var vm = Build(enumeration);
        await vm.RefreshAsync(CancellationToken.None);
        vm.SelectTile(vm.InternalDrives[1]); // D:

        enumeration.Result = new EnumerationResult { Success = true, Disks = [FixedDisk(Vol('C'))] };
        await vm.RefreshAsync(CancellationToken.None);

        Assert.Null(vm.SelectedTarget);
        Assert.Null(vm.SelectedTileKey);
        Assert.False(vm.HasSelection);
    }

    [Fact]
    public void CapacityText_MatchesOgeUsedTotalFormat()
    {
        var tile = new DriveTileViewModel
        {
            Volume = Vol('C', used: 31_150_000_000, size: 149_450_000_000),
            IsExternal = false,
        };

        // Windows convention: binary bytes, "GB" label (og tile reads
        // "31.15 GB/149.45 GB" for the runner disk).
        Assert.Equal("29.01 GB/139.19 GB", tile.CapacityText);
        Assert.NotNull(tile.UsedFraction);
    }

    [Fact]
    public void UsedFraction_IsNullWhenOsReportsNoUsedBytes()
    {
        var tile = new DriveTileViewModel
        {
            Volume = Vol('E', used: null),
            IsExternal = true,
        };

        Assert.Null(tile.UsedFraction);
        Assert.Equal("139.19 GB", tile.CapacityText);
    }
}
