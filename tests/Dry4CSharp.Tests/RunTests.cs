namespace Microsoft.Dry4CSharp.Tests;

using System.Globalization;

// Faithful port of dry4java's exit-code contract: main lets Options.parse / findDuplicates throw so
// the JVM exits 1, and uses System.exit(2) for an unknown format. Here Run returns the exit code
// (0 help/success, 2 unknown format) and Main translates any exception to 1.
//
// These tests redirect Console.Out / Console.Error, so they share the process-global "ConsoleCapture"
// collection with the other capturing tests to keep them from running in parallel (avoids interleaved
// captured output).
[Collection("ConsoleCapture")]
public sealed class RunTests : IDisposable
{
    private readonly List<string> _dirs = [];

    [Fact]
    public void RunReturnsZeroAndPrintsUsageForHelp()
    {
        int exit = -1;
        string output = CaptureOut(() => exit = Dry4CSharp.Run(["--help"]));

        exit.Should().Be(0);
        output.Should().Be(Dry4CSharp.Usage + "\n");
    }

    [Fact]
    public void RunReturnsZeroOnSuccessfulTextRun()
    {
        string dir = NewDir(); // empty directory → no candidates
        int exit = -1;
        string output = CaptureOut(() => exit = Dry4CSharp.Run([dir]));

        exit.Should().Be(0);
        output.Should().Be("No duplicate candidates found.\n");
    }

    [Fact]
    public void RunReturnsTwoForUnknownFormat()
    {
        string dir = NewDir();
        int exit = -1;
        string error = CaptureError(() => exit = Dry4CSharp.Run(["--format", "xml", dir]));

        exit.Should().Be(2);
        error.Should().Be("Unknown format: xml\n");
    }

    [Fact]
    public void MainReturnsOneWhenANumericArgumentIsMalformed()
    {
        int exit = -1;
        CaptureError(() => exit = Dry4CSharp.Main(["--threshold", "not-a-number"]));

        // Options.Parse throws FormatException; Main catches it at the CLI boundary and returns 1,
        // mirroring dry4java's main letting the exception reach the JVM (exit 1).
        exit.Should().Be(1);
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

    private static string CaptureError(Action action)
    {
        TextWriter original = Console.Error;
        using StringWriter captured = new(CultureInfo.InvariantCulture);
        Console.SetError(captured);
        try
        {
            action();
        }
        finally
        {
            Console.SetError(original);
        }

        return captured.ToString();
    }

    private string NewDir()
    {
        string dir = Path.Combine(Path.GetTempPath(), "dry4csharp-run-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        _dirs.Add(dir);
        return dir;
    }
}
