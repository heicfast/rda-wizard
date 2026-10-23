using System.Globalization;
using RdaWizard.Core.Models;

namespace RdaWizard.Integration.Parsers;

/// <summary>
/// Parses <c>rrecov health --disk N</c> output (grammar verified from
/// rrecov.cpp @ 183dfcd — see CORE-CONTRACT §13 "health").
/// </summary>
public static class HealthOutputParser
{
    public static HealthReport Parse(int diskIndex, IReadOnlyList<string> lines)
    {
        var attributes = new List<SmartAttribute>();
        SmartAggregates? aggregates = null;
        NvmeHealth? nvme = null;
        var warnings = new List<HealthWarning>();
        var isSsd = (bool?)null;
        var trim = TrimState.Unknown;

        foreach (var raw in lines)
        {
            var line = raw.Trim();

            // "  ID value worst raw name" header — skipped.
            // "  5 100 100 42 Reallocated_Sector_Count"
            var attrMatch = RegexMatch(line, @"^(\d+)\s+(\d+)\s+(\d+)\s+(\d+)\s+(\S.*)$");
            if (attrMatch is { } a && int.TryParse(a[0], out var id) && id is >= 1 and <= 255)
            {
                attributes.Add(new SmartAttribute
                {
                    Id = id,
                    Value = int.Parse(a[1], CultureInfo.InvariantCulture),
                    Worst = int.Parse(a[2], CultureInfo.InvariantCulture),
                    Raw = long.Parse(a[3], CultureInfo.InvariantCulture),
                    Name = a[4].Trim(),
                });
                continue;
            }

            // "  reallocated=%llu pending=%llu uncorrectable=%llu hours=%llu temp=%lluC"
            var aggMatch = RegexMatch(line, @"^reallocated=(\d+)\s+pending=(\d+)\s+uncorrectable=(\d+)\s+hours=(\d+)\s+temp=(\d+)C");
            if (aggMatch is { } g)
            {
                aggregates = new SmartAggregates
                {
                    Reallocated = int.Parse(g[0], CultureInfo.InvariantCulture),
                    Pending = int.Parse(g[1], CultureInfo.InvariantCulture),
                    Uncorrectable = int.Parse(g[2], CultureInfo.InvariantCulture),
                    PowerOnHours = long.Parse(g[3], CultureInfo.InvariantCulture),
                    TemperatureC = int.Parse(g[4], CultureInfo.InvariantCulture),
                };
                continue;
            }

            // "NVMe health: temp=%u.%uC spare=%u%%/threshold=%u%% used=%llu%%"
            var nvmeMatch = RegexMatch(line, @"^NVMe health:\s+temp=([\d.]+)C\s+spare=(\d+)%/threshold=(\d+)%\s+used=(\d+)%");
            if (nvmeMatch is { } n)
            {
                var details = new Dictionary<string, long>();
                foreach (var detailLine in lines)
                {
                    var d = RegexMatch(detailLine.Trim(), @"^read=(\d+)\s+units\s+written=(\d+)\s+units\s+power-on=(\d+)h\s+unsafe-shutdowns=(\d+)\s+media-errors=(\d+)");
                    if (d is { })
                    {
                        details["read"] = long.Parse(d[0], CultureInfo.InvariantCulture);
                        details["written"] = long.Parse(d[1], CultureInfo.InvariantCulture);
                        details["hours"] = long.Parse(d[2], CultureInfo.InvariantCulture);
                        details["shutdowns"] = long.Parse(d[3], CultureInfo.InvariantCulture);
                        details["media"] = long.Parse(d[4], CultureInfo.InvariantCulture);
                    }
                }

                nvme = new NvmeHealth
                {
                    TemperatureC = (int)double.Parse(n[0], CultureInfo.InvariantCulture),
                    SparePercent = int.Parse(n[1], CultureInfo.InvariantCulture),
                    SpareThresholdPercent = int.Parse(n[2], CultureInfo.InvariantCulture),
                    UsedPercent = int.Parse(n[3], CultureInfo.InvariantCulture),
                    PowerOnHours = details.GetValueOrDefault("hours"),
                    UnsafeShutdowns = details.GetValueOrDefault("shutdowns"),
                    MediaErrors = details.GetValueOrDefault("media"),
                };
                continue;
            }

            if (line.StartsWith("WARNING: failing attribute", StringComparison.Ordinal))
            {
                warnings.Add(new HealthWarning(HealthWarningKind.FailingAttribute, line));
            }
            else if (line.StartsWith("WARNING: unstable media", StringComparison.Ordinal))
            {
                warnings.Add(new HealthWarning(HealthWarningKind.UnstableMedia, line));
            }
            else if (line.StartsWith("WARNING: critical warning bits", StringComparison.Ordinal))
            {
                warnings.Add(new HealthWarning(HealthWarningKind.CriticalWarningBits, line));
            }
            else if (line.StartsWith("SMART unavailable", StringComparison.Ordinal))
            {
                warnings.Add(new HealthWarning(HealthWarningKind.SmartUnavailable, line));
            }
            else if (line.StartsWith("seek-penalty (SSD heuristics):", StringComparison.Ordinal))
            {
                isSsd = line.Contains("SSD", StringComparison.OrdinalIgnoreCase) && !line.Contains("not SSD", StringComparison.OrdinalIgnoreCase);
            }
            else if (line.StartsWith("TRIM:", StringComparison.Ordinal))
            {
                trim = line.Contains("enabled", StringComparison.OrdinalIgnoreCase)
                    ? TrimState.Enabled
                    : line.Contains("disabled", StringComparison.OrdinalIgnoreCase)
                        ? TrimState.Disabled
                        : TrimState.Unknown;
            }
        }

        return new HealthReport
        {
            DiskIndex = diskIndex,
            Attributes = attributes,
            Aggregates = aggregates,
            Nvme = nvme,
            IsSsd = isSsd,
            Trim = trim,
            Warnings = warnings,
        };
    }

    private static string[]? RegexMatch(string input, string pattern)
    {
        var match = System.Text.RegularExpressions.Regex.Match(input, pattern);
        if (!match.Success) return null;
        var groups = new string[match.Groups.Count - 1];
        for (var i = 1; i < match.Groups.Count; i++) groups[i - 1] = match.Groups[i].Value;
        return groups;
    }
}

/// <summary>Parses <c>rrecov fix</c> output (dry-run plan / apply / refused / clean / undo).</summary>
public static class FixOutputParser
{
    public static RepairResult Parse(IReadOnlyList<string> lines)
    {
        var actions = new List<RepairAction>();
        var phase = RepairPhase.DryRun;
        string? refusal = null;
        string? undoPath = null;
        long? sectorsWritten = null;
        string? engineText = null;

        foreach (var raw in lines)
        {
            var line = raw.Trim();

            if (line.StartsWith("REFUSED:", StringComparison.Ordinal))
            {
                phase = RepairPhase.Refused;
                refusal = line["REFUSED:".Length..].Trim();
                engineText = line;
                continue;
            }

            if (line.StartsWith("clean ", StringComparison.Ordinal) || line.Contains("nothing to repair"))
            {
                phase = RepairPhase.Clean;
                engineText = line;
                continue;
            }

            if (line.StartsWith("repair plan for", StringComparison.Ordinal))
            {
                phase = RepairPhase.DryRun;
                engineText = line;
                continue;
            }

            // "  [kind] offset=%lld len=%lld : reason"
            var actionMatch = RegexMatch(line, @"^\[(\S+)\]\s+offset=(\d+)\s+len=(\d+)\s*:\s*(.*)$");
            if (actionMatch is { } a)
            {
                actions.Add(new RepairAction
                {
                    Kind = a[0],
                    Offset = long.Parse(a[1], CultureInfo.InvariantCulture),
                    Length = long.Parse(a[2], CultureInfo.InvariantCulture),
                    Reason = a[3].Trim(),
                });
                continue;
            }

            if (line.StartsWith("execute OK:", StringComparison.Ordinal))
            {
                phase = RepairPhase.Applied;
                var m = RegexMatch(line, @"execute OK:\s*(\d+)\s*sector");
                sectorsWritten = m is { } ? long.Parse(m[0], CultureInfo.InvariantCulture) : null;
                engineText = line;
                continue;
            }

            if (line.StartsWith("undo log saved", StringComparison.Ordinal) ||
                line.Contains("--undo-apply", StringComparison.Ordinal))
            {
                // "undo log saved: <path>" + the restore command line.
                var m = RegexMatch(line, @"undo log saved:\s*(\S+)");
                if (m is { }) undoPath = m[0];
            }
        }

        return new RepairResult
        {
            Phase = phase,
            Actions = actions,
            RefusalReason = refusal,
            UndoLogPath = undoPath,
            SectorsWritten = sectorsWritten,
            EngineText = engineText,
        };
    }

    private static string[]? RegexMatch(string input, string pattern)
    {
        var match = System.Text.RegularExpressions.Regex.Match(input, pattern);
        if (!match.Success) return null;
        var groups = new string[match.Groups.Count - 1];
        for (var i = 1; i < match.Groups.Count; i++) groups[i - 1] = match.Groups[i].Value;
        return groups;
    }
}
