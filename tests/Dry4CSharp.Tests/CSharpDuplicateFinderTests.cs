namespace Microsoft.Dry4CSharp.Tests;

public sealed class CSharpDuplicateFinderTests : IDisposable
{
    private readonly List<string> _dirs = [];

    [Fact]
    public void ReportsStructuralDuplicateAcrossFilesWithLineRanges()
    {
        string leftSource = """
            class Left
            {
                int Alpha(int x)
                {
                    int y = x + 1;
                    return y + 2;
                }
            }
            """;
        string rightSource = """
            class Right
            {
                int Beta(int z)
                {
                    int w = z + 9;
                    return w + 8;
                }
            }
            """;
        string dir = NewDir();
        string left = Write(dir, "Left.cs", leftSource);
        string right = Write(dir, "Right.cs", rightSource);

        IReadOnlyList<Candidate> candidates = new CSharpDuplicateFinder()
            .FindDuplicates(new Options([dir], 0.50, 3, 8, "text", false));

        // The two methods (lines 3-7 in each file) are structurally identical after normalization.
        candidates.Should().Contain(candidate =>
            candidate.Left.File == left
            && candidate.Right.File == right
            && candidate.Left.StartLine == 3
            && candidate.Left.EndLine == 7
            && candidate.Right.StartLine == 3
            && candidate.Right.EndLine == 7);
    }

    [Fact]
    public void CollectsCSharpOnlyStructRootsAsCandidates()
    {
        string alphaSource = """
            struct Alpha
            {
                int First;
                int Second;
                int Sum()
                {
                    return First + Second;
                }
            }
            """;
        string betaSource = """
            struct Beta
            {
                int Left;
                int Right;
                int Combine()
                {
                    return Left + Right;
                }
            }
            """;
        string dir = NewDir();
        Write(dir, "AlphaStruct.cs", alphaSource);
        Write(dir, "BetaStruct.cs", betaSource);

        IReadOnlyList<Candidate> candidates = new CSharpDuplicateFinder()
            .FindDuplicates(new Options([dir], 0.80, 3, 8, "text", false));

        // The struct declarations (a C#-only root, lines 1-9) match structurally.
        candidates.Should().Contain(candidate =>
            candidate.Left.StartLine == 1
            && candidate.Left.EndLine == 9
            && candidate.Right.StartLine == 1
            && candidate.Right.EndLine == 9);
    }

    [Fact]
    public void FiltersEntriesShorterThanTheMinimumLineCount()
    {
        string dir = NewDir();
        Write(dir, "One.cs", "class One { int A(int x) { return x + 1; } }\n");
        Write(dir, "Two.cs", "class Two { int B(int y) { return y + 2; } }\n");

        IReadOnlyList<Candidate> candidates = new CSharpDuplicateFinder()
            .FindDuplicates(new Options([dir], 0.80, 3, 1, "text", false));

        // Every entry sits on a single line, below the 3-line minimum.
        candidates.Should().BeEmpty();
    }

    [Fact]
    public void FiltersEntriesBelowTheMinimumNodeCount()
    {
        string oneSource = """
            class One
            {
                int A(int x)
                {
                    return x + 1;
                }
            }
            """;
        string twoSource = """
            class Two
            {
                int B(int y)
                {
                    return y + 2;
                }
            }
            """;
        string dir = NewDir();
        Write(dir, "One.cs", oneSource);
        Write(dir, "Two.cs", twoSource);

        IReadOnlyList<Candidate> candidates = new CSharpDuplicateFinder()
            .FindDuplicates(new Options([dir], 0.80, 3, 1000, "text", false));

        // No entry reaches 1000 nodes, so nothing survives the node-count filter.
        candidates.Should().BeEmpty();
    }

    [Fact]
    public void ExcludesOverlappingEntriesButReportsTheSameContentAcrossFiles()
    {
        const string source = """
            class Wrapper
            {
                void Outer(int seed)
                {
                    int Inner(int a)
                    {
                        int b = a + 1;
                        int c = b + 2;
                        int d = c + 3;
                        return d + 4;
                    }
                }
            }
            """;

        // Same content in two files: the nested entries match across files (never overlapping).
        string crossDir = NewDir();
        Write(crossDir, "A.cs", source);
        Write(crossDir, "B.cs", source);
        IReadOnlyList<Candidate> crossFile = new CSharpDuplicateFinder()
            .FindDuplicates(new Options([crossDir], 0.20, 3, 8, "text", false));
        crossFile.Should().NotBeEmpty();

        // The very same content in a single file: every pair is ancestor/descendant, so all overlap.
        string singleDir = NewDir();
        Write(singleDir, "Only.cs", source);
        IReadOnlyList<Candidate> singleFile = new CSharpDuplicateFinder()
            .FindDuplicates(new Options([singleDir], 0.20, 3, 8, "text", false));
        singleFile.Should().BeEmpty();
    }

    [Fact]
    public void SortsCandidatesByScoreDescendingThenLocation()
    {
        string aSource = """
            class Ka
            {
                int Run(int x)
                {
                    int a = x + 1;
                    return a + 2;
                }
            }
            """;
        string bSource = """
            class Kb
            {
                int Run(int x)
                {
                    int a = x + 1;
                    return a + 2;
                }
            }
            """;
        string cSource = """
            class Kc
            {
                int Run(int x)
                {
                    int a = x + 1;
                    int b = a + 9;
                    return b + 2;
                }
            }
            """;
        string dir = NewDir();
        Write(dir, "A.cs", aSource);
        Write(dir, "B.cs", bSource);
        Write(dir, "C.cs", cSource);

        IReadOnlyList<Candidate> candidates = new CSharpDuplicateFinder()
            .FindDuplicates(new Options([dir], 0.50, 3, 8, "text", false));

        candidates.Should().NotBeEmpty();
        candidates.Select(candidate => candidate.Score).Should().BeInDescendingOrder();

        // The returned order is exactly: score desc, then left file/line, then right file/line.
        List<Candidate> expected =
        [
            .. candidates
                .OrderByDescending(candidate => candidate.Score)
                .ThenBy(candidate => candidate.Left.File, StringComparer.Ordinal)
                .ThenBy(candidate => candidate.Left.StartLine)
                .ThenBy(candidate => candidate.Right.File, StringComparer.Ordinal)
                .ThenBy(candidate => candidate.Right.StartLine),
        ];
        candidates.Should().Equal(expected);

        // Top candidates carry the maximal (identical-copy) score.
        candidates[0].Score.Should().Be(candidates.Max(candidate => candidate.Score));
    }

    [Fact]
    public void CollectsOperatorDeclarationsAsCandidates()
    {
        string leftSource = """
            struct Money
            {
                int Amount;
                public static Money operator +(Money left, Money right)
                {
                    int total = left.Amount + right.Amount;
                    int adjusted = total + 1;
                    return new Money();
                }
            }
            """;
        string rightSource = """
            struct Cash
            {
                int Value;
                public static Cash operator +(Cash first, Cash second)
                {
                    int sum = first.Value + second.Value;
                    int tweaked = sum + 9;
                    return new Cash();
                }
            }
            """;
        string dir = NewDir();
        Write(dir, "Money.cs", leftSource);
        Write(dir, "Cash.cs", rightSource);

        IReadOnlyList<Candidate> candidates = new CSharpDuplicateFinder()
            .FindDuplicates(new Options([dir], 0.80, 3, 8, "text", false));

        // The two `operator +` declarations (OperatorDeclaration roots, lines 4-9 in each file) are
        // structurally identical after normalization and match across files.
        candidates.Should().Contain(candidate =>
            candidate.Left.StartLine == 4
            && candidate.Left.EndLine == 9
            && candidate.Right.StartLine == 4
            && candidate.Right.EndLine == 9);
    }

    [Fact]
    public void CollectsAccessorDeclarationsAsCandidates()
    {
        string leftSource = """
            class Holder
            {
                int Field;
                int Value
                {
                    get
                    {
                        int a = Field + 1;
                        int b = a + 2;
                        return b + 3;
                    }
                }
            }
            """;
        string rightSource = """
            class Store
            {
                int Slot;
                int Amount
                {
                    get
                    {
                        int x = Slot + 9;
                        int y = x + 8;
                        return y + 7;
                    }
                }
            }
            """;
        string dir = NewDir();
        Write(dir, "Holder.cs", leftSource);
        Write(dir, "Store.cs", rightSource);

        IReadOnlyList<Candidate> candidates = new CSharpDuplicateFinder()
            .FindDuplicates(new Options([dir], 0.80, 3, 8, "text", false));

        // The individual `get` accessors (AccessorDeclaration roots, lines 6-11 in each file) are
        // collected as independent candidates and match across files, distinct from the enclosing
        // property (lines 4-12) — proving accessors become their own candidate roots.
        candidates.Should().Contain(candidate =>
            candidate.Left.StartLine == 6
            && candidate.Left.EndLine == 11
            && candidate.Right.StartLine == 6
            && candidate.Right.EndLine == 11);
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

    private static string Write(string dir, string name, string text)
    {
        string file = Path.Combine(dir, name);
        File.WriteAllText(file, text);
        return file;
    }

    private string NewDir()
    {
        string dir = Path.Combine(Path.GetTempPath(), "dry4csharp-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        _dirs.Add(dir);
        return dir;
    }
}
