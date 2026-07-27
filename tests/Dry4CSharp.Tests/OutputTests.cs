namespace Microsoft.Dry4CSharp.Tests;

using System.Globalization;

// Console.SetOut mutates process-global state; keeping every capturing test in a single class makes
// xUnit run them sequentially (tests within one class never run in parallel), avoiding interference.
// The shared "ConsoleCapture" collection extends that guarantee across class boundaries so these tests
// never run in parallel with the console-capturing parity test (CSharpDuplicateFinderParityTests).
[Collection("ConsoleCapture")]
public sealed class OutputTests
{
    [Fact]
    public void FormatCandidateRendersScoreAndLineRanges()
    {
        Candidate candidate = new(
            0.875,
            new Location("a.cs", 10, 14),
            new Location("b.cs", 20, 24),
            88,
            91);

        Dry4CSharp.FormatCandidate(candidate)
            .Should().Be("DUPLICATE score=0.88\n  a.cs:10-14\n  b.cs:20-24");
    }

    [Fact]
    public void FormatCandidateRoundsScoreHalfUpLikeJava()
    {
        // 0.125 is exactly representable, so the third decimal is a true midpoint. Java's
        // String.format("%.2f", 0.125) rounds HALF_UP to "0.13"; .NET's default banker's rounding
        // would yield "0.12". FormatCandidate must match Java (0.13).
        Candidate candidate = new(
            0.125,
            new Location("a.cs", 1, 2),
            new Location("b.cs", 3, 4),
            8,
            9);

        Dry4CSharp.FormatCandidate(candidate)
            .Should().Be("DUPLICATE score=0.13\n  a.cs:1-2\n  b.cs:3-4");
    }

    [Fact]
    public void PrintTextWritesClearMessageWhenNoCandidatesExist()
    {
        CaptureOutput(() => Dry4CSharp.PrintText([]))
            .Should().Be("No duplicate candidates found.\n");
    }

    [Fact]
    public void PrintTextSeparatesCandidateBlocksWithABlankLine()
    {
        Candidate first = new(
            0.9,
            new Location("a.cs", 1, 5),
            new Location("b.cs", 11, 15),
            30,
            31);
        Candidate second = new(
            0.5,
            new Location("c.cs", 2, 6),
            new Location("d.cs", 12, 16),
            20,
            21);

        CaptureOutput(() => Dry4CSharp.PrintText([first, second]))
            .Should().Be(
                "DUPLICATE score=0.90\n  a.cs:1-5\n  b.cs:11-15\n"
                + "\n"
                + "DUPLICATE score=0.50\n  c.cs:2-6\n  d.cs:12-16\n");
    }

    [Fact]
    public void ToEdnReturnsEmptyLiteralWhenNoCandidatesExist()
    {
        Dry4CSharp.ToEdn([]).Should().Be("{:candidates []}");
    }

    [Fact]
    public void ToEdnRendersPopulatedCandidateWithExactLayout()
    {
        Candidate candidate = new(
            0.875,
            new Location("a.cs", 10, 14),
            new Location("b.cs", 20, 24),
            88,
            91);

        Dry4CSharp.ToEdn([candidate]).Should().Be(
            "{:candidates\n"
            + " [{:score 0.875\n"
            + "   :left {:file \"a.cs\", :start-line 10, :end-line 14}\n"
            + "   :right {:file \"b.cs\", :start-line 20, :end-line 24}\n"
            + "   :left-nodes 88\n"
            + "   :right-nodes 91}]}");
    }

    [Fact]
    public void ToEdnEscapesBackslashBeforeQuoteInFilePaths()
    {
        Candidate candidate = new(
            0.5,
            new Location("dir\\a\".cs", 1, 2),
            new Location("b.cs", 3, 4),
            8,
            9);

        // Java escape order: replace "\" first, then '"'; so "\" -> "\\" and '"' -> '\"'.
        Dry4CSharp.ToEdn([candidate])
            .Should().Contain(":file \"dir\\\\a\\\".cs\"");
    }

    private static string CaptureOutput(Action action)
    {
        TextWriter original = Console.Out;
        using StringWriter captured = new(CultureInfo.InvariantCulture);
        Console.SetOut(captured);
        try
        {
            action();
        }
        finally
        {
            Console.SetOut(original);
        }

        return captured.ToString();
    }
}
