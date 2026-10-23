using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using RdaWizard.Core.Models;
using RdaWizard.Core.Services;
using RdaWizard.Core.ViewModels;
using Xunit;

namespace RdaWizard.Core.Tests.ViewModels;

/// <summary>
/// Detail page (og CDetailPage) scan-state behavior tests: og bottom-bar
/// status text formats, stage-boundary checkpoint refresh (ADR-2
/// browse-while-scanning), row selection, gauge label.
/// </summary>
public sealed class DetailViewModelTests
{
    private static readonly ILoggerFactory Nothing = NullLoggerFactory.Instance;

    private sealed class FakeScanService : IScanService
    {
        public Func<ScanOptions, IProgress<ScanProgressSnapshot>?, CancellationToken, Task<ScanSummary>> Run { get; set; }
            = (_, _, _) => Task.FromResult(new ScanSummary
            {
                Files = 1_139_261, Deleted = 10, Carved = 5, LostPartitions = 0,
                BytesScanned = 147_040_000_000, Elapsed = TimeSpan.FromMinutes(14.7),
                SessionPath = "/tmp/sessions/s.crsf",
            });

        public string? LastTransportLabel => null;

        public Task<ScanSummary> RunScanAsync(ScanOptions options, IProgress<ScanProgressSnapshot>? progress, CancellationToken cancellationToken)
            => Run(options, progress, cancellationToken);
    }

    private sealed class FakeSessionService : ISessionService
    {
        /// <summary>Stubbed by tests: sessionPath → load result.</summary>
        public Func<string, SessionLoadResult> Load { get; set; }
            = _ => new SessionLoadResult { Success = false, Session = null, Diagnostic = "not found" };

        public Task<SessionLoadResult> LoadAsync(string sessionPath, CancellationToken cancellationToken = default)
            => Task.FromResult(Load(sessionPath));

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

    private static ResultNode FileNode(string path, long size = 1024) => new()
    {
        FullPath = path,
        Name = Path.GetFileName(path),
        LogicalSize = size,
        IsDirectory = false,
        Origin = ResultOrigin.Structured,
        DeletedState = DeletedState.Active,
        Confidence = ConfidenceGrade.Excellent,
    };

    private static LoadedSession SessionOf(params ResultNode[] nodes) => new()
    {
        SessionPath = "/tmp/sessions/s.crsf",
        Nodes = nodes,
    };

    private static DetailViewModel Build(FakeScanService? scans = null, FakeSessionService? sessions = null)
        => new(Nothing.CreateLogger<DetailViewModel>(),
            scans ?? new FakeScanService(),
            sessions ?? new FakeSessionService(),
            new FakeDialogs(),
            new FakeClock());

    private static ScanOptions Options(string sessionPath = "/tmp/sessions/s.crsf") => new()
    {
        Target = new ScanTarget
        {
            Kind = ScanTargetKind.Volume, DiskIndex = 0, VolumeLetter = 'C',
            Description = "Windows(C:)", SizeBytes = 149_450_000_000,
        },
        Mode = ScanMode.DeepPlusCarve,
        SessionPath = sessionPath,
        StageLogPath = "/tmp/sessions/s.stage.log",
    };

    [Fact]
    public async Task RunScan_WhileRunning_StatusPrimaryIsPhaseLabelOnly()
    {
        var scans = new FakeScanService
        {
            Run = (_, progress, ct) => Task.Run(async () =>
            {
                progress?.Report(new ScanProgressSnapshot
                {
                    EngineFraction = 0.21,
                    Elapsed = TimeSpan.FromMinutes(1),
                    ReadErrors = Array.Empty<ReadErrorRange>(),
                });
                await Task.Delay(50, ct);
                return new ScanSummary
                {
                    Files = 10, Deleted = 0, Carved = 0, LostPartitions = 0,
                    BytesScanned = 0, Elapsed = TimeSpan.Zero, SessionPath = null,
                };
            }),
        };
        var vm = Build(scans);
        var sessionPath = Path.GetTempFileName();
        var options = Options(sessionPath);

        var run = vm.RunScanAsync(options, CancellationToken.None);
        await Task.Delay(20); // let the progress tick land
        Assert.True(vm.IsScanRunning);
        Assert.Equal("Advanced Scanning", vm.StatusPrimary);
        Assert.StartsWith("Remaining time: ", vm.StatusSecondary);
        await run;
        File.Delete(sessionPath);
    }

    [Fact]
    public async Task RunScan_Completion_StatusUsesSessionTruth()
    {
        var nodes = new[]
        {
            FileNode("a/photo.jpg", 1_000_000_000),
            FileNode("b/video.mp4", 2_000_000_000),
        };
        var sessions = new FakeSessionService
        {
            Load = _ => new SessionLoadResult { Success = true, Session = SessionOf(nodes) },
        };
        var vm = Build(sessions: sessions);
        var sessionPath = Path.GetTempFileName();
        File.WriteAllText(sessionPath, "checkpoint");
        var sessionsDir = Path.GetDirectoryName(sessionPath)!;
        var namedPath = Path.Combine(sessionsDir, "final.crsf");

        // Completion path: shell installs the final session after completion.
        await vm.RunScanAsync(Options(namedPath), CancellationToken.None);
        Assert.Equal(DetailViewModel.ScanPhase.Complete, vm.Phase);
        Assert.Equal("Scan Completed, Files Found: 1,139,261", vm.StatusPrimary); // summary-only (no session yet)

        vm.InstallSession(SessionOf(nodes));
        Assert.Equal("Scan Completed, Files Found: 2 (2.79 GB)", vm.StatusPrimary);
    }

    [Fact]
    public async Task RunScan_StageBoundaryCheckpoint_InstallsPartialResults()
    {
        var partialNodes = new[] { FileNode("a/one.txt"), FileNode("a/two.txt") };
        var sessions = new FakeSessionService
        {
            Load = _ => new SessionLoadResult { Success = true, Session = SessionOf(partialNodes) },
        };
        var sessionPath = Path.Combine(Path.GetTempPath(), "watcher-" + Guid.NewGuid().ToString("N") + ".crsf");
        File.WriteAllText(sessionPath, "stage1"); // checkpoint exists before the watcher's first poll

        var scans = new FakeScanService
        {
            Run = async (_, _, ct) =>
            {
                await Task.Delay(TimeSpan.FromSeconds(5), ct); // long enough for one watcher poll (2s)
                return new ScanSummary
                {
                    Files = 2, Deleted = 0, Carved = 0, LostPartitions = 0,
                    BytesScanned = 0, Elapsed = TimeSpan.Zero, SessionPath = sessionPath,
                };
            },
        };

        var vm = Build(scans, sessions);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await vm.RunScanAsync(Options(sessionPath), cts.Token);

        // Watcher polled once mid-scan and installed the checkpoint tree.
        Assert.True(vm.TotalFileCount == 2, $"expected checkpoint install, got {vm.TotalFileCount}");
        // Scan completed after the checkpoint; the completion line carries
        // session-truth counts (checkpoint install + completion update).
        Assert.Equal("Scan Completed, Files Found: 2 (2.0 KB)", vm.StatusPrimary);
        File.Delete(sessionPath);
    }

    [Fact]
    public async Task RunScan_CancelledWithCheckpoint_KeepsResultsBrowsable()
    {
        // og stop route: a stopped scan keeps its found files browsable —
        // the checkpoint session (G-13) is loaded and the phase reads
        // "Scan Stopped, Files Found: N (size)".
        var nodes = new[] { FileNode("photos\\one.jpg"), FileNode("photos\\two.jpg") };
        var sessionPath = Path.Combine(Path.GetTempPath(), "stopped-" + Guid.NewGuid().ToString("N") + ".crsf");
        File.WriteAllText(sessionPath, "checkpoint");

        var externalCts = new CancellationTokenSource();
        var scans = new FakeScanService
        {
            // Simulate the real stop route: the user cancels the external
            // token, the linked scan token observes it, the engine layer
            // surfaces OCE with that token.
            Run = async (_, _, ct) =>
            {
                externalCts.Cancel();
                await Task.Delay(Timeout.Infinite, ct);
                return new ScanSummary { Files = 0, Deleted = 0, Carved = 0, LostPartitions = 0, BytesScanned = 0, Elapsed = TimeSpan.Zero, SessionPath = null };
            },
        };
        var sessions = new FakeSessionService
        {
            Load = _ => new SessionLoadResult { Success = true, Session = SessionOf(nodes) },
        };
        var vm = Build(scans, sessions);

        ScanSummary? completed = null;
        string? failed = null;
        vm.ScanCompleted += (_, summary) => completed = summary;
        vm.ScanFailed += (_, diagnostic) => failed = diagnostic;

        await vm.RunScanAsync(Options(sessionPath), externalCts.Token);

        Assert.Null(failed);
        Assert.NotNull(completed);
        Assert.Equal(DetailViewModel.ScanPhase.Stopped, vm.Phase);
        Assert.Equal(2, vm.TotalFileCount);
        Assert.Equal("Scan Stopped, Files Found: 2 (2.0 KB)", vm.StatusPrimary);
        Assert.False(vm.IsScanRunning);
        File.Delete(sessionPath);
    }

    [Fact]
    public async Task RunScan_CancelledBeforeAnyStage_RoutesHomeWithTruthfulMessage()
    {
        var sessionPath = Path.Combine(Path.GetTempPath(), "nostop-" + Guid.NewGuid().ToString("N") + ".crsf");
        var externalCts = new CancellationTokenSource();
        var scans = new FakeScanService
        {
            Run = async (_, _, ct) =>
            {
                externalCts.Cancel();
                await Task.Delay(Timeout.Infinite, ct);
                return new ScanSummary { Files = 0, Deleted = 0, Carved = 0, LostPartitions = 0, BytesScanned = 0, Elapsed = TimeSpan.Zero, SessionPath = null };
            },
        };
        var vm = Build(scans); // default session service: load fails

        string? failed = null;
        ScanSummary? completed = null;
        vm.ScanFailed += (_, diagnostic) => failed = diagnostic;
        vm.ScanCompleted += (_, summary) => completed = summary;

        await vm.RunScanAsync(Options(sessionPath), externalCts.Token);

        Assert.Null(completed);
        Assert.NotNull(failed);
        Assert.Contains("no results were saved", failed);
    }

    [Fact]
    public void PercentLabel_RendersOgePercentFormat()
    {
        var vm = Build();
        vm.Percent = 21.4;
        Assert.Equal("21%", vm.PercentLabel);
    }

    [Fact]
    public void ToggleRow_SyncsRowVisualAndSelectionSet()
    {
        var vm = Build();
        var nodes = new[] { FileNode("a/one.txt"), FileNode("a/two.txt") };
        vm.InstallSession(SessionOf(nodes));

        var row = vm.VisibleFiles[0];
        vm.ToggleRow(row);
        Assert.True(row.IsSelected);
        Assert.Equal(1, vm.SelectedCount);
        Assert.Contains(row.Node, vm.SelectedNodes);

        vm.ToggleRow(row);
        Assert.False(row.IsSelected);
        Assert.Equal(0, vm.SelectedCount);
    }

    [Fact]
    public void SelectAllVisible_ThenClearSelection_SyncsRowVisuals()
    {
        var vm = Build();
        var nodes = new[] { FileNode("a/one.txt"), FileNode("a/two.txt") };
        vm.InstallSession(SessionOf(nodes));

        vm.SelectAllVisible();
        Assert.All(vm.VisibleFiles, r => Assert.True(r.IsSelected));
        Assert.Equal(2, vm.SelectedCount);

        vm.ClearSelection();
        Assert.All(vm.VisibleFiles, r => Assert.False(r.IsSelected));
        Assert.Equal(0, vm.SelectedCount);
    }
}
