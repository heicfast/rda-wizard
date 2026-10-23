using RdaWizard.Core.Models;
using RdaWizard.Integration.Parsers;
using Xunit;

namespace RdaWizard.Integration.Tests.Parsers;

/// <summary>
/// Golden fixtures frozen from rrecov.cpp list output (CORE-CONTRACT §13,
/// corrected per docs/PARITY-AUDIT.md F-C-1 — table kind is the final
/// UNBRACKETED token; engine prints 'GPT'/'MBR'/'-' when the table parsed
/// and 'no-table' when it did not):
/// <code>
/// %zu disk(s), %zu volume(s)
///   Disk %u: %llu MiB (%s) %s [removable|fixed] GPT|MBR|-|no-table
///     %c: %s %s %llu MiB [BitLocker]
/// </code>
/// </summary>
public class ListOutputParserTests
{
    private static readonly string[] GoldenOutput =
    {
        "2 disk(s), 3 volume(s)",
        "  Disk 0: 476940 MiB (Samsung SSD 970 EVO 500GB) S4EWNX0N123456 [fixed] GPT",
        "    C: NTFS Windows 190000 MiB",
        "    D: FAT32 DATA 10000 MiB",
        "  Disk 1: 76332 MiB (SanDisk Cruzer Glide 3.0) 1234567890 [removable] MBR",
        "    E: exFAT PHOTOS 70000 MiB [BitLocker]",
    };

    [Fact]
    public void ParsesDiskCountAndVolumes()
    {
        Assert.True(ListOutputParser.TryParse(GoldenOutput, out var disks, out _));
        Assert.Equal(2, disks.Count);
        Assert.Equal(3, disks.SelectMany(static d => d.Volumes).Count());
    }

    [Fact]
    public void ParsesDiskFields()
    {
        Assert.True(ListOutputParser.TryParse(GoldenOutput, out var disks, out _));
        var disk0 = disks[0];
        Assert.Equal(0, disk0.Index);
        Assert.Equal(476940L << 20, disk0.SizeBytes);
        Assert.Equal("Samsung SSD 970 EVO 500GB", disk0.Model);
        Assert.Equal("S4EWNX0N123456", disk0.Serial);
        Assert.False(disk0.IsRemovable);
        Assert.Equal(PartitionTableKind.Gpt, disk0.Table);
    }

    [Fact]
    public void ParsesVolumeFields_WithOwningDisk()
    {
        Assert.True(ListOutputParser.TryParse(GoldenOutput, out var disks, out _));
        var volumeE = disks[1].Volumes[0];
        Assert.Equal('E', volumeE.Letter);
        Assert.Equal("exFAT", volumeE.Filesystem);
        Assert.Equal("PHOTOS", volumeE.Label);
        Assert.Equal(70000L << 20, volumeE.SizeBytes);
        Assert.True(volumeE.IsBitLocker);
        Assert.Equal(1, volumeE.OwningDiskIndex);

        var volumeC = disks[0].Volumes[0];
        Assert.False(volumeC.IsBitLocker);
        Assert.Equal(0, volumeC.OwningDiskIndex);
    }

    [Fact]
    public void EnumerationFailedLine_Detected()
    {
        // No disks parse (returns false) AND the failure flag is set: the
        // enumeration service keys on the flag, never the exit code (G-20).
        Assert.False(ListOutputParser.TryParse(["enumeration failed"], out var disks, out var failed));
        Assert.True(failed);
        Assert.Empty(disks);
    }

    [Fact]
    public void EmptyOutput_ReportsNoDisks()
    {
        Assert.False(ListOutputParser.TryParse(Array.Empty<string>(), out var disks, out _));
        Assert.Empty(disks);
    }

    [Fact]
    public void NoTableKind_Parsed()
    {
        var lines = new[]
        {
            "1 disk(s), 0 volume(s)",
            "  Disk 2: 1000 MiB (RAW CARD) NONE [removable] no-table",
        };
        Assert.True(ListOutputParser.TryParse(lines, out var disks, out _));
        Assert.Equal(PartitionTableKind.None, disks[0].Table);
    }

    [Fact]
    public void UnknownLayoutDash_Parsed()
    {
        // PARITY-AUDIT F-C-1: engine prints '-' when the table parsed but the
        // layout is neither GPT nor MBR (rrecov.cpp:239-242) — must parse as
        // None, not be dropped (a dropped line loses the whole disk).
        var lines = new[]
        {
            "1 disk(s), 0 volume(s)",
            "  Disk 3: 2048 MiB (LOOP DEV) X [fixed] -",
        };
        Assert.True(ListOutputParser.TryParse(lines, out var disks, out _));
        Assert.Single(disks);
        Assert.Equal(PartitionTableKind.None, disks[0].Table);
    }

    [Fact]
    public void BracketedTableKind_RejectedAsEngineDrift()
    {
        // The OLD (wrong) grammar — CORE-CONTRACT §13 transcription error.
        // Real engine output NEVER brackets the table kind; if a line like
        // this ever parses again, the contract regressed. Guard test.
        var lines = new[]
        {
            "1 disk(s), 0 volume(s)",
            "  Disk 0: 100 MiB (M) S [fixed] [GPT]",
        };
        Assert.False(ListOutputParser.TryParse(lines, out var disks, out _));
        Assert.Empty(disks);
    }
}

/// <summary>Golden fixtures for the scan progress/summary grammar.</summary>
public class ScanOutputParserTests
{
    // "\r  %5.1f%%  (%lld s)   " — after \r-splitting the leading spaces remain.
    [Theory]
    [InlineData("  42.1%  (12 s)   ", 0.421, 12)]
    [InlineData("  0.0%  (0 s)   ", 0.0, 0)]
    [InlineData("  100.0%  (3599 s)   ", 1.0, 3599)]
    public void ParsesProgressLine(string line, double expectedFraction, int expectedSeconds)
    {
        Assert.True(ScanOutputParser.TryParseProgress(line, out var fraction, out var elapsed));
        Assert.Equal(expectedFraction, fraction, 3);
        Assert.Equal(expectedSeconds, elapsed.TotalSeconds);
    }

    [Theory]
    [InlineData("scan complete: 5 file(s)")]
    [InlineData("[transport] RDA driver path (\\\\.\\RDA)")]
    [InlineData("")]
    public void NonProgressLines_Rejected(string line)
    {
        Assert.False(ScanOutputParser.TryParseProgress(line, out _, out _));
    }

    [Fact]
    public void ParsesSummaryJson()
    {
        const string line = """{"files":1139157,"deleted":8123,"carved":45000,"lost_partitions":2,"bytes_scanned":987654321,"elapsed_ms":45012}""";
        Assert.True(ScanOutputParser.TryParseSummaryJson(line, out var summary));
        Assert.Equal(1139157, summary.Files);
        Assert.Equal(8123, summary.Deleted);
        Assert.Equal(45000, summary.Carved);
        Assert.Equal(2, summary.LostPartitions);
        Assert.Equal(987654321, summary.BytesScanned);
        Assert.Equal(45.012, summary.Elapsed.TotalSeconds, 3);
    }

    [Fact]
    public void SummaryJson_RejectsNonJson()
    {
        Assert.False(ScanOutputParser.TryParseSummaryJson("scan complete: 5 file(s)", out _));
    }

    [Fact]
    public void ParsesSessionSaved()
    {
        Assert.True(ScanOutputParser.TryParseSessionSaved("session saved: C:\\sessions\\auto-disk0.crsf", out var path));
        Assert.Equal(@"C:\sessions\auto-disk0.crsf", path);
    }

    [Fact]
    public void TransportLine_Detected()
    {
        Assert.True(ScanOutputParser.IsTransportLine("[transport] RDA driver path (\\\\.\\RDA)"));
        Assert.True(ScanOutputParser.IsTransportLine("[transport] direct path (\\\\.\\PhysicalDrive0)"));
        Assert.False(ScanOutputParser.IsTransportLine("direct path"));
    }

    [Fact]
    public void ParsesVolumeExtentLine()
    {
        Assert.True(ScanOutputParser.TryParseVolumeLine("[volume] C: disk 0 @2048 +409599999", out var letter, out var disk));
        Assert.Equal('C', letter);
        Assert.Equal(0, disk);
    }
}

/// <summary>Golden fixtures for stage-log read-error ranges.</summary>
public class StageLogParserTests
{
    [Fact]
    public void ParsesReadErrorRange()
    {
        Assert.True(StageLogParser.TryParseReadError(
            "  read-error range: @123456789 +4096 (attempts 3)",
            out var range));
        Assert.Equal(123456789, range.OffsetBytes);
        Assert.Equal(4096, range.LengthBytes);
        Assert.Equal(3, range.Attempts);
    }

    [Fact]
    public void RejectsOtherLines()
    {
        Assert.False(StageLogParser.TryParseReadError("scan end rc=0x0 files=10", out _));
    }

    [Fact]
    public void ParsesScanEnd()
    {
        Assert.True(StageLogParser.TryParseScanEnd("scan end rc=0x00000000 files=1139157 deleted=8123 carved=45000 lost_parts=2", out var files));
        Assert.Equal(1139157, files);
    }
}
