using System.Buffers;
using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;

namespace RdaWizard.Integration.Cli;

/// <summary>
/// Monitored engine subprocess host — the ONLY place processes are spawned.
///
/// Engine stdout specifics this host is built around (verified from
/// rrecov.cpp): progress lines are carriage-return overwritten
/// (<c>"\r  %5.1f%%  (%lld s)   "</c>) and never end with \n until the
/// final "done" line; tree dumps can reach millions of lines. Therefore:
/// - stdout is read from the RAW stream and split on \r AND \n so progress
///   arrives live;
/// - every complete line passes the onLine callback (parsers decide
///   what matters) — the host retains only the head (first
///   <see cref="HeadRetentionLines"/>) and the tail (last
///   <see cref="TailRetentionLines"/>) plus everything a sink flags as
///   "keep", keeping memory bounded on million-line dumps.
/// - cancellation kills the process tree (engine cleanup is crash-safe by
///   design; sessions are only written on successful completion).
/// </summary>
public sealed class EngineProcessHost
{
    private const int HeadRetentionLines = 400;
    private const int TailRetentionLines = 200;

    private readonly ILogger<EngineProcessHost> _logger;
    private readonly IEngineLocator _locator;

    public EngineProcessHost(ILogger<EngineProcessHost> logger, IEngineLocator locator)
    {
        _logger = logger;
        _locator = locator;
    }

    /// <summary>True when the engine executable is present (About + preflight).</summary>
    public bool EngineAvailable => _locator.Locate() is not null;

    /// <summary>
    /// A line-keeping policy: return true for lines that must be retained in
    /// <see cref="EngineRun.KeptLines"/> regardless of head/tail windows.
    /// </summary>
    public delegate bool KeepLinePolicy(string line);

    /// <summary>Outcome of one engine invocation.</summary>
    public sealed record EngineRun
    {
        public required int ExitCode { get; init; }
        public required IReadOnlyList<string> HeadLines { get; init; }
        public required IReadOnlyList<string> TailLines { get; init; }
        public required IReadOnlyList<string> KeptLines { get; init; }
        public required string StdErr { get; init; }
        public required TimeSpan Elapsed { get; init; }
        public bool WasCancelled { get; init; }

        public string TailText => string.Join(Environment.NewLine, TailLines);
    }

    /// <summary>
    /// Runs the engine. <paramref name="onLine"/> receives every stdout line
    /// live (in order); exceptions thrown from it propagate as faults.
    /// </summary>
    public async Task<EngineRun> RunAsync(
        IReadOnlyList<string> arguments,
        Action<string>? onLine = null,
        KeepLinePolicy? keepLine = null,
        int timeoutSeconds = 0,
        string? workingDirectory = null,
        CancellationToken cancellationToken = default)
    {
        var exe = _locator.Locate()
            ?? throw new InvalidOperationException(
                "The recovery engine (rrecov) is not installed with this application. Reinstall the application.");

        var startInfo = new ProcessStartInfo
        {
            FileName = exe,
            WorkingDirectory = workingDirectory ?? Path.GetDirectoryName(exe) ?? Environment.CurrentDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        _logger.LogInformation("engine exec: {Exe} {Args}", exe, string.Join(' ', arguments));

        using var process = new Process { StartInfo = startInfo };
        if (!process.Start())
        {
            throw new InvalidOperationException($"The engine could not start: {exe}");
        }

        var stopwatch = Stopwatch.StartNew();
        var stderrTask = ReadToEndAsync(process.StandardError, cancellationToken);
        var stdoutTask = ReadStdoutStreamingAsync(process, onLine, keepLine, cancellationToken);

        var wasCancelled = false;
        try
        {
            using var timeoutCts = timeoutSeconds > 0
                ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken)
                : null;
            if (timeoutCts is not null)
            {
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));
            }

            var effectiveToken = timeoutCts?.Token ?? cancellationToken;
            using var registration = effectiveToken.Register(static state =>
            {
                try
                {
                    var p = (Process)state!;
                    if (!p.HasExited)
                    {
                        p.Kill(entireProcessTree: true);
                    }
                }
                catch
                {
                    // Raced exit — nothing to do.
                }
            }, process);

            await process.WaitForExitAsync(effectiveToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            wasCancelled = true;
            await KillAndWaitAsync(process).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
        }
        catch (OperationCanceledException)
        {
            await KillAndWaitAsync(process).ConfigureAwait(false);
            throw new TimeoutException($"The engine did not finish within {timeoutSeconds} s and was stopped.");
        }

        var (head, tail, kept) = await stdoutTask.ConfigureAwait(false);
        var stderr = await stderrTask.ConfigureAwait(false);
        stopwatch.Stop();

        _logger.LogInformation(
            "engine exit: code={Code} elapsed={Elapsed:F1}s cancelled={Cancelled} stderr={ErrLen}B",
            process.ExitCode, stopwatch.Elapsed.TotalSeconds, wasCancelled, stderr.Length);

        return new EngineRun
        {
            ExitCode = process.ExitCode,
            HeadLines = head,
            TailLines = tail,
            KeptLines = kept,
            StdErr = stderr,
            Elapsed = stopwatch.Elapsed,
            WasCancelled = wasCancelled,
        };
    }

    private static async Task KillAndWaitAsync(Process process)
    {
        try
        {
            process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);
        }
        catch
        {
            // Best effort.
        }
    }

    private static async Task<string> ReadToEndAsync(StreamReader reader, CancellationToken cancellationToken)
    {
        try
        {
            return await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return string.Empty;
        }
    }

    /// <summary>Streams stdout, splitting on \r and \n; invokes the line callback live; builds bounded head/tail + kept lines.
    /// Decoding: UTF-8 with replacement fallback — the engine's CSV paths are UTF-8
    /// (name_utf8), header lines are ASCII; malformed bytes never throw.</summary>
    private static async Task<(List<string> head, List<string> tail, List<string> kept)> ReadStdoutStreamingAsync(
        Process process, Action<string>? onLine, KeepLinePolicy? keepLine, CancellationToken cancellationToken)
    {
        var head = new List<string>(HeadRetentionLines);
        var tail = new Queue<string>(TailRetentionLines);
        var kept = new List<string>(128);
        var current = new StringBuilder(512);

        var reader = process.StandardOutput; // UTF-8 decoder over the raw stream
        var buffer = new char[8192];

        try
        {
            int read;
            while ((read = await reader.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
            {
                for (var i = 0; i < read; i++)
                {
                    var c = buffer[i];
                    if (c is '\r' or '\n')
                    {
                        if (current.Length > 0)
                        {
                            var line = current.ToString();
                            current.Clear();
                            onLine?.Invoke(line);

                            if (head.Count < HeadRetentionLines)
                            {
                                head.Add(line);
                            }
                            else
                            {
                                tail.Enqueue(line);
                                while (tail.Count > TailRetentionLines) tail.Dequeue();
                            }

                            if (keepLine?.Invoke(line) == true)
                            {
                                kept.Add(line);
                            }
                        }
                    }
                    else
                    {
                        current.Append(c);
                    }
                }
            }

            // Final unterminated line.
            if (current.Length > 0)
            {
                var line = current.ToString();
                onLine?.Invoke(line);
                if (head.Count < HeadRetentionLines)
                {
                    head.Add(line);
                }
                else
                {
                    tail.Enqueue(line);
                }
                if (keepLine?.Invoke(line) == true)
                {
                    kept.Add(line);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // The process is being killed; harvested lines are still returned.
        }
        catch (IOException)
        {
            // Process died mid-write; harvested lines are still returned.
        }

        return (head, new List<string>(tail), kept);
    }
}
