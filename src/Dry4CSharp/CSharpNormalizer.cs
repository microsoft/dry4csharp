namespace Microsoft.Dry4CSharp;

using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

/// <summary>
/// Normalizes a Roslyn syntax subtree into a <see cref="NormalizedNode"/> tree, preserving syntactic
/// shape while stripping names and literal values. Faithful port of <c>dry4java</c>'s
/// <c>JavaNormalizer</c> (see the Fidelity mapping in <c>docs/decisions.md</c>).
/// </summary>
public sealed class CSharpNormalizer
{
    [SuppressMessage(
        "Performance",
        "CA1822:Mark members as static",
        Justification = "Instance method by design: CSharpNormalizer is a stateless collaborator instantiated and composed as a field by CSharpDuplicateFinder, faithfully mirroring dry4java's JavaNormalizer and preserving the ported instance API.")]
    public NormalizedNode Normalize(SyntaxNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        List<NormalizedNode> children = [];
        foreach (string marker in Markers(node))
        {
            children.Add(new NormalizedNode(marker, []));
        }

        foreach (SyntaxNode child in node.ChildNodes())
        {
            if (KeepsStructuralChild(child))
            {
                children.Add(Normalize(child));
            }
        }

        return new NormalizedNode(Tag(node), children);
    }

    private static string Tag(SyntaxNode node)
    {
        const string suffix = "Syntax";
        string name = node.GetType().Name;
        return name.EndsWith(suffix, StringComparison.Ordinal)
            ? name[..^suffix.Length]
            : name;
    }

    private static bool KeepsStructuralChild(SyntaxNode child) =>
        child is not (IdentifierNameSyntax
            or QualifiedNameSyntax
            or AliasQualifiedNameSyntax
            or LiteralExpressionSyntax);

    private static List<string> Markers(SyntaxNode node)
    {
        List<string> markers = [];

        if (node is MemberDeclarationSyntax member)
        {
            int annotations = member.AttributeLists.Sum(list => list.Attributes.Count);
            for (int index = 0; index < annotations; index++)
            {
                markers.Add("annotation");
            }

            foreach (SyntaxToken modifier in member.Modifiers)
            {
                markers.Add("modifier:" + modifier.ValueText);
            }
        }

        if (node is BinaryExpressionSyntax
            or PrefixUnaryExpressionSyntax
            or PostfixUnaryExpressionSyntax
            or AssignmentExpressionSyntax)
        {
            markers.Add("operator:" + node.Kind());
        }
        else if (node is LocalDeclarationStatementSyntax local)
        {
            foreach (SyntaxToken modifier in local.Modifiers)
            {
                markers.Add("modifier:" + modifier.ValueText);
            }
        }
        else if (node is PredefinedTypeSyntax predefined)
        {
            markers.Add("primitive:" + predefined.Keyword.ValueText);
        }
        else if (node is SwitchSectionSyntax)
        {
            markers.Add("switch:section");
        }
        else if (node is SwitchExpressionArmSyntax)
        {
            markers.Add("switch:arm");
        }
        else if (node is ParenthesizedLambdaExpressionSyntax)
        {
            markers.Add("lambda:parenthesized");
        }

        markers.Sort(StringComparer.Ordinal);
        return markers;
    }
}
