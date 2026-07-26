namespace Microsoft.Dry4CSharp;

using System.Globalization;

/// <summary>
/// Command-line options for the duplicate finder. Faithful port of <c>dry4java</c>'s
/// <c>Options</c>: same six-argument constructor arity/order and the same <c>parse</c> switch.
/// </summary>
public sealed record Options
{
    public Options(
        IReadOnlyList<string> paths,
        double threshold,
        int minLines,
        int minNodes,
        string format,
        bool help)
    {
        ArgumentNullException.ThrowIfNull(paths);
        Paths = [.. paths];
        Threshold = threshold;
        MinLines = minLines;
        MinNodes = minNodes;
        Format = format;
        Help = help;
    }

    public IReadOnlyList<string> Paths { get; }

    public double Threshold { get; }

    public int MinLines { get; }

    public int MinNodes { get; }

    public string Format { get; }

    public bool Help { get; }

    public static Options Defaults() => new(["src"], 0.82, 4, 20, "text", false);

    public static Options Parse(params string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);
        List<string> paths = [];
        double threshold = 0.82;
        int minLines = 4;
        int minNodes = 20;
        string format = "text";
        bool help = false;

        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i];
            switch (arg)
            {
                case "--threshold":
                    threshold = double.Parse(ValueFor(args, ++i, arg), CultureInfo.InvariantCulture);
                    break;
                case "--min-lines":
                    minLines = int.Parse(ValueFor(args, ++i, arg), CultureInfo.InvariantCulture);
                    break;
                case "--min-nodes":
                    minNodes = int.Parse(ValueFor(args, ++i, arg), CultureInfo.InvariantCulture);
                    break;
                case "--format":
                    format = ValueFor(args, ++i, arg);
                    break;
                case "--edn":
                    format = "edn";
                    break;
                case "--text":
                    format = "text";
                    break;
                case "--help":
                case "-h":
                    help = true;
                    break;
                default:
                    paths.Add(arg);
                    break;
            }
        }

        if (paths.Count == 0)
        {
            paths.Add("src");
        }

        return new Options(paths, threshold, minLines, minNodes, format, help);
    }

    private static string ValueFor(string[] args, int index, string option)
    {
        if (index >= args.Length)
        {
            throw new ArgumentException("Missing value for " + option, nameof(args));
        }

        return args[index];
    }
}
