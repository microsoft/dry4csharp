namespace Microsoft.Dry4CSharp;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

/// <summary>
/// Scans C# sources, normalizes each candidate declaration into structural fingerprints, and reports
/// pairs whose Jaccard similarity meets the threshold. Faithful port of <c>dry4java</c>'s
/// <c>JavaDuplicateFinder</c> (see <c>docs/decisions.md</c>).
/// </summary>
public sealed class CSharpDuplicateFinder
{
    private readonly CSharpNormalizer _normalizer = new();

    public IReadOnlyList<Candidate> FindDuplicates() => FindDuplicates(Options.Defaults());

    public IReadOnlyList<Candidate> FindDuplicates(Options options)
    {
        ArgumentNullException.ThrowIfNull(options);
        List<Entry> entries = Scan(options.Paths)
            .Where(entry => entry.Lines >= options.MinLines)
            .Where(entry => entry.Nodes >= options.MinNodes)
            .ToList();
        List<Candidate> candidates = [];

        for (int i = 0; i < entries.Count; i++)
        {
            for (int j = i + 1; j < entries.Count; j++)
            {
                Entry left = entries[i];
                Entry right = entries[j];
                double score = Similarity(left, right);
                if (!left.Overlaps(right) && score >= options.Threshold)
                {
                    candidates.Add(new Candidate(score, left.Location(), right.Location(), left.Nodes, right.Nodes));
                }
            }
        }

        return
        [
            .. candidates
                .OrderByDescending(candidate => candidate.Score)
                .ThenBy(candidate => candidate.Left.File, StringComparer.Ordinal)
                .ThenBy(candidate => candidate.Left.StartLine)
                .ThenBy(candidate => candidate.Right.File, StringComparer.Ordinal)
                .ThenBy(candidate => candidate.Right.StartLine),
        ];
    }

    private static IEnumerable<string> CSharpFiles(string path)
    {
        if (File.Exists(path) && path.EndsWith(".cs", StringComparison.Ordinal))
        {
            return [path];
        }

        if (!Directory.Exists(path))
        {
            return [];
        }

        return Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories)
            .Where(file => file.EndsWith(".cs", StringComparison.Ordinal));
    }

    private static bool IsCandidateRoot(SyntaxNode node) =>
        node is ClassDeclarationSyntax
            or InterfaceDeclarationSyntax
            or RecordDeclarationSyntax
            or EnumDeclarationSyntax
            or MethodDeclarationSyntax
            or ConstructorDeclarationSyntax
            or FieldDeclarationSyntax
            or EnumMemberDeclarationSyntax
            or SimpleLambdaExpressionSyntax
            or ParenthesizedLambdaExpressionSyntax
            or StructDeclarationSyntax
            or AnonymousMethodExpressionSyntax
            or PropertyDeclarationSyntax
            or DelegateDeclarationSyntax
            or LocalFunctionStatementSyntax
            or IndexerDeclarationSyntax
            or EventDeclarationSyntax
            or EventFieldDeclarationSyntax;

    private static double Similarity(Entry left, Entry right)
    {
        HashSet<string> intersection = new(left.Fingerprints, StringComparer.Ordinal);
        intersection.IntersectWith(right.Fingerprints);
        HashSet<string> union = new(left.Fingerprints, StringComparer.Ordinal);
        union.UnionWith(right.Fingerprints);
        if (union.Count == 0)
        {
            return 0.0;
        }

        return (double)intersection.Count / union.Count;
    }

    private List<Entry> Scan(IReadOnlyList<string> paths)
    {
        return paths
            .SelectMany(CSharpFiles)
            .OrderBy(path => path, StringComparer.Ordinal)
            .SelectMany(ScanFile)
            .ToList();
    }

    private List<Entry> ScanFile(string file)
    {
        string text = File.ReadAllText(file);
        SyntaxTree tree = CSharpSyntaxTree.ParseText(text, new CSharpParseOptions(LanguageVersion.Latest));
        if (tree.GetDiagnostics().Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error))
        {
            throw new InvalidOperationException("Unable to parse " + file);
        }

        List<Entry> entries = [];
        CollectEntries(file, tree.GetRoot(), entries);
        return entries;
    }

    private void CollectEntries(string file, SyntaxNode node, List<Entry> entries)
    {
        if (IsCandidateRoot(node))
        {
            entries.Add(CreateEntry(file, node));
        }

        foreach (SyntaxNode child in node.ChildNodes())
        {
            CollectEntries(file, child, entries);
        }
    }

    private Entry CreateEntry(string file, SyntaxNode node)
    {
        FileLinePositionSpan span = node.GetLocation().GetLineSpan();
        int startLine = span.StartLinePosition.Line + 1;
        int endLine = span.EndLinePosition.Line + 1;
        NormalizedNode normalized = _normalizer.Normalize(node);
        return new Entry(file, startLine, endLine, normalized.NodeCount, normalized.Fingerprints());
    }

    private sealed record Entry(string File, int StartLine, int EndLine, int Nodes, ISet<string> Fingerprints)
    {
        public int Lines => EndLine - StartLine + 1;

        public Location Location() => new(File, StartLine, EndLine);

        public bool Overlaps(Entry other) =>
            string.Equals(File, other.File, StringComparison.Ordinal)
                && StartLine <= other.EndLine
                && other.StartLine <= EndLine;
    }
}
