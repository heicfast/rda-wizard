using RdaWizard.Core.Filters;
using RdaWizard.Core.Models;
using Xunit;

namespace RdaWizard.Core.Tests.Filters;

public class FilterEvaluatorTests
{
    private static ResultNode File(
        string path,
        long size = 1024,
        ConfidenceGrade confidence = ConfidenceGrade.Excellent,
        DeletedState deleted = DeletedState.Active,
        ResultOrigin origin = ResultOrigin.Structured) => new()
        {
            FullPath = path,
            Name = path.Contains('\\') ? path[(path.LastIndexOf('\\') + 1)..] : path,
            LogicalSize = size,
            IsDirectory = false,
            Origin = origin,
            DeletedState = deleted,
            Confidence = confidence,
            FirstExtentOffset = 4096,
            Depth = path.Count(c => c == '\\'),
            ParentPath = path.Contains('\\') ? path[..path.LastIndexOf('\\')] : null,
        };

    private static ResultNode Dir(string path) => new()
    {
        FullPath = path,
        Name = path.Contains('\\') ? path[(path.LastIndexOf('\\') + 1)..] : path,
        LogicalSize = 0,
        IsDirectory = true,
        Origin = ResultOrigin.Structured,
        DeletedState = DeletedState.Active,
        Confidence = ConfidenceGrade.Unknown,
        FirstExtentOffset = 0,
        Depth = path.Count(c => c == '\\'),
        ParentPath = path.Contains('\\') ? path[..path.LastIndexOf('\\')] : null,
    };

    [Fact]
    public void EmptyFilter_PassesEverything()
    {
        var nodes = new[] { File("a.txt"), Dir("photos"), File("photos\\b.jpg") };
        var result = FilterEvaluator.Apply(nodes, ResultFilter.None);
        Assert.Same(nodes, result);
    }

    [Fact]
    public void NameContains_IsCaseInsensitive()
    {
        var nodes = new[] { File("Report.PDF"), File("photo.jpg") };
        var result = FilterEvaluator.Apply(nodes, new ResultFilter { NameContains = "report" });
        var single = Assert.Single(result);
        Assert.Equal("Report.PDF", single.Name);
    }

    [Fact]
    public void Categories_PhotosMatchesExtensions()
    {
        var nodes = new[]
        {
            File("a.jpg"), File("b.docx"), File("c.mp4"), File("d.png"), File("e.txt"),
        };
        var result = FilterEvaluator.Apply(nodes, new ResultFilter
        {
            Categories = new HashSet<FileCategory> { FileCategory.Photos },
        });
        Assert.Equal(2, result.Count);
        Assert.All(result, n => Assert.Equal(FileCategory.Photos, n.CategoryOf()));
    }

    [Fact]
    public void SizeRange_IsInclusive()
    {
        var nodes = new[]
        {
            File("a.bin", size: 100),
            File("b.bin", size: 500),
            File("c.bin", size: 1000),
        };
        var result = FilterEvaluator.Apply(nodes, new ResultFilter { MinSize = 100, MaxSize = 1000 });
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public void SizeRange_ExcludesOutOfRange()
    {
        var nodes = new[]
        {
            File("a.bin", size: 99),
            File("b.bin", size: 1001),
            File("c.bin", size: 500),
        };
        var result = FilterEvaluator.Apply(nodes, new ResultFilter { MinSize = 100, MaxSize = 1000 });
        var single = Assert.Single(result);
        Assert.Equal("c.bin", single.Name);
    }

    [Fact]
    public void ConfidenceFilter_MatchesExactGrades()
    {
        var nodes = new[]
        {
            File("a", confidence: ConfidenceGrade.Excellent),
            File("b", confidence: ConfidenceGrade.PartialRisk),
            File("c", confidence: ConfidenceGrade.Overwritten),
        };
        var result = FilterEvaluator.Apply(nodes, new ResultFilter
        {
            Confidences = new HashSet<ConfidenceGrade> { ConfidenceGrade.Excellent, ConfidenceGrade.PartialRisk },
        });
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void DeletedOnly_IncludesPartiallyOverwrittenAndDamaged()
    {
        // Engine semantics: "deleted" = anything not Active.
        var nodes = new[]
        {
            File("a", deleted: DeletedState.Active),
            File("b", deleted: DeletedState.Deleted),
            File("c", deleted: DeletedState.PartiallyOverwritten),
            File("d", deleted: DeletedState.Damaged),
        };
        var result = FilterEvaluator.Apply(nodes, new ResultFilter { DeletedOnly = true });
        Assert.Equal(3, result.Count);
        Assert.DoesNotContain(result, n => n.Name == "a");
    }

    [Fact]
    public void IncludeRawFalse_HidesCarvedAndLost()
    {
        var nodes = new[]
        {
            File("a", origin: ResultOrigin.Structured),
            File("b", origin: ResultOrigin.Carved),
            File("c", origin: ResultOrigin.Lost),
        };
        var result = FilterEvaluator.Apply(nodes, new ResultFilter { IncludeRaw = false });
        var single = Assert.Single(result);
        Assert.Equal("a", single.Name);
    }

    [Fact]
    public void ExtensionFilter_MatchesLowerInvariant()
    {
        var nodes = new[] { File("a.JPG"), File("b.jpg"), File("c.png") };
        var result = FilterEvaluator.Apply(nodes, new ResultFilter
        {
            Extensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "jpg" },
        });
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void DateFilter_RejectsNodesWithoutTimestamp()
    {
        // G-24: nodes carry no timestamps; a bounded date range must not match.
        var nodes = new[] { File("a"), File("b") };
        var result = FilterEvaluator.Apply(nodes, new ResultFilter
        {
            ModifiedFrom = DateTimeOffset.UtcNow.AddDays(-7),
        });
        Assert.Empty(result);
    }

    [Fact]
    public void PathPrefix_MatchesScope()
    {
        var nodes = new[]
        {
            File("photos\\2019\\a.jpg"),
            File("docs\\b.pdf"),
            File("photos\\2020\\c.jpg"),
        };
        var result = FilterEvaluator.Apply(nodes, new ResultFilter { PathPrefix = "photos" });
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void Directories_PassCategoryFilters_Vacuously()
    {
        var nodes = new[] { Dir("photos"), File("a.jpg") };
        var result = FilterEvaluator.Apply(nodes, new ResultFilter
        {
            Categories = new HashSet<FileCategory> { FileCategory.Videos },
        });
        // Directory survives (grouping node); the jpg file does not.
        var single = Assert.Single(result);
        Assert.True(single.IsDirectory);
    }

    [Fact]
    public void Count_MatchesApply()
    {
        var nodes = new[]
        {
            File("a.jpg"), File("b.jpg"), File("c.png"), Dir("d"),
        };
        var filter = new ResultFilter
        {
            Extensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "jpg" },
        };
        Assert.Equal(FilterEvaluator.Apply(nodes, filter).Count, FilterEvaluator.Count(nodes, filter));
    }

    [Fact]
    public void CategoryOf_CoversEmailAndArchives()
    {
        Assert.Equal(FileCategory.Email, File("inbox.pst").CategoryOf());
        Assert.Equal(FileCategory.Email, File("mail.eml").CategoryOf());
        Assert.Equal(FileCategory.Archives, File("backup.7z").CategoryOf());
        Assert.Equal(FileCategory.Archives, File("disk.iso").CategoryOf());
        Assert.Equal(FileCategory.Audio, File("song.flac").CategoryOf());
        Assert.Equal(FileCategory.Documents, File("notes.md").CategoryOf());
        Assert.Equal(FileCategory.Other, File("unknown.xyz").CategoryOf());
    }

    [Fact]
    public void IsEmpty_DetectsActiveFilters()
    {
        Assert.True(ResultFilter.None.IsEmpty);
        Assert.False(new ResultFilter { NameContains = "x" }.IsEmpty);
        Assert.False(new ResultFilter { DeletedOnly = true }.IsEmpty);
        Assert.False(new ResultFilter { IncludeRaw = false }.IsEmpty);
        Assert.False(new ResultFilter { MinSize = 1 }.IsEmpty);
    }
}
