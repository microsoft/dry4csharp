namespace Microsoft.Dry4CSharp;

/// <summary>
/// A normalized syntax tree: a structural <see cref="Tag"/> and its ordered children. Names and
/// literal values are already stripped away by the normalizer, so equal shape yields equal
/// fingerprints. Faithful port of <c>dry4java</c>'s <c>NormalizedNode</c>.
/// </summary>
/// <remarks>
/// The algorithm compares candidates only through the string fingerprints produced by
/// <see cref="Fingerprints"/>, so Java's deep <c>equals</c>/<c>hashCode</c> are intentionally not
/// ported; the record's synthesized value-equality (shallow over <see cref="Children"/>) is unused.
/// </remarks>
public sealed record NormalizedNode
{
    public NormalizedNode(string tag, IReadOnlyList<NormalizedNode> children)
    {
        ArgumentNullException.ThrowIfNull(children);
        Tag = tag;
        Children = [.. children];
    }

    public string Tag { get; }

    public IReadOnlyList<NormalizedNode> Children { get; }

    /// <summary>The total number of nodes in this subtree (this node plus every descendant).</summary>
    public int NodeCount
    {
        get
        {
            int count = 1;
            foreach (NormalizedNode child in Children)
            {
                count += child.NodeCount;
            }

            return count;
        }
    }

    /// <summary>The set of every subtree's fingerprint, ordered by <see cref="StringComparer.Ordinal"/>.</summary>
    public ISet<string> Fingerprints()
    {
        SortedSet<string> result = new(StringComparer.Ordinal);
        CollectFingerprints(result);
        return result;
    }

    /// <summary>Leaf → <see cref="Tag"/>; internal → <c>"(" + tag + " " + child fingerprints + ")"</c>.</summary>
    public string ToFingerprint()
    {
        if (Children.Count == 0)
        {
            return Tag;
        }

        List<string> parts = [Tag];
        foreach (NormalizedNode child in Children)
        {
            parts.Add(child.ToFingerprint());
        }

        return "(" + string.Join(" ", parts) + ")";
    }

    private void CollectFingerprints(ISet<string> result)
    {
        result.Add(ToFingerprint());
        foreach (NormalizedNode child in Children)
        {
            child.CollectFingerprints(result);
        }
    }
}
