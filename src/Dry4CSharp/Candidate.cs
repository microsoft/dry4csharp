namespace Microsoft.Dry4CSharp;

/// <summary>A reported duplicate: the Jaccard score plus both matched locations and node counts.</summary>
public sealed record Candidate(double Score, Location Left, Location Right, int LeftNodes, int RightNodes);
