namespace Microsoft.Dry4CSharp.Tests;

public class SmokeTests
{
    [Fact]
    public void EntryPointTypeIsPublicStaticAndReachable()
    {
        var entryType = typeof(Dry4CSharp);

        entryType.IsPublic.Should().BeTrue();

        // A C# `static class` compiles to an `abstract sealed` type in IL.
        entryType.IsAbstract.Should().BeTrue();
        entryType.IsSealed.Should().BeTrue();
    }
}
