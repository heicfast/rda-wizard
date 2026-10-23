namespace RdaWizard.Core.Models;

/// <summary>
/// Scan progress snapshot — ONLY observable truth (ADR-4):
/// the engine streams a percent line during the NTFS walk; other stages are
/// silent until boundaries; bytes_scanned exists only in the carve stage;
/// read-error ranges stream live from the stage log. Everything null here
/// means "the engine is not reporting this right now" and the UI shows
/// elapsed time instead of fabricating a number.
/// </summary>
public sealed record ScanProgressSnapshot
{
    /// <summary>Fraction [0..1] when the engine streams a percentage (NTFS walk), else null.</summary>
    public double? EngineFraction { get; init; }

    /// <summary>Elapsed since scan start.</summary>
    public required TimeSpan Elapsed { get; init; }

    /// <summary>Live read-error ranges seen so far (from the stage log lines).</summary>
    public required IReadOnlyList<ReadErrorRange> ReadErrors { get; init; }

    /// <summary>Read-error ranges' total bytes (bad-sector indicator).</summary>
    public long ReadErrorBytes => ReadErrors.Sum(r => r.LengthBytes);
}

/// <summary>One read-error range as the stage log reports it: "@offset +len (attempts N)".</summary>
public sealed record ReadErrorRange
{
    /// <summary>Byte offset of the failed range.</summary>
    public required long OffsetBytes { get; init; }

    /// <summary>Length in bytes of the failed range.</summary>
    public required long LengthBytes { get; init; }

    /// <summary>Retry attempts the engine made.</summary>
    public required int Attempts { get; init; }
}

/// <summary>Final scan summary — exactly the engine's --json fields.</summary>
public sealed record ScanSummary
{
    public required int Files { get; init; }
    public required int Deleted { get; init; }
    public required int Carved { get; init; }
    public required int LostPartitions { get; init; }
    /// <summary>Carve-stage bytes (the only bytes_scanned the engine reports).</summary>
    public required long BytesScanned { get; init; }
    public required TimeSpan Elapsed { get; init; }

    /// <summary>Session (.crsf) written by the engine (--out). Null when the scan failed or was cancelled.</summary>
    public string? SessionPath { get; init; }
}

/// <summary>Scan stages in engine order (docs/CORE-CONTRACT.md §5.2).</summary>
public enum ScanStage
{
    Idle,
    Quick,
    Deep,
    Carve,
    Done,
}

/// <summary>Live scan-stage observation — only what is actually detectable.</summary>
public enum ScanStageStatus
{
    Pending,
    Running,
    Completed,
}
