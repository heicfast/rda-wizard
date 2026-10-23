namespace RdaWizard.Core.Models;

/// <summary>Recovery destination + policy — the UI pre-flight and hard gates apply before the engine runs.</summary>
public sealed record RecoveryRequest
{
    /// <summary>Loaded session providing the nodes.</summary>
    public required string SessionPath { get; init; }

    /// <summary>Source physical disk (engine requires --disk; gap G-21: no --volume form).</summary>
    public required int SourceDiskIndex { get; init; }

    /// <summary>Destination directory (UI pre-creates the tree — gap G-08).</summary>
    public required string DestinationDirectory { get; init; }

    /// <summary>Name-substring filter when recovering a filtered subset (--filter).</summary>
    public string? NameFilter { get; init; }

    /// <summary>Deleted-only subset (--deleted-only).</summary>
    public bool DeletedOnly { get; init; }
}

/// <summary>Collision naming policy (our rule: never silently overwrite — AUDIT-UI §5.7).</summary>
public enum CollisionPolicy
{
    /// <summary>Auto-rename as "name (2).ext". Default.</summary>
    Rename,

    /// <summary>Skip items that would collide.</summary>
    Skip,

    /// <summary>Ask per item (queue pauses with a dialog).</summary>
    Ask,
}

/// <summary>
/// Outcome of the engine recover run (exit-code contract: 0 ok, 1 setup
/// failure, 2 partial — errors or hash mismatches, files still on disk).
/// </summary>
public sealed record RecoverySummary
{
    public required long Recovered { get; init; }
    public required long Refused { get; init; }
    public required long Errors { get; init; }
    public required long HashMismatch { get; init; }
    public required RecoveryOutcome Outcome { get; init; }
    public required TimeSpan Elapsed { get; init; }

    /// <summary>Per-file details when the queue tracked them (UI-side state machine).</summary>
    public IReadOnlyList<RecoveredItem> Items { get; init; } = Array.Empty<RecoveredItem>();
}

public enum RecoveryOutcome
{
    /// <summary>Everything requested was written and verified.</summary>
    Success,

    /// <summary>Some items failed or mismatched (engine exit 2).</summary>
    Partial,

    /// <summary>Setup failure (exit 1) — nothing written.</summary>
    Failed,

    /// <summary>User cancelled; completed files remain in the destination.</summary>
    Cancelled,
}

public enum RecoveredItemState
{
    Queued,
    Writing,
    Done,
    Refused,
    Error,
    HashMismatch,
    Skipped,
}

public sealed record RecoveredItem
{
    public required string FullPath { get; init; }
    public required long LogicalSize { get; init; }
    public required RecoveredItemState State { get; init; }
    public string? Diagnostic { get; init; }
}

/// <summary>A saved scan session on disk (.crsf) for the Home import surface.</summary>
public sealed record SessionInfo
{
    public required string Path { get; init; }
    public required DateTimeOffset SavedAtUtc { get; init; }
    public required long SizeBytes { get; init; }
    public string? TargetDescription { get; init; }
    public int? FileCount { get; init; }
}

/// <summary>RawFixer transactional repair state (write path — maximum gravity, consent-gated).</summary>
public enum RepairPhase
{
    /// <summary>Analyze/dry-run only (default engine posture — nothing is written without --apply).</summary>
    DryRun,

    /// <summary>Plan shown; awaiting explicit safety acknowledgment.</summary>
    AwaitingConsent,

    /// <summary>Executing with --apply; undo log written alongside.</summary>
    Applying,

    /// <summary>Done — sectors written + verified.</summary>
    Applied,

    /// <summary>Refused by the engine (REFUSED: reason).</summary>
    Refused,

    /// <summary>Nothing to repair (clean volume).</summary>
    Clean,
}

public sealed record RepairAction
{
    /// <summary>Engine kind string from the plan line.</summary>
    public required string Kind { get; init; }
    public required long Offset { get; init; }
    public required long Length { get; init; }
    public required string Reason { get; init; }
}

public sealed record RepairResult
{
    public required RepairPhase Phase { get; init; }
    public required IReadOnlyList<RepairAction> Actions { get; init; }
    public string? RefusalReason { get; init; }
    public string? UndoLogPath { get; init; }
    public long? SectorsWritten { get; init; }
    public string? EngineText { get; init; }
}
