namespace Microsoft.Dry4CSharp;

/// <summary>A candidate's source position: file path and 1-based inclusive line range.</summary>
public sealed record Location(string File, int StartLine, int EndLine);
