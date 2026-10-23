namespace RdaWizard.Core.Models;

/// <summary>
/// Scan target — what the engine will actually scan. Volume scans resolve
/// to a slice over their owning disk (engine behavior, gap G-21); disk scans
/// use the whole-disk view. Lost partitions are surfaced as roots inside the
/// result tree of a disk scan ([lost] origin), not as separate targets.
/// </summary>
public sealed record ScanTarget
{
    public required ScanTargetKind Kind { get; init; }

    /// <summary>Physical disk index (both kinds need it — engine's recover requires --disk).</summary>
    public required int DiskIndex { get; init; }

    /// <summary>Drive letter for volume targets; null for whole-disk targets.</summary>
    public char? VolumeLetter { get; init; }

    /// <summary>Human-readable description for the progress/results headers.</summary>
    public required string Description { get; init; }

    /// <summary>Size in bytes of the scanned extent (whole disk or volume extent).</summary>
    public required long SizeBytes { get; init; }
}

public enum ScanTargetKind
{
    WholeDisk,
    Volume,
}

/// <summary>
/// Scan mode — mapped EXACTLY to rrecov flags (docs/CORE-CONTRACT.md §13):
/// <list type="bullet">
/// <item>Quick: <c>--quick-only</c> (deep+carve off)</item>
/// <item>Deep: <c>--no-carve</c></item>
/// <item>DeepPlusCarve: default flags (deep + carve on)</item>
/// </list>
/// <c>--deep</c> (whole_disk view) is added by the integration layer for
/// WholeDisk targets per engine semantics.
/// </summary>
public enum ScanMode
{
    /// <summary>Filesystem-aware quick pass only (MFT/FAT walk). Fastest.</summary>
    Quick,

    /// <summary>Full deep pass without raw carving.</summary>
    Deep,

    /// <summary>Deep pass plus raw carving (signature scan) — the full model.</summary>
    DeepPlusCarve,
}

public static class ScanModeExtensions
{
    /// <summary>The CLI flags this mode maps to (frozen contract; unit-tested).</summary>
    public static IReadOnlyList<string> CliFlags(this ScanMode mode) => mode switch
    {
        ScanMode.Quick => ["--quick-only"],
        ScanMode.Deep => ["--no-carve"],
        ScanMode.DeepPlusCarve => Array.Empty<string>(),
        _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null),
    };

    /// <summary>Truthful one-line description (UI copy, localized at the view layer).</summary>
    public static string Describe(this ScanMode mode) => mode switch
    {
        ScanMode.Quick => "Fast scan of the file tables — best when the disk just lost files.",
        ScanMode.Deep => "Thorough scan without raw carving — good when the filesystem is damaged but present.",
        ScanMode.DeepPlusCarve => "Thorough scan plus signature search for files the filesystem no longer references — the most complete, takes longest.",
        _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null),
    };
}

/// <summary>Full scan configuration as the engine accepts it.</summary>
public sealed record ScanOptions
{
    public required ScanTarget Target { get; init; }

    public required ScanMode Mode { get; init; }

    /// <summary>Worker threads: 0 = engine default; explicit values 1..64 honored (contract D-7).</summary>
    public int Threads { get; init; }

    /// <summary>Name substring filter pushed to the engine (--filter).</summary>
    public string? NameFilter { get; init; }

    /// <summary>--deleted-only: engine-side deleted-file filtering.</summary>
    public bool DeletedOnly { get; init; }

    /// <summary>Where the session (.crsf) is written (--out). Managed by the session service.</summary>
    public required string SessionPath { get; init; }

    /// <summary>Where the detailed stage log is written (--log) — read live for read-error ranges.</summary>
    public required string StageLogPath { get; init; }
}
