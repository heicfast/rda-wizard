using System.Management;
using Microsoft.Extensions.Logging;
using RdaWizard.Core.Models;
using RdaWizard.Core.Services;
using RdaWizard.Core.ViewModels;
using RdaWizard.Integration.Cli;
using RdaWizard.Integration.Parsers;

namespace RdaWizard.Integration;

/// <summary>
/// Session service: lists sessions in the managed folder, loads them via
/// <c>rrecov tree --in &lt;path&gt; --csv</c> (the full CSV streams through the
/// parser). Corrupt sessions surface the engine's load failure — never a
/// partial tree.
/// </summary>
public sealed class SessionService : ISessionService
{
    private readonly EngineProcessHost _engine;
    private readonly ILogger<SessionService> _logger;

    public string SessionsDirectory { get; }

    public SessionService(EngineProcessHost engine, ILogger<SessionService> logger, string? sessionsDirectoryOverride = null)
    {
        _engine = engine;
        _logger = logger;
        SessionsDirectory = sessionsDirectoryOverride ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "RdaWizard", "sessions");
        Directory.CreateDirectory(SessionsDirectory);
    }

    public async Task<SessionLoadResult> LoadAsync(string sessionPath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(sessionPath))
        {
            return new SessionLoadResult
            {
                Success = false,
                Diagnostic = $"The session file does not exist: {sessionPath}",
                Session = null,
            };
        }

        var csvLines = new List<string>(capacity: 4096);
        var run = await _engine.RunAsync(
            ["tree", "--in", sessionPath, "--csv"],
            onLine: line => csvLines.Add(line),
            keepLine: static line => line.StartsWith("session:", StringComparison.Ordinal) ||
                                     line.StartsWith("cannot open session", StringComparison.Ordinal) ||
                                     line.StartsWith("session load failed", StringComparison.Ordinal),
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (run.ExitCode != 0)
        {
            var diagnostic = (run.KeptLines.Count > 0 ? run.KeptLines[0] : null)
                ?? (run.ExitCode == 1 ? "The engine rejected the session file (corrupt or incompatible)." : $"The engine exited with code {run.ExitCode}.");
            _logger.LogWarning("Session load failed: {Path}: {Diagnostic}", sessionPath, diagnostic);
            return new SessionLoadResult { Success = false, Diagnostic = diagnostic, Session = null };
        }

        var nodes = TreeCsvParser.Parse(csvLines);
        var session = new LoadedSession { SessionPath = sessionPath, Nodes = nodes };
        _logger.LogInformation("Session loaded: {Path}: {Nodes} nodes ({Files} files)", sessionPath, nodes.Count, session.FileCount);
        return new SessionLoadResult { Success = true, Diagnostic = null, Session = session };
    }

    public Task<IReadOnlyList<SessionInfo>> ListRecentAsync(CancellationToken cancellationToken = default)
        => Task.Run<IReadOnlyList<SessionInfo>>(() =>
        {
            var result = new List<SessionInfo>();
            try
            {
                foreach (var file in Directory.EnumerateFiles(SessionsDirectory, "*.crsf"))
                {
                    var info = new FileInfo(file);
                    result.Add(new SessionInfo
                    {
                        Path = file,
                        SavedAtUtc = info.LastWriteTimeUtc,
                        SizeBytes = info.Length,
                        TargetDescription = null, // engine session records are sparse (G-14)
                        FileCount = null,
                    });
                }
            }
            catch (DirectoryNotFoundException)
            {
                // Empty result.
            }

            return (IReadOnlyList<SessionInfo>)result.OrderByDescending(static s => s.SavedAtUtc).ToArray();
        }, cancellationToken);
}

/// <summary>Recovery execution + destination pre-flight (overlap gate + space check + pre-creation).</summary>
public sealed class RecoveryService : IRecoveryService
{
    private readonly EngineProcessHost _engine;
    private readonly ILogger<RecoveryService> _logger;

    public RecoveryService(EngineProcessHost engine, ILogger<RecoveryService> logger)
    {
        _engine = engine;
        _logger = logger;
    }

    public async Task<IReadOnlyList<string>> PreFlightAsync(RecoveryRequest request, CancellationToken cancellationToken = default)
    {
        var refusals = new List<string>();

        // 1. Hard gate: destination overlaps source disk (ADR-7, defense in depth over engine [F12]).
        var root = ResolvePathRoot(request.DestinationDirectory);
        if (OperatingSystem.IsWindows() && TryMapRootToDisk(root, out var destinationDisk) &&
            destinationDisk == request.SourceDiskIndex)
        {
            refusals.Add(
                $"The destination is on disk {destinationDisk} — the same disk being scanned. Recovering onto the source destroys recoverable data. Choose a different disk.");
        }

        // 2. Destination writability + pre-creation (G-08: engine cannot mkdir).
        try
        {
            Directory.CreateDirectory(request.DestinationDirectory);
            var probe = Path.Combine(request.DestinationDirectory, $".rda-write-probe-{Guid.NewGuid():N}");
            await File.WriteAllTextAsync(probe, string.Empty, cancellationToken).ConfigureAwait(false);
            File.Delete(probe);
        }
        catch (UnauthorizedAccessException)
        {
            refusals.Add("The destination folder is not writable by this application. Choose another location.");
        }
        catch (IOException ex)
        {
            refusals.Add("The destination folder is not accessible: " + ex.Message);
        }

        // 3. Space: the needed-bytes soft gate (og "Insufficient Disk Space"
        // — Continue-anyway / Reselect) lives in the RecoveryViewModel
        // (F-C-4): preflight keeps only the HARD gates (overlap, writability).

        return refusals;
    }

    public async Task<RecoverySummary> RecoverAsync(RecoveryRequest request, IProgress<RecoveryProgress>? progress, CancellationToken cancellationToken)
    {
        var arguments = new List<string>
        {
            "recover",
            "--in", request.SessionPath,
            "--disk", request.SourceDiskIndex.ToString(System.Globalization.CultureInfo.InvariantCulture),
            "--to", request.DestinationDirectory,
        };
        if (!string.IsNullOrWhiteSpace(request.NameFilter))
        {
            arguments.Add("--filter");
            arguments.Add(request.NameFilter);
        }
        if (request.DeletedOnly)
        {
            arguments.Add("--deleted-only");
        }

        var startedAt = DateTimeOffset.UtcNow;
        var lastFilesDone = 0L;

        void OnLine(string line)
        {
            // Engine recover progress: the final "recovered: %llu done, ..." line.
            var match = System.Text.RegularExpressions.Regex.Match(line, @"(\d+)\s+done");
            if (match.Success && long.TryParse(match.Groups[1].Value, out var done))
            {
                lastFilesDone = done;
                progress?.Report(new RecoveryProgress
                {
                    FilesDone = done,
                    BytesDone = null,
                    Elapsed = DateTimeOffset.UtcNow - startedAt,
                });
            }
        }

        var run = await _engine.RunAsync(arguments, onLine: OnLine,
            keepLine: static line => line.StartsWith("queued", StringComparison.Ordinal) ||
                                     line.StartsWith("recovered:", StringComparison.Ordinal) ||
                                     line.StartsWith("recovery failed", StringComparison.Ordinal) ||
                                     line.StartsWith("[F12]", StringComparison.Ordinal) ||
                                     line.StartsWith("[dest]", StringComparison.Ordinal),
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (run.WasCancelled)
        {
            return new RecoverySummary
            {
                Recovered = lastFilesDone,
                Refused = 0,
                Errors = 0,
                HashMismatch = 0,
                Outcome = RecoveryOutcome.Cancelled,
                Elapsed = run.Elapsed,
            };
        }

        var failureLine = FindFirst(run.KeptLines, static l => l.StartsWith("recovery failed", StringComparison.Ordinal));
        if (run.ExitCode == 1 || failureLine is not null)
        {
            throw new InvalidOperationException(failureLine ?? $"Recovery setup failed (engine exit {run.ExitCode}).");
        }

        // "recovered: %llu done, %llu refused, %llu error(s), %llu hash mismatch"
        long done = 0, refused = 0, errors = 0, mismatch = 0;
        foreach (var line in run.KeptLines)
        {
            if (!line.StartsWith("recovered:", StringComparison.Ordinal)) continue;
            var numbers = ExtractNumbers(line);
            if (numbers.Count >= 4)
            {
                done = numbers[0];
                refused = numbers[1];
                errors = numbers[2];
                mismatch = numbers[3];
            }
        }

        var outcome = run.ExitCode switch
        {
            0 when errors == 0 && mismatch == 0 => RecoveryOutcome.Success,
            0 or 2 when errors > 0 || mismatch > 0 => RecoveryOutcome.Partial,
            2 => RecoveryOutcome.Partial,
            _ => RecoveryOutcome.Failed,
        };

        return new RecoverySummary
        {
            Recovered = done,
            Refused = refused,
            Errors = errors,
            HashMismatch = mismatch,
            Outcome = outcome,
            Elapsed = run.Elapsed,
        };
    }

    public Task<string> ExportReportAsync(RecoverySummary summary, RecoveryRequest request, string destinationFile, CancellationToken cancellationToken = default)
        => Task.Run(() =>
        {
            var report = BuildReport(summary, request);
            File.WriteAllText(destinationFile, report, System.Text.Encoding.UTF8);
            return destinationFile;
        }, cancellationToken);

    internal static string BuildReport(RecoverySummary summary, RecoveryRequest request)
    {
        var culture = System.Globalization.CultureInfo.InvariantCulture;
        var builder = new System.Text.StringBuilder(1024);
        builder.AppendLine("RDA Recovery Report");
        builder.AppendLine("===================");
        builder.AppendLine(culture, $"Generated (UTC):  {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss}");
        builder.AppendLine(culture, $"Session:          {request.SessionPath}");
        builder.AppendLine(culture, $"Source disk:      {request.SourceDiskIndex}");
        builder.AppendLine(culture, $"Destination:      {request.DestinationDirectory}");
        builder.AppendLine();
        builder.AppendLine(culture, $"Outcome:          {summary.Outcome}");
        builder.AppendLine(culture, $"Recovered:        {summary.Recovered}");
        builder.AppendLine(culture, $"Refused:          {summary.Refused}  (destination safety refusals)");
        builder.AppendLine(culture, $"Errors:           {summary.Errors}");
        builder.AppendLine(culture, $"Hash mismatches:  {summary.HashMismatch}");
        builder.AppendLine(culture, $"Elapsed:          {summary.Elapsed:hh\\:mm\\:ss}");
        builder.AppendLine();
        builder.AppendLine("Notes:");
        builder.AppendLine("- Structured (non-carved) files carry no stored hash; verification is hash-less for them (engine gap G-22).");
        builder.AppendLine("- Empty folders are not reconstructed (engine gap G-08).");
        builder.AppendLine("- 'Refused' items were blocked because the destination overlapped the source (safety gate).");
        return builder.ToString();
    }

    private static string? FindFirst(IReadOnlyList<string> lines, Func<string, bool> predicate)
    {
        foreach (var line in lines)
        {
            if (predicate(line)) return line;
        }
        return null;
    }

    private static string ResolvePathRoot(string path)
    {
        try
        {
            var full = Path.GetFullPath(path);
            return Path.GetPathRoot(full) ?? path;
        }
        catch (ArgumentException)
        {
            return path;
        }
    }

    internal static bool TryMapRootToDisk(string root, out int diskIndex)
    {
        diskIndex = -1;
        if (!OperatingSystem.IsWindows()) return false;
        try
        {
            var letter = root.TrimEnd('\\', ':');
            if (letter.Length != 1 || !char.IsLetter(letter[0])) return false;

            using var searcher = new ManagementObjectSearcher(
                $"ASSOCIATORS OF {{Win32_LogicalDisk.DeviceID='{letter}:'}} WHERE AssocClass = Win32_LogicalDiskToPartition");
            foreach (var item in searcher.Get())
            {
                var device = item["DeviceID"]?.ToString();
                var match = System.Text.RegularExpressions.Regex.Match(device ?? string.Empty, @"Disk #(\d+)");
                if (match.Success)
                {
                    diskIndex = int.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
                    return true;
                }
            }
        }
        catch
        {
            // WMI unavailable — overlap check reports "unavailable" (truthful).
            return false;
        }
        return false;
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

/// <summary>Health service via rrecov health.</summary>
public sealed class HealthService : IHealthService
{
    private readonly EngineProcessHost _engine;

    public HealthService(EngineProcessHost engine) => _engine = engine;

    public async Task<HealthReport> GetHealthAsync(int diskIndex, CancellationToken cancellationToken = default)
    {
        var run = await _engine.RunAsync(
            ["health", "--disk", diskIndex.ToString(System.Globalization.CultureInfo.InvariantCulture)],
            keepLine: static _ => true,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (run.ExitCode != 0)
        {
            var diagnostic = (run.HeadLines.Count > 0 ? run.HeadLines[0].Trim() : null)
                ?? $"The engine could not read disk {diskIndex} health (exit {run.ExitCode}).";
            throw new InvalidOperationException(diagnostic);
        }

        return HealthOutputParser.Parse(diskIndex, run.KeptLines);
    }
}

/// <summary>RawFixer service via rrecov fix.</summary>
public sealed class RepairService : IRepairService
{
    private readonly EngineProcessHost _engine;

    public RepairService(EngineProcessHost engine) => _engine = engine;

    public async Task<RepairResult> AnalyzeAsync(int diskIndex, int? partitionIndex, CancellationToken cancellationToken = default)
    {
        var args = BuildArgs(diskIndex, partitionIndex, apply: false, undoPath: null);
        var run = await _engine.RunAsync(args, keepLine: static _ => true, cancellationToken: cancellationToken).ConfigureAwait(false);
        return FixOutputParser.Parse(run.KeptLines);
    }

    public async Task<RepairResult> ApplyAsync(int diskIndex, int? partitionIndex, string undoLogPath, CancellationToken cancellationToken = default)
    {
        var args = BuildArgs(diskIndex, partitionIndex, apply: true, undoPath: undoLogPath);
        var run = await _engine.RunAsync(args, keepLine: static _ => true, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (run.ExitCode == 2)
        {
            throw new InvalidOperationException(
                (run.KeptLines.FirstOrDefault(l => l.Contains("undo FAILED", StringComparison.Ordinal)) ?? run.TailText) +
                " — verify the disk state!");
        }
        return FixOutputParser.Parse(run.KeptLines);
    }

    public async Task<RepairResult> UndoAsync(string undoLogPath, CancellationToken cancellationToken = default)
    {
        var run = await _engine.RunAsync(["fix", "--undo-apply", undoLogPath], keepLine: static _ => true, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (run.ExitCode != 0)
        {
            throw new InvalidOperationException(run.TailText + " — verify the disk state!");
        }
        return FixOutputParser.Parse(run.KeptLines);
    }

    private static List<string> BuildArgs(int diskIndex, int? partitionIndex, bool apply, string? undoPath)
    {
        var args = new List<string> { "fix", "--disk", diskIndex.ToString(System.Globalization.CultureInfo.InvariantCulture) };
        if (partitionIndex is { } partition)
        {
            args.Add("--part");
            args.Add(partition.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }
        if (apply)
        {
            args.Add("--apply");
            if (!string.IsNullOrWhiteSpace(undoPath))
            {
                args.Add("--undo");
                args.Add(undoPath);
            }
        }
        return args;
    }
}
