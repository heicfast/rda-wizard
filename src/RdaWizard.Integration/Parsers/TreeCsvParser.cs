using System.Globalization;
using RdaWizard.Core.Models;

namespace RdaWizard.Integration.Parsers;

/// <summary>
/// Parses <c>rrecov tree --csv</c> output. Grammar (frozen):
/// <code>"full\path",logical_size,origin,deleted_state,confidence,first_extent_offset</code>
/// — quoted UTF-8 path (may contain commas), then five numeric fields.
/// Origin: 0 structured / 1 carved / 2 lost / 3 virtual.
/// DeletedState: 0 active / 1 deleted / 2 partially-overwritten / 3 damaged.
/// Confidence: 0 unknown / 1 excellent / 2 partial-risk / 3 overwritten / 4 metadata-only.
/// Directories: rows whose size is 0 AND which appear as a path prefix of
/// other rows (the engine prints directories with attributes 0x10; the CSV
/// projection does not flag them directly — derived truthfully).
/// </summary>
public static class TreeCsvParser
{
    /// <summary>Stream-parses CSV lines into nodes with directory derivation.
    /// A path is a directory iff some other row extends it ("dir\child") —
    /// verified by sorting all paths and checking neighbors (O(n log n)).</summary>
    public static IReadOnlyList<ResultNode> Parse(IEnumerable<string> lines)
    {
        var rowsByPath = new Dictionary<string, RawRow>(4096, StringComparer.OrdinalIgnoreCase);
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            if (TryParseRow(line, out var row))
            {
                rowsByPath[row.Path] = row; // duplicate paths (shouldn't occur) — last wins
            }
        }

        var paths = rowsByPath.Keys.ToArray();
        Array.Sort(paths, StringComparer.OrdinalIgnoreCase);

        var nodes = new ResultNode[paths.Length];
        for (var i = 0; i < paths.Length; i++)
        {
            var row = rowsByPath[paths[i]];
            var isDirectory = IsDirectoryBySuccessor(paths, i);
            nodes[i] = BuildNode(row, isDirectory);
        }
        return nodes;
    }

    /// <summary>After sorting, row i is a directory iff row i+1 starts with "row_i_path\".</summary>
    private static bool IsDirectoryBySuccessor(string[] sortedPaths, int i)
    {
        if (i + 1 >= sortedPaths.Length) return false;
        var next = sortedPaths[i + 1];
        var current = sortedPaths[i];
        return next.Length > current.Length &&
               next.StartsWith(current, StringComparison.OrdinalIgnoreCase) &&
               next[current.Length] == '\\';
    }

    /// <summary>Parses one CSV row (single-line API for incremental streaming).</summary>
    public static bool TryParseRow(string line, out RawRow row)
    {
        row = default;
        if (line.Length == 0 || line[0] != '"') return false;

        var pathEnd = line.IndexOf('"', 1);
        if (pathEnd <= 1) return false;

        var path = line[1..pathEnd];
        var rest = line[(pathEnd + 1)..].TrimStart(',');
        var fields = rest.Split(',');

        if (fields.Length < 5) return false;
        if (!long.TryParse(fields[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var size)) return false;
        if (!TryParseEnumField(fields[1], 3, out var origin)) return false;
        if (!TryParseEnumField(fields[2], 3, out var deleted)) return false;
        if (!TryParseEnumField(fields[3], 4, out var confidence)) return false;
        if (!long.TryParse(fields[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out var extent)) return false;

        row = new RawRow(path, size, origin, deleted, confidence, extent);
        return true;
    }

    /// <summary>Raw parsed row before structure derivation.</summary>
    public readonly record struct RawRow(
        string Path,
        long Size,
        int Origin,
        int Deleted,
        int Confidence,
        long FirstExtent);

    private static ResultNode BuildNode(in RawRow row, bool isDirectory)
    {
        var name = row.Path.Contains('\\')
            ? row.Path[(row.Path.LastIndexOf('\\') + 1)..]
            : row.Path;
        var parentPath = row.Path.Contains('\\')
            ? row.Path[..row.Path.LastIndexOf('\\')]
            : null;
        var depth = 0;
        foreach (var c in row.Path)
        {
            if (c == '\\') depth++;
        }

        return new ResultNode
        {
            FullPath = row.Path,
            Name = name,
            LogicalSize = row.Size,
            IsDirectory = isDirectory,
            Origin = (ResultOrigin)row.Origin,
            DeletedState = (DeletedState)row.Deleted,
            Confidence = (ConfidenceGrade)row.Confidence,
            FirstExtentOffset = row.FirstExtent,
            Timestamp = null, // G-24: CSV projection carries no timestamps.
            Depth = depth,
            ParentPath = parentPath,
        };
    }

    private static bool TryParseEnumField(string field, int max, out int value)
    {
        value = -1;
        if (!int.TryParse(field, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)) return false;
        if (parsed < 0 || parsed > max) return false;
        value = parsed;
        return true;
    }
}
