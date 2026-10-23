using RdaWizard.Core.Models;
using RdaWizard.Core.Wizard;
using Xunit;

namespace RdaWizard.Core.Tests.Wizard;

public class ScanModeTests
{
    /// <summary>Flag mapping frozen against docs/CORE-CONTRACT.md §13 (rrecov scan).</summary>
    [Theory]
    [InlineData(ScanMode.Quick, "--quick-only")]
    [InlineData(ScanMode.Deep, "--no-carve")]
    public void CliFlags_MatchFrozenContract(ScanMode mode, string expected)
    {
        var flags = mode.CliFlags();
        Assert.Equal(expected, Assert.Single(flags));
    }

    [Fact]
    public void DeepPlusCarve_HasNoExtraFlags()
    {
        Assert.Empty(ScanMode.DeepPlusCarve.CliFlags());
    }

    [Fact]
    public void Describe_IsNonEmpty_AndHonest()
    {
        foreach (ScanMode mode in Enum.GetValues<ScanMode>())
        {
            var description = mode.Describe();
            Assert.False(string.IsNullOrWhiteSpace(description));
        }
    }

}
