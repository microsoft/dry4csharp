namespace Microsoft.Dry4CSharp.Tests;

public class NormalizedNodeTests
{
    [Fact]
    public void LeafNodeHasCountOneAndTagAsFingerprint()
    {
        NormalizedNode leaf = Leaf("Identifier");

        leaf.NodeCount.Should().Be(1);
        leaf.ToFingerprint().Should().Be("Identifier");
        leaf.Fingerprints().Should().Equal("Identifier");
    }

    [Fact]
    public void NodeCountCountsThisNodePlusEveryDescendant()
    {
        NormalizedNode root = new("Root", [Leaf("A"), new NormalizedNode("Inner", [Leaf("B"), Leaf("C")])]);

        root.NodeCount.Should().Be(5);
    }

    [Fact]
    public void InternalFingerprintNestsChildrenInParentheses()
    {
        NormalizedNode root = new("Root", [Leaf("A"), Leaf("B")]);

        root.ToFingerprint().Should().Be("(Root A B)");
    }

    [Fact]
    public void FingerprintsIncludeEverySubtreeFingerprint()
    {
        NormalizedNode root = new("Root", [Leaf("A"), new NormalizedNode("Inner", [Leaf("B")])]);

        // Every subtree's fingerprint, ordered Ordinal: '(' (0x28) sorts before 'A' (0x41).
        root.Fingerprints().Should().Equal("(Inner B)", "(Root A (Inner B))", "A", "B");
    }

    [Fact]
    public void FingerprintsAreDeduplicatedAndSortedOrdinally()
    {
        NormalizedNode root = new("Dup", [Leaf("X"), Leaf("X")]);

        // Duplicate child fingerprints collapse to one set entry; '(' (0x28) sorts before 'X' (0x58).
        root.Fingerprints().Should().Equal("(Dup X X)", "X");
    }

    private static NormalizedNode Leaf(string tag) => new(tag, []);
}
