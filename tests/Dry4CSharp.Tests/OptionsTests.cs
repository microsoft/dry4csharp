namespace Microsoft.Dry4CSharp.Tests;

using System.Globalization;

public class OptionsTests
{
    [Fact]
    public void DefaultsMatchTheJavaDefaults()
    {
        Options options = Options.Defaults();

        options.Paths.Should().Equal("src");
        options.Threshold.Should().Be(0.82);
        options.MinLines.Should().Be(4);
        options.MinNodes.Should().Be(20);
        options.Format.Should().Be("text");
        options.Help.Should().BeFalse();
    }

    [Fact]
    public void ParseWithNoArgumentsDefaultsToSrc()
    {
        Options.Parse().Paths.Should().Equal("src");
    }

    [Fact]
    public void ParseReadsFlagsAndTreatsUnknownTokensAsPaths()
    {
        Options options = Options.Parse(
            "--threshold",
            "0.9",
            "--min-lines",
            "5",
            "--min-nodes",
            "30",
            "--edn",
            "spec");

        options.Paths.Should().Equal("spec");
        options.Threshold.Should().Be(0.9);
        options.MinLines.Should().Be(5);
        options.MinNodes.Should().Be(30);
        options.Format.Should().Be("edn");
    }

    [Fact]
    public void ParseCollectsMultiplePathsInOrder()
    {
        Options.Parse("first", "second").Paths.Should().Equal("first", "second");
    }

    [Theory]
    [InlineData("--help")]
    [InlineData("-h")]
    public void ParseRecognizesHelpFlags(string flag)
    {
        Options.Parse(flag).Help.Should().BeTrue();
    }

    [Fact]
    public void ParseFormatFlagAndTextFlagAdjustFormat()
    {
        Options.Parse("--format", "edn").Format.Should().Be("edn");
        Options.Parse("--edn", "--text").Format.Should().Be("text");
    }

    [Fact]
    public void ParseThrowsWhenAValueIsMissing()
    {
        Action act = () => Options.Parse("--threshold");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ParseUsesInvariantCultureForNumbers()
    {
        CultureInfo previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("de-DE"); // comma is the decimal separator here
        try
        {
            Options.Parse("--threshold", "0.5").Threshold.Should().Be(0.5);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }
}
