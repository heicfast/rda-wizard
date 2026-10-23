using RdaWizard.Core.Models;

namespace RdaWizard.Core.Filters;

/// <summary>
/// The complete results filter (UI-MAP §6 / prompt D: path, type, date
/// range, size range, confidence, extension, name search). Evaluated
/// client-side over the loaded session snapshot because the engine has no
/// query API (gap G-12) — this is the truthful implementation, designed so
/// the evaluator is O(n) with no allocations per predicate.
/// </summary>
public sealed record ResultFilter
{
    /// <summary>Name substring (case-insensitive). Null/whitespace = no filter.</summary>
    public string? NameContains { get; init; }

    /// <summary>Path prefix filter (tree scope). Null = all scopes.</summary>
    public string? PathPrefix { get; init; }

    /// <summary>Category filter; empty set = all categories.</summary>
    public IReadOnlySet<Models.FileCategory> Categories { get; init; } = new HashSet<Models.FileCategory>();

    /// <summary>Inclusive date range on node timestamp; null bounds are open.</summary>
    public DateTimeOffset? ModifiedFrom { get; init; }
    public DateTimeOffset? ModifiedTo { get; init; }

    /// <summary>Inclusive size range in bytes; null bounds are open.</summary>
    public long? MinSize { get; init; }
    public long? MaxSize { get; init; }

    /// <summary>Confidence filter; empty set = all grades.</summary>
    public IReadOnlySet<Models.ConfidenceGrade> Confidences { get; init; } = new HashSet<Models.ConfidenceGrade>();

    /// <summary>Extension filter without dots, lower-invariant; empty set = all.</summary>
    public IReadOnlySet<string> Extensions { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>When true, deleted-state rows only; when false explicitly, alive only; null = both.</summary>
    public bool? DeletedOnly { get; init; }

    /// <summary>Include carved/lost-origin rows (the "raw found" groupings).</summary>
    public bool IncludeRaw { get; init; } = true;

    public static ResultFilter None { get; } = new();

    public bool IsEmpty =>
        string.IsNullOrWhiteSpace(NameContains) &&
        PathPrefix is null &&
        Categories.Count == 0 &&
        ModifiedFrom is null && ModifiedTo is null &&
        MinSize is null && MaxSize is null &&
        Confidences.Count == 0 &&
        Extensions.Count == 0 &&
        DeletedOnly is null &&
        IncludeRaw;
}

public static class FilterEvaluator
{
    /// <summary>Applies the filter to one node. Directories pass category/size/confidence checks vacuously (they group rows).</summary>
    public static bool Passes(in Models.ResultNode node, in ResultFilter filter)
    {
        if (!string.IsNullOrWhiteSpace(filter.NameContains) &&
            !node.Name.Contains(filter.NameContains, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (filter.PathPrefix is { } prefix &&
            !node.FullPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (filter.DeletedOnly is { } deleted)
        {
            // Engine states: Active / Deleted / PartiallyOverwritten / Damaged.
            // "Deleted only" = anything not Active (matches user intent: recoverable-lost rows).
            var isActive = node.DeletedState == Models.DeletedState.Active;
            if (deleted == isActive) return false;
        }

        if (!node.IsDirectory)
        {
            if (!filter.IncludeRaw && node.Origin is Models.ResultOrigin.Carved or Models.ResultOrigin.Lost)
            {
                return false;
            }

            if (filter.Categories.Count > 0 && !filter.Categories.Contains(node.CategoryOf()))
            {
                return false;
            }

            if (filter.Extensions.Count > 0 && !filter.Extensions.Contains(node.Extension))
            {
                return false;
            }

            if (filter.MinSize is { } min && node.LogicalSize < min) return false;
            if (filter.MaxSize is { } max && node.LogicalSize > max) return false;

            if (filter.Confidences.Count > 0 && !filter.Confidences.Contains(node.Confidence)) return false;

            if (node.Timestamp is { } ts)
            {
                if (filter.ModifiedFrom is { } from && ts < from) return false;
                if (filter.ModifiedTo is { } to && ts > to) return false;
            }
            else if (filter.ModifiedFrom is not null || filter.ModifiedTo is not null)
            {
                // Date filter active but the node carries no timestamp: it cannot match a bounded range.
                return false;
            }
        }

        return true;
    }

    /// <summary>Filters a snapshot in one pass; returns the matching nodes in input order.</summary>
    public static IReadOnlyList<Models.ResultNode> Apply(
        IReadOnlyList<Models.ResultNode> nodes, in ResultFilter filter)
    {
        if (filter.IsEmpty) return nodes;

        var matches = new List<Models.ResultNode>(nodes.Count / 4 + 1);
        foreach (var node in nodes)
        {
            if (Passes(node, filter)) matches.Add(node);
        }
        return matches;
    }

    /// <summary>Count-only mode (for header counts without materializing lists).</summary>
    public static long Count(IReadOnlyList<Models.ResultNode> nodes, in ResultFilter filter)
    {
        if (filter.IsEmpty) return nodes.Count;

        long count = 0;
        foreach (var node in nodes)
        {
            if (Passes(node, filter)) count++;
        }
        return count;
    }
}
