namespace Microsoft.Dry4CSharp;

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

/// <summary>
/// Command-line entry point and output formatters. Faithful port of <c>dry4java</c>'s
/// <c>Dry4Java</c>: same <c>USAGE</c>, argument/format dispatch, exit codes, and text/EDN layout
/// (see <c>docs/decisions.md</c>). All program output uses explicit <c>"\n"</c> newlines and
/// <see cref="CultureInfo.InvariantCulture"/> number formatting.
/// </summary>
[SuppressMessage(
    "Naming",
    "CA1724:Type names should not match namespaces",
    Justification = "The entry-point type name (Dry4CSharp) and the root namespace (Microsoft.Dry4CSharp) are both locked in docs/decisions.md; the leaf-segment collision is an intentional, unavoidable consequence.")]
public static class Dry4CSharp
{
    /// <summary>Help text printed for <c>--help</c>/<c>-h</c>; mirrors <c>dry4java</c>'s <c>USAGE</c>.</summary>
    public const string Usage =
        "Usage: dry4csharp [options] [file-or-directory ...]\n"
        + "\n"
        + "Options:\n"
        + "  --threshold N   Minimum structural similarity score, default 0.82\n"
        + "  --min-lines N   Minimum source lines in a candidate declaration, default 4\n"
        + "  --min-nodes N   Minimum normalized syntax nodes, default 20\n"
        + "  --format F      text or edn, default text\n"
        + "  --edn           Same as --format edn\n"
        + "  --text          Same as --format text";

    public static void Main(string[] args)
    {
        Options options = Options.Parse(args);
        if (options.Help)
        {
            Console.Out.Write(Usage + "\n");
            return;
        }

        IReadOnlyList<Candidate> candidates = new CSharpDuplicateFinder().FindDuplicates(options);
        switch (options.Format)
        {
            case "edn":
                Console.Out.Write(ToEdn(candidates) + "\n");
                break;
            case "text":
                PrintText(candidates);
                break;
            default:
                Console.Error.Write("Unknown format: " + options.Format + "\n");
                Environment.Exit(2);
                break;
        }
    }

    public static void PrintText(IReadOnlyList<Candidate> candidates)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        if (candidates.Count == 0)
        {
            Console.Out.Write("No duplicate candidates found.\n");
            return;
        }

        for (int i = 0; i < candidates.Count; i++)
        {
            if (i > 0)
            {
                Console.Out.Write("\n");
            }

            Console.Out.Write(FormatCandidate(candidates[i]) + "\n");
        }
    }

    public static string FormatCandidate(Candidate candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        return "DUPLICATE score=" + candidate.Score.ToString("F2", CultureInfo.InvariantCulture) + "\n"
            + "  " + LineRange(candidate.Left) + "\n"
            + "  " + LineRange(candidate.Right);
    }

    public static string ToEdn(IReadOnlyList<Candidate> candidates)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        if (candidates.Count == 0)
        {
            return "{:candidates []}";
        }

        StringBuilder builder = new("{:candidates\n [");
        for (int i = 0; i < candidates.Count; i++)
        {
            if (i > 0)
            {
                builder.Append("\n  ");
            }

            Candidate candidate = candidates[i];
            builder.Append("{:score ").Append(candidate.Score.ToString(CultureInfo.InvariantCulture))
                .Append("\n   :left ").Append(LocationEdn(candidate.Left))
                .Append("\n   :right ").Append(LocationEdn(candidate.Right))
                .Append("\n   :left-nodes ").Append(candidate.LeftNodes.ToString(CultureInfo.InvariantCulture))
                .Append("\n   :right-nodes ").Append(candidate.RightNodes.ToString(CultureInfo.InvariantCulture))
                .Append('}');
        }

        builder.Append("]}");
        return builder.ToString();
    }

    private static string LocationEdn(Location location) =>
        "{:file \"" + Escape(location.File) + "\", :start-line "
            + location.StartLine.ToString(CultureInfo.InvariantCulture)
            + ", :end-line " + location.EndLine.ToString(CultureInfo.InvariantCulture) + "}";

    private static string Escape(string text) =>
        text.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);

    private static string LineRange(Location location) =>
        location.File + ":" + location.StartLine.ToString(CultureInfo.InvariantCulture)
            + "-" + location.EndLine.ToString(CultureInfo.InvariantCulture);
}
