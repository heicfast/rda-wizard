using System.Globalization;
using System.Text.RegularExpressions;
using RdaWizard.Core.Models;

namespace RdaWizard.Integration.Parsers;

/// <summary>
/// Parsers for the frozen rrecov output grammar (docs/CORE-CONTRACT.md §13;
/// formats verified line-by-line against rrecov.cpp — re-verified at the
/// parity audit against engine HEAD, see docs/PARITY-AUDIT.md F-C-1: the
/// table kind is the FINAL UNBRACKETED token; only [removable|fixed] is
/// bracketed). Every parser is pure (string in → model out) and unit-tested
/// against golden fixtures shaped like real engine output.
/// </summary>
public static partial class ListOutputParser
{
    // Engine format (rrecov.cpp print_disks):
    //   "  Disk %u: %llu MiB (%s) %s [%s] %s" — number, MiB, (model), serial,
    //   [removable|fixed], then the table kind as the final bare token:
    //   GPT | MBR | - (table parsed, layout neither GPT nor MBR) | no-table.
    private static readonly Regex DiskLine = new(
        @"^Disk\s+(\d+):\s+(\d+)\s+MiB\s+\((.*?)\)\s+(\S+)\s+\[(removable|fixed)\]\s+(GPT|MBR|no-table|-)$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    // "    %c: %s %s %llu MiB %s" — letter, fs, label, MiB, optional [BitLocker]
    private static readonly Regex VolumeLine = new(
        @"^([A-Za-z]):\s+(\S+)\s+(.*?)\s+(\d+)\s+MiB(?:\s+\[BitLocker\])?$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static bool TryParse(IReadOnlyList<string> lines, out IReadOnlyList<DiskInfo> disks, out bool enumerationFailed)
    {
        var result = new List<DiskInfo>();
        var volumes = new List<VolumeInfo>();
        enumerationFailed = lines.Any(static l => l.Trim().Equals("enumeration failed", StringComparison.Ordinal));

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (line.Length == 0) continue;

            var diskMatch = DiskLine.Match(line);
            if (diskMatch.Success)
            {
                FlushDisk(result, volumes);
                result.Add(new DiskInfo
                {
                    Index = int.Parse(diskMatch.Groups[1].Value, CultureInfo.InvariantCulture),
                    SizeBytes = long.Parse(diskMatch.Groups[2].Value, CultureInfo.InvariantCulture) << 20,
                    Model = diskMatch.Groups[3].Value.Trim(),
                    Serial = diskMatch.Groups[4].Value.Trim(),
                    IsRemovable = diskMatch.Groups[5].Value.Equals("removable", StringComparison.OrdinalIgnoreCase),
                    Table = ParseTable(diskMatch.Groups[6].Value),
                    Volumes = Array.Empty<VolumeInfo>(),
                });
                continue;
            }

            var volumeMatch = VolumeLine.Match(line);
            if (volumeMatch.Success)
            {
                volumes.Add(new VolumeInfo
                {
                    Letter = volumeMatch.Groups[1].Value[0],
                    Filesystem = volumeMatch.Groups[2].Value,
                    Label = volumeMatch.Groups[3].Value.Trim(),
                    SizeBytes = long.Parse(volumeMatch.Groups[4].Value, CultureInfo.InvariantCulture) << 20,
                    IsBitLocker = line.Contains("[BitLocker]", StringComparison.Ordinal),
                    OwningDiskIndex = result.Count > 0 ? result[^1].Index : -1,
                });
            }
        }

        FlushDisk(result, volumes);
        disks = result;
        return result.Count > 0;
    }

    private static void FlushDisk(List<DiskInfo> disks, List<VolumeInfo> volumes)
    {
        if (volumes.Count > 0 && disks.Count > 0)
        {
            var index = disks.Count - 1;
            disks[index] = disks[index] with { Volumes = volumes.ToArray() };
            volumes.Clear();
        }
    }

    private static PartitionTableKind ParseTable(string value) => value switch
    {
        "GPT" => PartitionTableKind.Gpt,
        "MBR" => PartitionTableKind.Mbr,
        _ => PartitionTableKind.None,
    };
}

/// <summary>Parses live scan progress lines + the completion summary JSON.</summary>
public static partial class ScanOutputParser
{
    // "\r  %5.1f%%  (%lld s)   " → after \r-splitting: "  42.1%  (12 s)   "
    [GeneratedRegex(@"^\s*(\d+(?:\.\d+)?)%\s+\((\d+)\s*s\)", RegexOptions.CultureInvariant)]
    private static partial Regex ProgressLineRegex();

    /// <summary>Tries to parse an overall progress line. Fraction is engine-weighted (quick→deep→carve, monotonic).</summary>
    public static bool TryParseProgress(string line, out double fraction, out TimeSpan elapsed)
    {
        fraction = 0;
        elapsed = TimeSpan.Zero;
        var match = ProgressLineRegex().Match(line.Trim());
        if (!match.Success) return false;

        fraction = double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture) / 100.0;
        elapsed = TimeSpan.FromSeconds(long.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture));
        return true;
    }

    /// <summary>Parses the scan summary JSON: {"files":N,"deleted":N,"carved":N,"lost_partitions":N,"bytes_scanned":N,"elapsed_ms":N}</summary>
    public static bool TryParseSummaryJson(string line, out ScanSummary summary)
    {
        summary = new ScanSummary
        {
            Files = 0,
            Deleted = 0,
            Carved = 0,
            LostPartitions = 0,
            BytesScanned = 0,
            Elapsed = TimeSpan.Zero,
        };

        var start = line.IndexOf('{');
        var end = line.LastIndexOf('}');
        if (start < 0 || end <= start) return false;

        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(line.Substring(start, end - start + 1));
            var root = doc.RootElement;
            summary = summary with
            {
                Files = GetInt(root, "files"),
                Deleted = GetInt(root, "deleted"),
                Carved = GetInt(root, "carved"),
                LostPartitions = GetInt(root, "lost_partitions"),
                BytesScanned = GetLong(root, "bytes_scanned"),
                Elapsed = TimeSpan.FromMilliseconds(GetLong(root, "elapsed_ms")),
            };
            return true;
        }
        catch (System.Text.Json.JsonException)
        {
            return false;
        }
    }

    /// <summary>"session saved: %s" → the .crsf path.</summary>
    public static bool TryParseSessionSaved(string line, out string sessionPath)
    {
        sessionPath = string.Empty;
        if (!line.StartsWith("session saved:", StringComparison.Ordinal)) return false;
        sessionPath = line["session saved:".Length..].Trim();
        return sessionPath.Length > 0;
    }

    /// <summary>"[transport] ..." lines → the truthful transport label for preflight/About.</summary>
    public static bool IsTransportLine(string line) => line.StartsWith("[transport]", StringComparison.Ordinal);

    /// <summary>Extracts transport truth: RDA path / direct path / failure.</summary>
    public static string ParseTransportLabel(string line) => line.Trim();

    /// <summary>"[volume] %c: disk %u @%llu +%llu" → volume→disk resolution for the overlap gate.</summary>
    public static bool TryParseVolumeLine(string line, out char letter, out int diskIndex)
    {
        letter = default;
        diskIndex = -1;
        var match = Regex.Match(line, @"^\[volume\]\s+([A-Za-z]):\s+disk\s+(\d+)");
        if (!match.Success) return false;
        letter = match.Groups[1].Value[0];
        diskIndex = int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
        return true;
    }

    private static int GetInt(System.Text.Json.JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) && value.ValueKind == System.Text.Json.JsonValueKind.Number
            ? value.GetInt32()
            : 0;

    private static long GetLong(System.Text.Json.JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) && value.ValueKind == System.Text.Json.JsonValueKind.Number
            ? value.GetInt64()
            : 0;
}

/// <summary>Parses the stage-log lines (live read-error ranges; scan end).</summary>
public static partial class StageLogParser
{
    // "  read-error range: @%llu +%llu (attempts %u)"
    [GeneratedRegex(@"read-error range:\s*@(\d+)\s*\+(\d+)\s*\(attempts\s*(\d+)\)", RegexOptions.CultureInvariant)]
    private static partial Regex ReadErrorRegex();

    public static bool TryParseReadError(string line, out ReadErrorRange range)
    {
        range = new ReadErrorRange { OffsetBytes = 0, LengthBytes = 0, Attempts = 0 };
        var match = ReadErrorRegex().Match(line);
        if (!match.Success) return false;
        range = range with
        {
            OffsetBytes = long.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture),
            LengthBytes = long.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture),
            Attempts = int.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture),
        };
        return true;
    }

    /// <summary>"scan end rc=0x%08x files=%llu ..." → files found so far (completion).</summary>
    public static bool TryParseScanEnd(string line, out int files)
    {
        files = 0;
        var match = Regex.Match(line, @"scan end rc=0x[0-9a-fA-F]+\s+files=(\d+)");
        if (!match.Success) return false;
        files = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
        return true;
    }
}
