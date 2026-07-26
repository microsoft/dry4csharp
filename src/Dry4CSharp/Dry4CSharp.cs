namespace Microsoft.Dry4CSharp;

using System.Diagnostics.CodeAnalysis;

[SuppressMessage(
    "Naming",
    "CA1724:Type names should not match namespaces",
    Justification = "The entry-point type name (Dry4CSharp) and the root namespace (Microsoft.Dry4CSharp) are both locked in docs/decisions.md; the leaf-segment collision is an intentional, unavoidable consequence.")]
public static class Dry4CSharp
{
    public static void Main(string[] args)
    {
    }
}
