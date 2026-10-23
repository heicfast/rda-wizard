namespace RdaWizard.Core.Models;

/// <summary>
/// Result tree node — one row of the session tree as loaded through
/// <c>rrecov tree --csv</c> (grammar frozen in docs/CORE-CONTRACT.md §13):
/// <code>"full\path",logical_size,origin,deleted_state,confidence,first_extent_offset</code>
/// Confidence and origin are surfaced EXACTLY as the engine returns them —
/// the UI never re-grades (standing rule).
/// </summary>
public sealed record ResultNode
{
    /// <summary>Full path within the session tree, backslash-separated, root-relative.</summary>
    public required string FullPath { get; init; }

    /// <summary>File name (last segment).</summary>
    public required string Name { get; init; }

    /// <summary>Logical size in bytes (0 for directories).</summary>
    public required long LogicalSize { get; init; }

    /// <summary>True when the row is a directory (derived: size 0 + has children / trailing separator per grammar).</summary>
    public required bool IsDirectory { get; init; }

    /// <summary>Engine origin marker.</summary>
    public required ResultOrigin Origin { get; init; }

    /// <summary>Deleted state as returned.</summary>
    public required DeletedState DeletedState { get; init; }

    /// <summary>Engine confidence grade — displayed, never re-graded.</summary>
    public required ConfidenceGrade Confidence { get; init; }

    /// <summary>First extent offset (numeric per grammar) — for metadata display.</summary>
    public long FirstExtentOffset { get; init; }

    /// <summary>Extension without dot, lower-invariant (category mapping).</summary>
    public string Extension => Path.GetExtension(FullPath).TrimStart('.').ToLowerInvariant();

    /// <summary>
    /// Modification timestamp. ALWAYS NULL today: the tree CSV projection does not
    /// carry timestamps (gap G-24 — filed against core). Date filtering and the date
    /// column degrade truthfully until the core exposes times.
    /// </summary>
    public DateTimeOffset? Timestamp { get; init; }

    /// <summary>Depth in the tree (root = 0).</summary>
    public int Depth { get; init; }

    /// <summary>Parent path (null for roots).</summary>
    public string? ParentPath { get; init; }
}

/// <summary>Origin values emitted by the tree grammar (numeric u32: 0..3).</summary>
public enum ResultOrigin
{
    /// <summary>Regular filesystem-structured file (0).</summary>
    Structured = 0,

    /// <summary>Carved by signature scan (1).</summary>
    Carved = 1,

    /// <summary>Found inside a lost partition (2).</summary>
    Lost = 2,

    /// <summary>Engine-constructed virtual node, e.g. a reconstructed grouping root (3).</summary>
    Virtual = 3,
}

/// <summary>Deleted-state field (numeric u32: 0..3) — engine's exact states.</summary>
public enum DeletedState
{
    Active = 0,
    Deleted = 1,

    /// <summary>Partially overwritten (2).</summary>
    PartiallyOverwritten = 2,

    /// <summary>Damaged (3).</summary>
    Damaged = 3,
}

/// <summary>
/// Confidence grades the engine emits (numeric u32 in CSV: 0..4).
/// Labels are honest to users; the engine's exact wording is preserved in
/// <see cref="EngineLabel"/>. The UI never re-grades.
/// </summary>
public enum ConfidenceGrade
{
    /// <summary>Engine grade 0 — not yet graded.</summary>
    Unknown = 0,

    /// <summary>Grade 1 — all extents verified unallocated ($Bitmap).</summary>
    Excellent = 1,

    /// <summary>Grade 2 — some extents reused.</summary>
    PartialRisk = 2,

    /// <summary>Grade 3 — all extents reused; metadata-only salvage.</summary>
    Overwritten = 3,

    /// <summary>Grade 4 — no extents at all (residentless/torn record).</summary>
    MetadataOnly = 4,
}

public static class ConfidenceGradeExtensions
{
    /// <summary>Engine-side label preserved for tooltips/diagnostics (never re-graded).</summary>
    public static string EngineLabel(this ConfidenceGrade grade) => grade switch
    {
        ConfidenceGrade.Excellent => "excellent",
        ConfidenceGrade.PartialRisk => "risk",
        ConfidenceGrade.Overwritten => "overwritten",
        ConfidenceGrade.MetadataOnly => "metadata-only",
        _ => "unknown",
    };

    /// <summary>Grade as parsed from the numeric CSV field; out-of-range degrades to Unknown.</summary>
    public static ConfidenceGrade Parse(long raw) => raw switch
    {
        0 => ConfidenceGrade.Unknown,
        1 => ConfidenceGrade.Excellent,
        2 => ConfidenceGrade.PartialRisk,
        3 => ConfidenceGrade.Overwritten,
        4 => ConfidenceGrade.MetadataOnly,
        _ => ConfidenceGrade.Unknown,
    };
}

/// <summary>File categories for filtering (og parity: the category taxonomy).</summary>
public enum FileCategory
{
    Photos,
    Videos,
    Documents,
    Audio,
    Archives,
    Email,
    Other,
}

public static class FileCategoryExtensions
{
    private static readonly HashSet<string> Photos =
        new(StringComparer.OrdinalIgnoreCase) { "jpg", "jpeg", "png", "gif", "bmp", "tiff", "tif", "webp", "heic", "heif", "raw", "cr2", "cr3", "nef", "arw", "dng", "orf", "rw2", "srw", "raf" };

    private static readonly HashSet<string> Videos =
        new(StringComparer.OrdinalIgnoreCase) { "mp4", "mov", "avi", "mkv", "wmv", "flv", "webm", "m4v", "mpg", "mpeg", "3gp", "mts", "m2ts", "vob" };

    private static readonly HashSet<string> Documents =
        new(StringComparer.OrdinalIgnoreCase) { "doc", "docx", "xls", "xlsx", "ppt", "pptx", "pdf", "txt", "rtf", "odt", "ods", "odp", "md", "csv", "xml", "json", "hwp" };

    private static readonly HashSet<string> Audio =
        new(StringComparer.OrdinalIgnoreCase) { "mp3", "wav", "flac", "aac", "ogg", "wma", "m4a", "aiff", "opus", "mid" };

    private static readonly HashSet<string> Archives =
        new(StringComparer.OrdinalIgnoreCase) { "zip", "rar", "7z", "tar", "gz", "bz2", "xz", "iso", "cab", "arj" };

    private static readonly HashSet<string> Email =
        new(StringComparer.OrdinalIgnoreCase) { "pst", "ost", "eml", "msg", "mbox", "dbx" };

    public static FileCategory CategoryOf(this ResultNode node)
    {
        var ext = node.Extension;
        if (Photos.Contains(ext)) return FileCategory.Photos;
        if (Videos.Contains(ext)) return FileCategory.Videos;
        if (Documents.Contains(ext)) return FileCategory.Documents;
        if (Audio.Contains(ext)) return FileCategory.Audio;
        if (Archives.Contains(ext)) return FileCategory.Archives;
        if (Email.Contains(ext)) return FileCategory.Email;
        return FileCategory.Other;
    }
}
