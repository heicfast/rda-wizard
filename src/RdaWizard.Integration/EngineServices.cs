using Microsoft.Extensions.Logging;
using RdaWizard.Core.Models;
using RdaWizard.Core.Services;
using RdaWizard.Integration.Cli;
using RdaWizard.Integration.Parsers;

namespace RdaWizard.Integration;

/// <summary>Device enumeration via <c>rrecov list</c> (gap G-20: exit code never trusted; stdout parsed).</summary>
public sealed class DeviceEnumerationService : IDeviceEnumerationService
{
    private readonly EngineProcessHost _engine;
    private readonly ILogger<DeviceEnumerationService> _logger;

    public DeviceEnumerationService(EngineProcessHost engine, ILogger<DeviceEnumerationService> logger)
    {
        _engine = engine;
        _logger = logger;
    }

    public async Task<EnumerationResult> EnumerateAsync(CancellationToken cancellationToken = default)
    {
        var run = await _engine.RunAsync(["list"], keepLine: static _ => true, cancellationToken: cancellationToken).ConfigureAwait(false);

        // "list" exits 0 even on enumeration failure (G-20) — parse stdout truth.
        var lines = run.KeptLines;
        var ok = ListOutputParser.TryParse(lines, out var disks, out var enumerationFailed);

        if (!ok || enumerationFailed)
        {
            var diagnostic = enumerationFailed
                ? "The engine could not enumerate disks on this system."
                : "The engine reported no disks.";
            _logger.LogWarning("Enumeration degraded: {Diagnostic} (exit {Exit})", diagnostic, run.ExitCode);
            return new EnumerationResult
            {
                Success = false,
                Diagnostic = diagnostic,
                Disks = disks,
                RawOutput = run.TailText,
            };
        }

        return new EnumerationResult { Success = true, Disks = EnrichWithCapacity(disks), RawOutput = run.TailText };
    }

    private static VolumeInfo WithCapacity(VolumeInfo volume)
    {
        if (volume.Letter is not { } letter)
        {
            return volume;
        }

        try
        {
            var drive = new DriveInfo(letter.ToString());
            if (!drive.IsReady)
            {
                return volume;
            }

            return volume with
            {
                UsedBytes = Math.Max(0, drive.TotalSize - drive.AvailableFreeSpace),
            };
        }
        catch (ArgumentException)
        {
            return volume;
        }
        catch (IOException)
        {
            return volume;
        }
    }

    /// <summary>
    /// OS capacity pass for the landing tiles' used/total bars — the og
    /// queries WMI/mountmgr for live capacity next to the engine's list; the
    /// .NET equivalent is DriveInfo per mounted letter. Unmounted or
    /// BitLocker volumes report no used figure (the bar hides). Defensive
    /// per volume: an OS refusal degrades that tile, never the enumeration.
    /// </summary>
    private static DiskInfo[] EnrichWithCapacity(IReadOnlyList<DiskInfo> disks)
    {
        return disks.Select(disk => disk with
        {
            Volumes = disk.Volumes.Select(WithCapacity).ToArray(),
        }).ToArray();
    }
}

/// <summary>Scan execution: maps options to CLI flags, streams truthful progress, harvests the summary.</summary>
public sealed class ScanService : IScanService
{
    private readonly EngineProcessHost _engine;
    private readonly ILogger<ScanService> _logger;

    public string? LastTransportLabel { get; private set; }

    public ScanService(EngineProcessHost engine, ILogger<ScanService> logger)
    {
        _engine = engine;
        _logger = logger;
    }

    public async Task<ScanSummary> RunScanAsync(ScanOptions options, IProgress<ScanProgressSnapshot>? progress, CancellationToken cancellationToken)
    {
        var arguments = BuildArguments(options);
        var startedAt = DateTimeOffset.UtcNow;
        var readErrors = new List<ReadErrorRange>();
        ScanSummary? summary = null;
        string? sessionPath = null;

        void OnLine(string line)
        {
            if (ScanOutputParser.TryParseProgress(line, out var fraction, out var elapsed))
            {
                progress?.Report(new ScanProgressSnapshot
                {
                    EngineFraction = fraction,
                    Elapsed = elapsed,
                    ReadErrors = readErrors.ToArray(),
                });
                return;
            }

            if (ScanOutputParser.IsTransportLine(line))
            {
                LastTransportLabel = ScanOutputParser.ParseTransportLabel(line);
                return;
            }

            if (StageLogParser.TryParseReadError(line, out var range))
            {
                readErrors.Add(range);
                progress?.Report(new ScanProgressSnapshot
                {
                    EngineFraction = null,
                    Elapsed = DateTimeOffset.UtcNow - startedAt,
                    ReadErrors = readErrors.ToArray(),
                });
                return;
            }

            if (ScanOutputParser.TryParseSummaryJson(line, out var parsedSummary))
            {
                summary = parsedSummary;
                return;
            }

            if (ScanOutputParser.TryParseSessionSaved(line, out var parsedSessionPath))
            {
                sessionPath = parsedSessionPath;
            }
        }

        var run = await _engine.RunAsync(
            arguments,
            onLine: OnLine,
            keepLine: static line => line.StartsWith('[') ||
                                     line.StartsWith("scan complete", StringComparison.Ordinal) ||
                                     line.StartsWith("scan failed", StringComparison.Ordinal) ||
                                     line.StartsWith("session saved", StringComparison.Ordinal) ||
                                     ScanOutputParser.TryParseSummaryJson(line, out _),
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (run.WasCancelled)
        {
            throw new OperationCanceledException(cancellationToken);
        }

        // Scan failure: exit 1 with "scan failed: ..." — surface the engine diagnostic.
        var failureLine = run.KeptLines.FirstOrDefault(l => l.StartsWith("scan failed", StringComparison.Ordinal));
        if (run.ExitCode != 0 || failureLine is not null)
        {
            throw new InvalidOperationException(failureLine ?? $"The engine exited with code {run.ExitCode} during the scan.");
        }

        summary ??= HarvestSummaryFromText(run.KeptLines);
        if (summary is null)
        {
            throw new InvalidOperationException("The engine finished without a scan summary — the scan did not complete correctly.");
        }

        return summary with
        {
            SessionPath = sessionPath ?? options.SessionPath,
            Elapsed = run.Elapsed,
        };
    }

    /// <summary>Flag mapping — EXACTLY the frozen contract (unit-tested in Integration.Tests).</summary>
    internal static IReadOnlyList<string> BuildArguments(ScanOptions options)
    {
        var arguments = new List<string> { "scan" };

        if (options.Target.Kind == ScanTargetKind.WholeDisk)
        {
            arguments.Add("--disk");
            arguments.Add(options.Target.DiskIndex.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }
        else
        {
            arguments.Add("--volume");
            arguments.Add(options.Target.VolumeLetter + ":");
        }

        arguments.AddRange(options.Mode.CliFlags());

        if (options.Target.Kind == ScanTargetKind.WholeDisk &&
            options.Mode is ScanMode.Deep or ScanMode.DeepPlusCarve)
        {
            // --deep flips the engine's whole_disk view (lost-partition discovery).
            // Quick mode must NOT carry it: --quick-only turns deep+carve off,
            // and a contradictory flag pair is not part of the frozen contract.
            arguments.Add("--deep");
        }

        if (options.Threads > 0)
        {
            arguments.Add("--threads");
            arguments.Add(options.Threads.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        if (!string.IsNullOrWhiteSpace(options.NameFilter))
        {
            arguments.Add("--filter");
            arguments.Add(options.NameFilter);
        }

        if (options.DeletedOnly)
        {
            arguments.Add("--deleted-only");
        }

        arguments.Add("--out");
        arguments.Add(options.SessionPath);
        arguments.Add("--log");
        arguments.Add(options.StageLogPath);
        arguments.Add("--json");

        return arguments;
    }

    private static ScanSummary? HarvestSummaryFromText(IReadOnlyList<string> lines)
    {
        // "scan complete: %llu file(s), %llu deleted, %llu carved, %llu lost partition(s) in %llu.%01u s"
        foreach (var line in lines)
        {
            if (!line.StartsWith("scan complete", StringComparison.Ordinal)) continue;
            var numbers = ExtractNumbers(line);
            if (numbers.Count >= 4)
            {
                return new ScanSummary
                {
                    Files = (int)numbers[0],
                    Deleted = (int)numbers[1],
                    Carved = (int)numbers[2],
                    LostPartitions = (int)numbers[3],
                    BytesScanned = 0,
                    Elapsed = numbers.Count > 4 ? TimeSpan.FromSeconds(numbers[4]) : TimeSpan.Zero,
                };
            }
        }
        return null;
    }

    private static List<long> ExtractNumbers(string line)
    {
        var numbers = new List<long>(8);
        var numberText = new System.Text.StringBuilder();
        foreach (var c in line)
        {
            if (char.IsDigit(c))
            {
                numberText.Append(c);
            }
            else if (numberText.Length > 0)
            {
                if (long.TryParse(numberText.ToString(), out var n)) numbers.Add(n);
                numberText.Clear();
            }
        }
        if (numberText.Length > 0 && long.TryParse(numberText.ToString(), out var last)) numbers.Add(last);
        return numbers;
    }
}
