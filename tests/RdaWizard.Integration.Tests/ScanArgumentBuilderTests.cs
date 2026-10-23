using RdaWizard.Core.Models;
using RdaWizard.Integration;
using Xunit;

namespace RdaWizard.Integration.Tests;

/// <summary>
/// The CLI flag mapping is the parity surface: every wizard scan must be
/// reproducible as one rrecov command line (frozen contract, CORE-CONTRACT §13).
/// </summary>
public class ScanArgumentBuilderTests
{
    private static ScanOptions Options(ScanTargetKind kind, ScanMode mode, int threads = 0,
        string? filter = null, bool deletedOnly = false) => new()
        {
            Target = new ScanTarget
            {
                Kind = kind,
                DiskIndex = 3,
                VolumeLetter = kind == ScanTargetKind.Volume ? 'E' : null,
                Description = "test target",
                SizeBytes = 123,
            },
            Mode = mode,
            Threads = threads,
            NameFilter = filter,
            DeletedOnly = deletedOnly,
            SessionPath = @"C:\s\out.crsf",
            StageLogPath = @"C:\s\stage.log",
        };

    private static IReadOnlyList<string> Args(ScanTargetKind kind, ScanMode mode, int threads = 0,
        string? filter = null, bool deletedOnly = false)
        => ScanService.BuildArguments(Options(kind, mode, threads, filter, deletedOnly));

    [Fact]
    public void WholeDisk_DeepPlusCarve_DefaultModel()
    {
        var args = Args(ScanTargetKind.WholeDisk, ScanMode.DeepPlusCarve);
        Assert.Equal(
            ["scan", "--disk", "3", "--deep", "--out", @"C:\s\out.crsf", "--log", @"C:\s\stage.log", "--json"],
            args);
    }

    [Fact]
    public void WholeDisk_Quick()
    {
        var args = Args(ScanTargetKind.WholeDisk, ScanMode.Quick);
        Assert.Equal(
            ["scan", "--disk", "3", "--quick-only", "--out", @"C:\s\out.crsf", "--log", @"C:\s\stage.log", "--json"],
            args);
    }

    [Fact]
    public void WholeDisk_Deep_NoCarve()
    {
        var args = Args(ScanTargetKind.WholeDisk, ScanMode.Deep);
        Assert.Equal(
            ["scan", "--disk", "3", "--no-carve", "--deep", "--out", @"C:\s\out.crsf", "--log", @"C:\s\stage.log", "--json"],
            args);
    }

    [Fact]
    public void Volume_Deep_NoDeepFlag()
    {
        // --deep is a whole-disk-view concept; the engine notes it is skipped
        // on volume targets, so the wizard does not send it.
        var args = Args(ScanTargetKind.Volume, ScanMode.Deep);
        Assert.Equal(
            ["scan", "--volume", "E:", "--no-carve", "--out", @"C:\s\out.crsf", "--log", @"C:\s\stage.log", "--json"],
            args);
    }

    [Fact]
    public void Volume_Quick()
    {
        var args = Args(ScanTargetKind.Volume, ScanMode.Quick);
        Assert.Equal(
            ["scan", "--volume", "E:", "--quick-only", "--out", @"C:\s\out.crsf", "--log", @"C:\s\stage.log", "--json"],
            args);
    }

    [Fact]
    public void Threads_Filter_DeletedOnly_AllApplied()
    {
        var args = Args(ScanTargetKind.WholeDisk, ScanMode.DeepPlusCarve, threads: 16, filter: "report", deletedOnly: true);
        Assert.Equal(
            ["scan", "--disk", "3", "--deep", "--threads", "16", "--filter", "report", "--deleted-only",
             "--out", @"C:\s\out.crsf", "--log", @"C:\s\stage.log", "--json"],
            args);
    }

    [Fact]
    public void ZeroThreads_OmitsFlag()
    {
        var args = Args(ScanTargetKind.WholeDisk, ScanMode.Quick, threads: 0);
        Assert.DoesNotContain("--threads", args);
    }
}
