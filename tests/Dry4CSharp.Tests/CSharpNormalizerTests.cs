namespace Microsoft.Dry4CSharp.Tests;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public class CSharpNormalizerTests
{
    private readonly CSharpNormalizer _normalizer = new();

    [Fact]
    public void NormalizesAwayIdentifierNamesAndLiteralValues()
    {
        MethodDeclarationSyntax left = First<MethodDeclarationSyntax>("class C { int F(int a) { return a + 1; } }");
        MethodDeclarationSyntax right = First<MethodDeclarationSyntax>("class D { int G(int b) { return b + 99; } }");

        _normalizer.Normalize(left).Fingerprints()
            .Should().BeEquivalentTo(_normalizer.Normalize(right).Fingerprints());
    }

    [Fact]
    public void EmitsModifierAndPrimitiveMarkersSortedBeforeStructuralChildren()
    {
        FieldDeclarationSyntax field = First<FieldDeclarationSyntax>("class C { public static readonly int Answer = 42; }");

        NormalizedNode normalized = _normalizer.Normalize(field);

        // Marker leaves are prepended, Ordinal-sorted, ahead of the (internal) VariableDeclaration child.
        normalized.Children
            .Where(child => child.Children.Count == 0)
            .Select(child => child.Tag)
            .Should().Equal("modifier:public", "modifier:readonly", "modifier:static");
        normalized.Fingerprints().Should().Contain("primitive:int");
    }

    [Fact]
    public void EmitsAnnotationMarkerPerAttribute()
    {
        ClassDeclarationSyntax type = First<ClassDeclarationSyntax>("[System.Serializable] class C { }");

        _normalizer.Normalize(type).Fingerprints().Should().Contain("annotation");
    }

    [Fact]
    public void EmitsOperatorMarkerCarryingTheSyntaxKind()
    {
        BinaryExpressionSyntax expression = First<BinaryExpressionSyntax>("class C { int F(int a) { return a + a; } }");

        _normalizer.Normalize(expression).Fingerprints().Should().Contain("operator:AddExpression");
    }

    [Fact]
    public void KeepsGenericAndInterpolatedShapeButDropsNamesAndLiterals()
    {
        MethodDeclarationSyntax method = First<MethodDeclarationSyntax>(
            "using System.Collections.Generic; class C { void F() { List<int> xs = new(); int n = 5; var s = $\"{n}\"; } }");

        ISet<string> fingerprints = _normalizer.Normalize(method).Fingerprints();

        fingerprints.Should().Contain(fingerprint => fingerprint.Contains("GenericName", StringComparison.Ordinal));
        fingerprints.Should().Contain(fingerprint => fingerprint.Contains("InterpolatedStringExpression", StringComparison.Ordinal));
        fingerprints.Should().NotContain(fingerprint => fingerprint.Contains("IdentifierName", StringComparison.Ordinal));
        fingerprints.Should().NotContain(fingerprint => fingerprint.Contains("LiteralExpression", StringComparison.Ordinal));
    }

    private static T First<T>(string code)
        where T : SyntaxNode =>
        CSharpSyntaxTree.ParseText(code, new CSharpParseOptions(LanguageVersion.Latest))
            .GetRoot()
            .DescendantNodesAndSelf()
            .OfType<T>()
            .First();
}
