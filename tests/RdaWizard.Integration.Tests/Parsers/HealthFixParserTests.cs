using RdaWizard.Core.Models;
using RdaWizard.Integration.Parsers;
using Xunit;

namespace RdaWizard.Integration.Tests.Parsers;

/// <summary>Golden fixtures for rrecov health + fix output (CORE-CONTRACT §13).</summary>
public class HealthFixParserTests
{
    private static readonly string[] GoldenHealth =
    {
        "SMART (\\\\.\\PhysicalDrive0): ATA version 8, 22 attribute(s)",
        "  ID value worst raw name",
        "  5 100 100 42 Reallocated_Sector_Ct",
        "  9 98 98 12345 Power_On_Hours",
        "  194 118 100 6500004 Temperature_Celsius",
        "  reallocated=42 pending=0 uncorrectable=0 hours=12345 temp=32C",
        "  WARNING: failing attribute present",
        "seek-penalty (SSD heuristics): no seek penalty (SSD)",
        "TRIM: enabled (WARNING: TRIM erases deleted data on SSDs - disable before recovery)",
    };

    [Fact]
    public void ParsesSmartAttributes()
    {
        var report = HealthOutputParser.Parse(0, GoldenHealth);
        Assert.Equal(3, report.Attributes.Count);
        Assert.Equal(5, report.Attributes[0].Id);
        Assert.Equal(100, report.Attributes[0].Value);
        Assert.Equal(42, report.Attributes[0].Raw);
        Assert.Equal("Reallocated_Sector_Ct", report.Attributes[0].Name);
    }

    [Fact]
    public void ParsesAggregates()
    {
        var report = HealthOutputParser.Parse(0, GoldenHealth);
        Assert.NotNull(report.Aggregates);
        Assert.Equal(42, report.Aggregates!.Reallocated);
        Assert.Equal(0, report.Aggregates.Pending);
        Assert.Equal(12345, report.Aggregates.PowerOnHours);
        Assert.Equal(32, report.Aggregates.TemperatureC);
    }

    [Fact]
    public void ParsesWarnings()
    {
        var report = HealthOutputParser.Parse(0, GoldenHealth);
        var warning = Assert.Single(report.Warnings);
        Assert.Equal(HealthWarningKind.FailingAttribute, warning.Kind);
    }

    [Fact]
    public void ParsesSsdHeuristic_AndTrim()
    {
        var report = HealthOutputParser.Parse(0, GoldenHealth);
        Assert.True(report.IsSsd);
        Assert.Equal(TrimState.Enabled, report.Trim);
    }

    [Fact]
    public void ParsesNvmeHealth()
    {
        var lines = new[]
        {
            "NVMe health: temp=36.5C spare=90%/threshold=10% used=10%",
            "  read=1234 units written=567 units power-on=890h unsafe-shutdowns=12 media-errors=0",
        };
        var report = HealthOutputParser.Parse(1, lines);
        Assert.NotNull(report.Nvme);
        Assert.Equal(36, report.Nvme!.TemperatureC);
        Assert.Equal(90, report.Nvme.SparePercent);
        Assert.Equal(10, report.Nvme.SpareThresholdPercent);
        Assert.Equal(10, report.Nvme.UsedPercent);
        Assert.Equal(890, report.Nvme.PowerOnHours);
        Assert.Equal(12, report.Nvme.UnsafeShutdowns);
        Assert.Equal(0, report.Nvme.MediaErrors);
    }

    [Fact]
    public void SmartUnavailableWarning_Parsed()
    {
        var lines = new[]
        {
            "SMART unavailable (rc=80090032) - trying NVMe...",
            "seek-penalty (SSD heuristics): seek penalty present (not SSD)",
            "TRIM: disabled",
        };
        var report = HealthOutputParser.Parse(2, lines);
        Assert.Contains(report.Warnings, w => w.Kind == HealthWarningKind.SmartUnavailable);
        Assert.False(report.IsSsd!.Value);
        Assert.Equal(TrimState.Disabled, report.Trim);
    }

    private static readonly string[] GoldenFixPlan =
    {
        "repair plan for ntfs volume: 2 action(s)",
        "  [backup-bs] offset=0 len=512 : NTFS backup boot sector differs from primary",
        "  [restore-bs] offset=0 len=512 : restore primary from backup",
        "dry-run only - re-run with --apply to write",
    };

    [Fact]
    public void ParsesFixPlan()
    {
        var result = FixOutputParser.Parse(GoldenFixPlan);
        Assert.Equal(RepairPhase.DryRun, result.Phase);
        Assert.Equal(2, result.Actions.Count);
        Assert.Equal("backup-bs", result.Actions[0].Kind);
        Assert.Equal(0, result.Actions[0].Offset);
        Assert.Equal(512, result.Actions[0].Length);
        Assert.Contains("backup boot sector", result.Actions[0].Reason);
    }

    [Fact]
    public void ParsesRefusal()
    {
        var result = FixOutputParser.Parse(["REFUSED: no valid NTFS structures found (nothing to anchor a repair)"]);
        Assert.Equal(RepairPhase.Refused, result.Phase);
        Assert.NotNull(result.RefusalReason);
        Assert.Contains("no valid NTFS", result.RefusalReason);
    }

    [Fact]
    public void ParsesCleanVolume()
    {
        var result = FixOutputParser.Parse(["clean fat32 volume - nothing to repair (S2: signature ok)"]);
        Assert.Equal(RepairPhase.Clean, result.Phase);
    }

    [Fact]
    public void ParsesExecuteOk()
    {
        var lines = new[]
        {
            "repair plan for ntfs volume: 1 action(s)",
            "  [restore-bs] offset=0 len=512 : reason",
            "execute OK: 512 sector(s) written + verified",
            "undo log saved: C:\\undo\\log.bin  (restore with: rrecov fix --undo-apply C:\\undo\\log.bin)",
        };
        var result = FixOutputParser.Parse(lines);
        Assert.Equal(RepairPhase.Applied, result.Phase);
        Assert.Equal(512, result.SectorsWritten);
        Assert.Equal("C:\\undo\\log.bin", result.UndoLogPath);
    }
}
