using RdaWizard.Core.Models;
using RdaWizard.Integration.Parsers;
using Xunit;

namespace RdaWizard.Integration.Tests.Parsers;

/// <summary>
/// Golden fixtures frozen from rrecov tree --csv (CORE-CONTRACT §13 +
/// rrecov.cpp print_tree):
/// <code>"full\path",logical_size,origin,deleted_state,confidence,first_extent_offset</code>
/// origin: 0 structured/1 carved/2 lost/3 virtual; deleted: 0 active/1
/// deleted/2 partially-overwritten/3 damaged; confidence: 0..4.
/// </summary>
public class TreeCsvParserTests
{
    private static readonly string[] GoldenTree =
    {
        @"""Users"",0,0,0,0,0",
        @"""Users\john"",0,0,0,0,0",
        @"""Users\john\report.docx"",1024768,0,1,1,4096",
        @"""Users\john\photo.jpg"",2048,1,1,2,65536",
        @"""Users\john\vanished.txt"",0,0,1,4,0",
        @"""$Lost"",0,0,0,0,0",
        @"""$Lost\found_001.pdf"",1048576,1,1,1,1048576",
    };

    [Fact]
    public void ParsesRows_WithExactEnums()
    {
        var nodes = TreeCsvParser.Parse(GoldenTree);

        var report = Assert.Single(nodes, n => n.Name == "report.docx");
        Assert.Equal(1024768, report.LogicalSize);
        Assert.Equal(ResultOrigin.Structured, report.Origin);
        Assert.Equal(DeletedState.Deleted, report.DeletedState);
        Assert.Equal(ConfidenceGrade.Excellent, report.Confidence);
        Assert.Equal(4096, report.FirstExtentOffset);
        Assert.False(report.IsDirectory);
        Assert.Equal(2, report.Depth); // root=0, Users=0? no: Users(depth0)\john(1)\report(2)
        Assert.Equal("Users\\john", report.ParentPath);
        Assert.Null(report.Timestamp); // G-24 truth
    }

    [Fact]
    public void CarvedOrigin_ParsedAsCarved()
    {
        var nodes = TreeCsvParser.Parse(GoldenTree);
        var photo = Assert.Single(nodes, n => n.Name == "photo.jpg");
        Assert.Equal(ResultOrigin.Carved, photo.Origin);
        Assert.Equal(ConfidenceGrade.PartialRisk, photo.Confidence);
    }

    [Fact]
    public void MetadataOnlyConfidence_Parsed()
    {
        var nodes = TreeCsvParser.Parse(GoldenTree);
        var vanished = Assert.Single(nodes, n => n.Name == "vanished.txt");
        Assert.Equal(ConfidenceGrade.MetadataOnly, vanished.Confidence);
    }

    [Fact]
    public void Directories_DerivedBySuccessor()
    {
        var nodes = TreeCsvParser.Parse(GoldenTree);
        Assert.True(nodes.Single(n => n.FullPath == "Users").IsDirectory);
        Assert.True(nodes.Single(n => n.FullPath == "Users\\john").IsDirectory);
        Assert.False(nodes.Single(n => n.Name == "report.docx").IsDirectory);
    }

    [Fact]
    public void LeafDirectory_WithNoChildren_NotDerived()
    {
        var lines = new[] { @"""empty-dir"",0,0,0,0,0" };
        var nodes = TreeCsvParser.Parse(lines);
        // No successor extends it — cannot be identified as a directory.
        Assert.False(nodes[0].IsDirectory);
    }

    [Fact]
    public void QuotedPathsWithCommasAndUnicode_Survive()
    {
        var lines = new[]
        {
            @"""docs\файл, с запятой.txt"",123,0,1,1,4096",
            @"""docs\日本語ファイル.pdf"",456,0,1,1,8192",
        };
        var nodes = TreeCsvParser.Parse(lines);
        Assert.Equal(2, nodes.Count);
        Assert.Equal("файл, с запятой.txt", nodes[0].Name);
        Assert.Equal("日本語ファイル.pdf", nodes[1].Name);
    }

    [Fact]
    public void MalformedRows_SkippedNotFatal()
    {
        var lines = new[]
        {
            "not-a-csv-row",
            @"""good.txt"",10,0,1,1,4096",
            @"""badenums.txt"",10,99,99,99,4096",
        };
        var nodes = TreeCsvParser.Parse(lines);
        var single = Assert.Single(nodes);
        Assert.Equal("good.txt", single.Name);
    }

    [Fact]
    public void EmptyInput_YieldsEmpty()
    {
        Assert.Empty(TreeCsvParser.Parse(Array.Empty<string>()));
    }

    [Fact]
    public void TryParseRow_RawApi()
    {
        Assert.True(TreeCsvParser.TryParseRow(@"""a\b.txt"",77,1,2,2,12345", out var row));
        Assert.Equal("a\\b.txt", row.Path);
        Assert.Equal(77, row.Size);
        Assert.Equal(1, row.Origin);
        Assert.Equal(2, row.Deleted);
        Assert.Equal(2, row.Confidence);
        Assert.Equal(12345, row.FirstExtent);
    }

    [Fact]
    public void MillionRow_Scale_Performance()
    {
        // Synthetic 1M-node session load (budget: filter < 200 ms is tested
        // separately in Core.Tests; here the parse itself must stay sane).
        const int count = 1_000_000;
        var lines = new string[count];
        for (var i = 0; i < count; i++)
        {
            lines[i] = $@"""folder{i / 100}\file{i}.dat"",{i * 100},0,1,1,{i * 512}";
        }

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var nodes = TreeCsvParser.Parse(lines);
        stopwatch.Stop();

        Assert.Equal(count, nodes.Count);
        // Generous bound for CI variance: parsing 1M rows must stay under 10 s.
        Assert.True(stopwatch.ElapsedMilliseconds < 10_000,
            $"Parse of 1M rows took {stopwatch.ElapsedMilliseconds} ms");
    }
}
