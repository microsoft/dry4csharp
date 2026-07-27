namespace Microsoft.Dry4CSharp.Tests;

using System.Globalization;

// =============================================================================================
// FIDELITY CONTRACT (guardrail #8): a faithful 1:1 C# counterpart for every dry4java test.
//
// Source of truth: ../dry4java/src/test/java/dry4java/JavaDuplicateFinderTest.java (9 @Test methods).
// Each method below is PascalCased to mirror its Java original and carries a comment naming it.
//
//   #  Java test (JavaDuplicateFinderTest)                       C# counterpart (this file)
//   -  --------------------------------------------------------  ---------------------------------------------------------
//   1  reportsStructuralDuplicateCandidatesWithFileAndLineRanges ReportsStructuralDuplicateCandidatesWithFileAndLineRanges
//   2  matchesRecordsWithDifferentNamesAndLiteralValues          MatchesRecordsWithDifferentNamesAndLiteralValues
//   3  matchesEnumsAndConstantsStructurally                      MatchesEnumsAndConstantsStructurally
//   4  filtersCandidatesShorterThanTheMinimumLineCount           FiltersCandidatesShorterThanTheMinimumLineCount
//   5  parsesCommandLineOptionsAndPaths                          ParsesCommandLineOptionsAndPaths
//   6  defaultsToSrcWhenNoPathsAreProvided                       DefaultsToSrcWhenNoPathsAreProvided
//   7  formatsTextOutputWithLineRanges                           FormatsTextOutputWithLineRanges
//   8  printsClearMessageWhenNoTextCandidatesExist               PrintsClearMessageWhenNoTextCandidatesExist
//   9  printsEdn                                                 PrintsEdn
//
// Deliberate, approved departures preserved here (docs/decisions.md):
//   * A2 — the structural-duplicate tests assert the C# *sample's actual* line ranges (observed by
//     running the finder), not dry4java's literal Java-source ranges (e.g. #1 lands at 6-10, not 4-7).
//   * R2 — #3 has no param-for-param port (C# enums cannot carry ctors/fields); it verifies the same
//     *intent* via option (a): a real C# enum with several members and relaxed thresholds so both the
//     EnumDeclaration and EnumMember (constant) roots qualify and match structurally across files.
//   * .java sample/expected strings become .cs (e.g. a.java -> a.cs).
// =============================================================================================
[Collection("ConsoleCapture")]
public sealed class CSharpDuplicateFinderParityTests : IDisposable
{
    private readonly List<string> _dirs = [];

    // Java: reportsStructuralDuplicateCandidatesWithFileAndLineRanges
    // Two classes with a structurally-identical method (Java stream().filter/mapToInt -> C# LINQ
    // Where/Select/Sum), differing only in names and literals. Per A2 the assertion pins the C#
    // sample's real range (method at lines 6-10, observed by running the finder) rather than Java's 4-7.
    [Fact]
    public void ReportsStructuralDuplicateCandidatesWithFileAndLineRanges()
    {
        string leftSource = """
            using System.Collections.Generic;
            using System.Linq;

            class Left
            {
                int Alpha(List<int> xs)
                {
                    var ys = xs.Where(x => x % 2 == 1).ToList();
                    return ys.Select(x => x + 1).Sum();
                }
            }
            """;
        string rightSource = """
            using System.Collections.Generic;
            using System.Linq;

            class Right
            {
                int Beta(List<int> items)
                {
                    var kept = items.Where(item => item % 2 == 0).ToList();
                    return kept.Select(item => item - 1).Sum();
                }
            }
            """;
        string dir = NewDir();
        string left = Write(dir, "Left.cs", leftSource);
        string right = Write(dir, "Right.cs", rightSource);

        IReadOnlyList<Candidate> candidates = new CSharpDuplicateFinder()
            .FindDuplicates(new Options([dir], 0.50, 3, 8, "text", false));

        Candidate candidate = candidates
            .Where(each => each.Left.File == left)
            .Where(each => each.Right.File == right)
            .Where(each => each.Left.StartLine == 6)
            .Where(each => each.Right.StartLine == 6)
            .First();
        candidate.Left.StartLine.Should().Be(6);
        candidate.Left.EndLine.Should().Be(10);
        candidate.Right.StartLine.Should().Be(6);
        candidate.Right.EndLine.Should().Be(10);
    }

    // Java: matchesRecordsWithDifferentNamesAndLiteralValues
    // Two records (Invoice/Receipt) with a boolean method differing only in names and literal values.
    [Fact]
    public void MatchesRecordsWithDifferentNamesAndLiteralValues()
    {
        string oneSource = """
            record Invoice(string Id, int Amount)
            {
                bool Payable()
                {
                    return Id != null && Amount > 0;
                }
            }
            """;
        string twoSource = """
            record Receipt(string Code, int Total)
            {
                bool Closed()
                {
                    return Code != null && Total > 10;
                }
            }
            """;
        string dir = NewDir();
        Write(dir, "One.cs", oneSource);
        Write(dir, "Two.cs", twoSource);

        IReadOnlyList<Candidate> candidates = new CSharpDuplicateFinder()
            .FindDuplicates(new Options([dir], 0.80, 3, 8, "text", false));

        candidates.Should().Contain(candidate =>
            candidate.Left.File.EndsWith("One.cs", StringComparison.Ordinal)
            && candidate.Right.File.EndsWith("Two.cs", StringComparison.Ordinal));
    }

    // Java: matchesEnumsAndConstantsStructurally
    // R2 option (a): the Java sample (enum constants with ctor args + private field + constructor) has
    // no param-for-param C# port because C# enums cannot declare constructors, fields, or methods. This
    // verifies the same *intent* — enum and constant roots match structurally across files — with a real
    // C# enum (several members, differing names and literal values). Thresholds stay relaxed on lines
    // (min-lines 1) and nodes (min-nodes 2) so the single-line EnumMember (constant) roots qualify, but
    // the score bar is a strict 0.80: both the EnumDeclaration roots (lines 1-7) and the EnumMember roots
    // normalize their names/literals away and match *exactly* (score 1.0), so nothing hinges on a loose
    // threshold.
    [Fact]
    public void MatchesEnumsAndConstantsStructurally()
    {
        string oneSource = """
            enum One
            {
                Ready = 1,
                Done = 2,
                Waiting = 3,
                Failed = 4,
            }
            """;
        string twoSource = """
            enum Two
            {
                Open = 10,
                Closed = 20,
                Pending = 30,
                Blocked = 40,
            }
            """;
        string dir = NewDir();
        Write(dir, "One.cs", oneSource);
        Write(dir, "Two.cs", twoSource);

        IReadOnlyList<Candidate> candidates = new CSharpDuplicateFinder()
            .FindDuplicates(new Options([dir], 0.80, 1, 2, "text", false));

        // Java parity: One and Two produce a cross-file match.
        candidates.Should().Contain(candidate =>
            candidate.Left.File.EndsWith("One.cs", StringComparison.Ordinal)
            && candidate.Right.File.EndsWith("Two.cs", StringComparison.Ordinal));

        // The two enum declarations themselves match (EnumDeclaration roots, lines 1-7).
        candidates.Should().Contain(candidate =>
            candidate.Left.File.EndsWith("One.cs", StringComparison.Ordinal)
            && candidate.Left.StartLine == 1
            && candidate.Left.EndLine == 7
            && candidate.Right.File.EndsWith("Two.cs", StringComparison.Ordinal)
            && candidate.Right.StartLine == 1
            && candidate.Right.EndLine == 7);

        // ...and individual enum members (EnumMember/constant roots, each on a single line) match too.
        candidates.Should().Contain(candidate =>
            candidate.Left.File.EndsWith("One.cs", StringComparison.Ordinal)
            && candidate.Right.File.EndsWith("Two.cs", StringComparison.Ordinal)
            && candidate.Left.StartLine == candidate.Left.EndLine
            && candidate.Right.StartLine == candidate.Right.EndLine);
    }

    // Java: filtersCandidatesShorterThanTheMinimumLineCount
    // Two single-line candidates below the 3-line minimum are filtered out entirely.
    [Fact]
    public void FiltersCandidatesShorterThanTheMinimumLineCount()
    {
        string dir = NewDir();
        Write(dir, "One.cs", "class One { int A(int x) { return x + 1; } }\n");
        Write(dir, "Two.cs", "class Two { int B(int y) { return y + 2; } }\n");

        IReadOnlyList<Candidate> candidates = new CSharpDuplicateFinder()
            .FindDuplicates(new Options([dir], 0.80, 3, 1, "text", false));

        candidates.Should().BeEmpty();
    }

    // Java: parsesCommandLineOptionsAndPaths
    [Fact]
    public void ParsesCommandLineOptionsAndPaths()
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

    // Java: defaultsToSrcWhenNoPathsAreProvided
    [Fact]
    public void DefaultsToSrcWhenNoPathsAreProvided()
    {
        Options.Parse().Paths.Should().Equal("src");
    }

    // Java: formatsTextOutputWithLineRanges (.java expected strings become .cs)
    [Fact]
    public void FormatsTextOutputWithLineRanges()
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

    // Java: printsClearMessageWhenNoTextCandidatesExist (System.out capture -> Console.Out capture)
    [Fact]
    public void PrintsClearMessageWhenNoTextCandidatesExist()
    {
        CaptureOut(() => Dry4CSharp.PrintText([]))
            .Should().Be("No duplicate candidates found.\n");
    }

    // Java: printsEdn
    [Fact]
    public void PrintsEdn()
    {
        Dry4CSharp.ToEdn([]).Should().Be("{:candidates []}");
    }

    public void Dispose()
    {
        foreach (string dir in _dirs)
        {
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, recursive: true);
            }
        }

        GC.SuppressFinalize(this);
    }

    private static string CaptureOut(Action action)
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

    private static string Write(string dir, string name, string text)
    {
        string file = Path.Combine(dir, name);
        File.WriteAllText(file, text);
        return file;
    }

    private string NewDir()
    {
        string dir = Path.Combine(Path.GetTempPath(), "dry4csharp-parity-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        _dirs.Add(dir);
        return dir;
    }
}

// Serializes the two console-capturing test classes (this file's parity tests + OutputTests) so their
// Console.SetOut redirections never overlap. Test classes in the same xUnit collection do not run in
// parallel with each other; without this, capturing tests in different classes could corrupt each
// other's captured output (a flaky, timing-sensitive failure — see guardrail on avoiding those).
[CollectionDefinition("ConsoleCapture")]
public sealed class ConsoleCapture
{
}
