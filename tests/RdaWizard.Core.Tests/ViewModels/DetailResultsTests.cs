using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using RdaWizard.Core.Models;
using RdaWizard.Core.Services;
using RdaWizard.Core.ViewModels;
using Xunit;

namespace RdaWizard.Core.Tests.ViewModels;

/// <summary>T6 results-state behaviors: filter bar presets, contextual CTA,
/// PathPrefix scope, tree rollups, smart selections, preview wrapper.</summary>
public sealed class DetailResultsTests
{
    private static readonly ILoggerFactory Nothing = NullLoggerFactory.Instance;

    private sealed class FakeScanService : IScanService
    {
        public Task<ScanSummary> RunScanAsync(ScanOptions options, IProgress<ScanProgressSnapshot>? progress, CancellationToken cancellationToken)
            => Task.FromResult(new ScanSummary
            {
                Files = 0, Deleted = 0, Carved = 0, LostPartitions = 0,
                BytesScanned = 0, Elapsed = TimeSpan.Zero, SessionPath = null,
            });

        public string? LastTransportLabel => null;
    }

    private sealed class FakeSessions : ISessionService
    {
        public Task<SessionLoadResult> LoadAsync(string sessionPath, CancellationToken cancellationToken = default)
            => Task.FromResult(new SessionLoadResult { Success = false, Session = null, Diagnostic = null });

        public Task<IReadOnlyList<SessionInfo>> ListRecentAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<SessionInfo>>([]);

        public string SessionsDirectory => "/tmp/sessions";
    }

    private sealed class FakeDialogs : IDialogService
    {
        public Task<DialogResult> ConfirmAsync(string title, string message, string confirmLabel, bool destructive = false)
            => Task.FromResult(DialogResult.Confirmed);

        public Task AlertAsync(string title, string message) => Task.CompletedTask;

        public Task<string?> PickFolderAsync(string title) => Task.FromResult((string?)null);

        public Task<string?> PickOpenFileAsync(string title, string filter) => Task.FromResult((string?)null);
    }

    private sealed class FakeClock : IClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UnixEpoch;
        public long TimestampFrequency => 1;
        public long Timestamp => 0;
    }

    private static DetailViewModel Build() => new(
        Nothing.CreateLogger<DetailViewModel>(),
        new FakeScanService(), new FakeSessions(), new FakeDialogs(), new FakeClock());

    private static string? ParentOf(string path)
    {
        var idx = path.LastIndexOfAny(['\\', '/']);
        return idx > 0 ? path[..idx] : null;
    }

    private static string NameOf(string path)
    {
        var idx = path.LastIndexOfAny(['\\', '/']);
        return idx >= 0 && idx < path.Length - 1 ? path[(idx + 1)..] : path;
    }

    private static ResultNode Dir(string path) => new()
    {
        FullPath = path, Name = NameOf(path), LogicalSize = 0, IsDirectory = true,
        Origin = ResultOrigin.Structured, DeletedState = DeletedState.Active, Confidence = ConfidenceGrade.Excellent,
        ParentPath = ParentOf(path),
    };

    private static ResultNode File(string path, long size = 1024, DeletedState state = DeletedState.Active,
        ConfidenceGrade confidence = ConfidenceGrade.Excellent, ResultOrigin origin = ResultOrigin.Structured) => new()
    {
        FullPath = path, Name = NameOf(path), LogicalSize = size, IsDirectory = false,
        Origin = origin, DeletedState = state, Confidence = confidence,
        ParentPath = ParentOf(path),
    };

    private static LoadedSession SessionOf(params ResultNode[] nodes) => new()
    {
        SessionPath = "/tmp/sessions/s.crsf",
        Nodes = nodes,
    };

    [Fact]
    public void RecoverCta_IsContextual_PerOgeBehavior()
    {
        var vm = Build();
        vm.InstallSession(SessionOf(File("a/one.txt"), File("a/two.txt")));
        Assert.Equal("Recover All ...", vm.RecoverCtaLabel);

        vm.ToggleRow(vm.VisibleFiles[0]);
        Assert.Equal("Recover ...", vm.RecoverCtaLabel);

        vm.ClearSelection();
        Assert.Equal("Recover All ...", vm.RecoverCtaLabel);
    }

    [Fact]
    public void FilterCategory_Photos_KeepsOnlyPictures()
    {
        var vm = Build();
        vm.InstallSession(SessionOf(
            File("a/photo.jpg"),
            File("a/song.mp3"),
            File("a/doc.pdf")));
        vm.FilterCategory("Photos");

        Assert.Single(vm.VisibleFiles);
        Assert.Equal("photo.jpg", vm.VisibleFiles[0].Name);
    }

    [Fact]
    public void FilterShow_Deleted_KeepsOnlyDeletedRows()
    {
        var vm = Build();
        vm.InstallSession(SessionOf(
            File("a/alive.txt", state: DeletedState.Active),
            File("a/gone.txt", state: DeletedState.Deleted)));
        vm.FilterShow("deleted");

        Assert.Single(vm.VisibleFiles);
        Assert.Equal("gone.txt", vm.VisibleFiles[0].Name);
    }

    [Fact]
    public void FilterSizeRange_Large_KeepsOnlyBigFiles()
    {
        var vm = Build();
        vm.InstallSession(SessionOf(
            File("a/small.txt", size: 1024),
            File("a/big.bin", size: 1024L * 1024 * 1024)));
        vm.FilterSizeRange("large");

        Assert.Single(vm.VisibleFiles);
        Assert.Equal("big.bin", vm.VisibleFiles[0].Name);
    }

    [Fact]
    public void ClearFilter_ResetsToUnfiltered()
    {
        var vm = Build();
        vm.InstallSession(SessionOf(File("a/photo.jpg"), File("a/song.mp3")));
        vm.FilterCategory("Photos");
        Assert.Single(vm.VisibleFiles);

        vm.ClearFilter();
        Assert.Equal(2, vm.VisibleFiles.Count);
    }

    [Fact]
    public void TreeRoots_RollupCounts_FormatAsOge()
    {
        var vm = Build();
        vm.InstallSession(SessionOf(
            Dir("C:"),
            Dir("C:/photos"),
            File("C:/photos/one.jpg"),
            File("C:/photos/two.jpg"),
            File("C:/note.txt"),
            Dir("C:/lost") with { Origin = ResultOrigin.Lost },
            File("C:/lost/carved.dat", origin: ResultOrigin.Carved)));

        var treeRoot = vm.TreeRoots.Single();
        Assert.Equal(4, treeRoot.FileCount); // 4 files under C:
        Assert.Equal("4", treeRoot.CountText);

        // Icon keys by origin (og root icons)
        Assert.Equal("Icon.Tree.DirectoryIntact", treeRoot.IconKey);
    }

    [Fact]
    public void CountText_FormatsAsOgeCompactUnits()
    {
        var item = new DetailViewModel.TreeNodeItem(Dir("C:")) { FileCount = 1_100_000 };
        Assert.Equal("1.1M", item.CountText);

        item = new DetailViewModel.TreeNodeItem(Dir("C:")) { FileCount = 4_300 };
        Assert.Equal("4.3K", item.CountText);

        item = new DetailViewModel.TreeNodeItem(Dir("C:")) { FileCount = 88 };
        Assert.Equal("88", item.CountText);
    }

    [Fact]
    public void SelectUnrecovered_PicksDeletedRows()
    {
        var vm = Build();
        vm.InstallSession(SessionOf(
            File("a/alive.txt", state: DeletedState.Active),
            File("a/gone.txt", state: DeletedState.Deleted)));
        vm.SelectUnrecovered();

        Assert.Equal(1, vm.SelectedCount);
        Assert.Contains(vm.VisibleFiles.Single(r => r.Name == "gone.txt").Node, vm.SelectedNodes);
    }

    [Fact]
    public void SelectBest_PicksHighConfidenceRows()
    {
        var vm = Build();
        vm.InstallSession(SessionOf(
            File("a/good.txt", confidence: ConfidenceGrade.Excellent),
            File("a/risky.txt", confidence: ConfidenceGrade.PartialRisk)));
        vm.SelectBest();

        Assert.Equal(1, vm.SelectedCount);
        Assert.Equal("good.txt", vm.VisibleFiles.Single(r => r.IsSelected).Name);
    }

    [Fact]
    public void PreviewItem_MapsOgeMetadataRows()
    {
        var vm = Build();
        var node = File("C:/photos/pic.jpg", size: 18_874, state: DeletedState.Deleted,
            confidence: ConfidenceGrade.Excellent);
        vm.Preview(node);

        Assert.NotNull(vm.PreviewItem);
        Assert.False(vm.PreviewItem!.IsFolder);
        Assert.Equal("High", vm.PreviewItem.RecoveryChances);
        Assert.Equal("Deleted", vm.PreviewItem.Status);
        Assert.Equal("pic.jpg", vm.PreviewItem.Name);
    }

    [Fact]
    public void PreviewItem_FolderState_MapsStatusAndChances()
    {
        var vm = Build();
        var folder = Dir("C:/photos") with { DeletedState = DeletedState.Active };
        vm.Preview(folder);

        Assert.NotNull(vm.PreviewItem);
        Assert.True(vm.PreviewItem!.IsFolder);
        Assert.Equal("Existing", vm.PreviewItem.Status);
    }

    [Fact]
    public async Task FolderTarget_ScopesTreeToPathPrefix()
    {
        var vm = Build();
        // Folder target over C: scoped to "photos/"
        await vm.RunScanAsync(new ScanOptions
        {
            Target = new ScanTarget
            {
                Kind = ScanTargetKind.Folder, DiskIndex = 0, VolumeLetter = 'C',
                FolderPath = "C:\\photos", PathPrefix = "photos",
                Description = "photos", SizeBytes = 149_450_000_000,
            },
            Mode = ScanMode.DeepPlusCarve,
            SessionPath = "/tmp/sessions/never.crsf",
            StageLogPath = "/tmp/sessions/never.log",
        }, CancellationToken.None);

        // Engine grammar: volume-relative backslash paths.
        vm.InstallSession(SessionOf(
            Dir("photos"),
            File("photos\\one.jpg"),
            File("other.txt")));

        // Only the photos subtree is visible; the unscoped file is filtered.
        Assert.Single(vm.VisibleFiles);
        Assert.Equal("one.jpg", vm.VisibleFiles[0].Name);
    }
}
