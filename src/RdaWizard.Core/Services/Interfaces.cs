namespace RdaWizard.Core.Services;

using Models;

/// <summary>Enumerates disks and volumes through the engine (rrecov list).</summary>
public interface IDeviceEnumerationService
{
    /// <summary>Live enumeration. Never throws for routine failures: failures surface as the result.</summary>
    Task<EnumerationResult> EnumerateAsync(CancellationToken cancellationToken = default);
}

public sealed record EnumerationResult
{
    public required bool Success { get; init; }

    /// <summary>Engine diagnostic when enumeration failed (shown verbatim-classified).</summary>
    public string? Diagnostic { get; init; }

    public required IReadOnlyList<DiskInfo> Disks { get; init; }

    /// <summary>Flattened volumes (each carries its owning disk).</summary>
    public IReadOnlyList<VolumeInfo> Volumes => Disks.SelectMany(d => d.Volumes).ToArray();

    /// <summary>Raw stdout for diagnostics/parity checks.</summary>
    public string? RawOutput { get; init; }
}

/// <summary>Runs scans through the engine, streaming observable progress (truthful only — ADR-4).</summary>
public interface IScanService
{
    /// <summary>
    /// Runs the scan to completion. Progress ticks carry ONLY observable truth
    /// (engine-weighted fraction, elapsed, live read-error ranges). Returns the
    /// final summary; throws with the engine diagnostic on failure; observes
    /// cancellation (results discarded — gap G-07).
    /// </summary>
    Task<ScanSummary> RunScanAsync(ScanOptions options, IProgress<ScanProgressSnapshot>? progress, CancellationToken cancellationToken);

    /// <summary>Transport the engine chose for the last scan ("[transport]" line) — for preflight truth.</summary>
    string? LastTransportLabel { get; }
}

/// <summary>Session lifecycle: list recent, save, load (CRSF only — gap G-11: no .rsf import).</summary>
public interface ISessionService
{
    /// <summary>Loads a session (.crsf) into a queryable snapshot via rrecov tree --csv.</summary>
    Task<SessionLoadResult> LoadAsync(string sessionPath, CancellationToken cancellationToken = default);

    /// <summary>Lists saved sessions in the session folder for the Home import surface.</summary>
    Task<IReadOnlyList<SessionInfo>> ListRecentAsync(CancellationToken cancellationToken = default);

    /// <summary>Default session directory (managed under %LOCALAPPDATA%\RdaWizard\sessions).</summary>
    string SessionsDirectory { get; }
}

public sealed record SessionLoadResult
{
    public required bool Success { get; init; }

    /// <summary>Engine diagnostic on failure (corrupt session etc. — never a partial tree).</summary>
    public string? Diagnostic { get; init; }

    public required LoadedSession? Session { get; init; }
}

/// <summary>The loaded session: flat node snapshot + groupings (ADR-9: client-side queries).</summary>
public sealed record LoadedSession
{
    public required string SessionPath { get; init; }

    public required IReadOnlyList<ResultNode> Nodes { get; init; }

    /// <summary>Root entries (depth 0) for the path tree.</summary>
    public IReadOnlyList<ResultNode> Roots => Nodes.Where(n => n.Depth == 0).ToArray();

    /// <summary>Carved-origin group ("raw found").</summary>
    public IReadOnlyList<ResultNode> CarvedNodes => Nodes.Where(n => !n.IsDirectory && n.Origin == ResultOrigin.Carved).ToArray();

    /// <summary>Lost-partition group.</summary>
    public IReadOnlyList<ResultNode> LostNodes => Nodes.Where(n => n.Origin == ResultOrigin.Lost).ToArray();

    /// <summary>Total file count (non-directory).</summary>
    public long FileCount => Nodes.Count(n => !n.IsDirectory);
}

/// <summary>Recovery execution + destination safety gate.</summary>
public interface IRecoveryService
{
    /// <summary>
    /// Pre-flight: destination free-space check + overlap refusal (hard gate, ADR-7) +
    /// directory pre-creation (G-08). Returns refusal reasons; an empty list = GO.
    /// </summary>
    Task<IReadOnlyList<string>> PreFlightAsync(RecoveryRequest request, CancellationToken cancellationToken = default);

    /// <summary>Runs the recovery; progress ticks per engine progress line + completion summary.</summary>
    Task<RecoverySummary> RecoverAsync(RecoveryRequest request, IProgress<RecoveryProgress>? progress, CancellationToken cancellationToken);

    /// <summary>Generates the human-readable recovery report (UI-owned — gap G-09).</summary>
    Task<string> ExportReportAsync(RecoverySummary summary, RecoveryRequest request, string destinationFile, CancellationToken cancellationToken = default);
}

public sealed record RecoveryProgress
{
    /// <summary>Files written so far (engine progress line).</summary>
    public required long FilesDone { get; init; }

    /// <summary>Bytes written so far when the engine reports them; else null.</summary>
    public long? BytesDone { get; init; }

    public required TimeSpan Elapsed { get; init; }
}

/// <summary>Disk health via rrecov health (report-only fields — gap G-15/G-16 degrade truthfully).</summary>
public interface IHealthService
{
    Task<HealthReport> GetHealthAsync(int diskIndex, CancellationToken cancellationToken = default);
}

/// <summary>Driver presence/version detection (TRANSPORT-CONTRACT §9 — UI-side probe, no IOCTLs).</summary>
public interface IDriverStatusService
{
    Task<DriverStatus> ProbeAsync(CancellationToken cancellationToken = default);
}

/// <summary>RawFixer transactional repair (dry-run default; --apply gated by explicit consent).</summary>
public interface IRepairService
{
    /// <summary>Analyze/dry-run: nothing is written (engine default posture).</summary>
    Task<RepairResult> AnalyzeAsync(int diskIndex, int? partitionIndex, CancellationToken cancellationToken = default);

    /// <summary>Execute with --apply. Call ONLY after the user acknowledged the safety gate.</summary>
    Task<RepairResult> ApplyAsync(int diskIndex, int? partitionIndex, string undoLogPath, CancellationToken cancellationToken = default);

    /// <summary>Replay an undo log.</summary>
    Task<RepairResult> UndoAsync(string undoLogPath, CancellationToken cancellationToken = default);
}

/// <summary>
/// Preview pipeline (ADR-8): bounded workers over temp-materialized content.
/// Every failure carries a diagnostic reason — never a bare "cannot preview".
/// </summary>
public interface IPreviewService
{
    /// <summary>Previews one node; the task is bounded (time+size caps) and crash-isolated.</summary>
    Task<PreviewResult> PreviewAsync(ResultNode node, LoadedSession session, CancellationToken cancellationToken = default);

    /// <summary>Materializes node content to the preview temp area via the engine recovery path.</summary>
    Task<string> MaterializeForPreviewAsync(ResultNode node, LoadedSession session, CancellationToken cancellationToken = default);

    /// <summary>Releases preview temp artifacts older than the retention window.</summary>
    Task CleanupAsync(CancellationToken cancellationToken = default);
}

public enum PreviewKind
{
    Image,
    Text,
    Hex,
    Pdf,
    AudioVideo,
    OfficeDocument,
    MetadataOnly,
}

public sealed record PreviewResult
{
    public required PreviewKind Kind { get; init; }

    /// <summary>True when content decoded; false + Diagnostic explains why not (never generic).</summary>
    public required bool Success { get; init; }

    public string? Diagnostic { get; init; }

    /// <summary>Local materialized file path (image/pdf/av/office/text/hex source).</summary>
    public string? LocalPath { get; init; }

    /// <summary>Decoded size cap applied, when relevant.</summary>
    public long? CappedBytes { get; init; }

    /// <summary>Detected text encoding for text previews.</summary>
    public string? Encoding { get; init; }

    /// <summary>Media metadata key-values when the kind is AudioVideo.</summary>
    public IReadOnlyDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();
}

/// <summary>Dialog surfaces owned by the shell (alerts, confirmations, file/folder pickers).</summary>
public interface IDialogService
{
    Task<DialogResult> ConfirmAsync(string title, string message, string confirmLabel, bool destructive = false);
    Task AlertAsync(string title, string message);
    Task<string?> PickFolderAsync(string title);
    Task<string?> PickOpenFileAsync(string title, string filter);
}

public enum DialogResult
{
    Confirmed,
    Cancelled,
}

/// <summary>Clock abstraction for deterministic tests.</summary>
public interface IClock
{
    DateTimeOffset UtcNow { get; }
    long TimestampFrequency { get; }
    long Timestamp { get; }
}
