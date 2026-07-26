# Design decisions — dry4csharp

Locked decisions for the C# port of `dry4java`. Source of truth alongside `docs/features/`.

> NOTE: Earlier revisions of this repo (and `.github/copilot-instructions.md`) describe a
> CRAP-metric / coverage analyzer. That wording is **stale** from a previous port and is
> superseded by this document. `dry4csharp` is a **duplicate-code detector**. There is no
> coverage, no CRAP formula, no Coverlet/Cobertura, and no `dotnet test`-as-analysis anywhere
> in the product.

## Product intent

- Analyze **C# projects** — the C# member of the `dry4*` family (`dry4clj` → `dry4java` →
  `dry4csharp`). It **detects candidate duplicate code**: parse C# source with **Roslyn**,
  select C# declarations as comparison candidates, **normalize** each candidate's syntax tree
  (names and literal values normalize away; syntactic shape remains), reduce each candidate to a
  set of **structural fingerprints**, and compare candidate pairs with **Jaccard similarity**:

  ```text
  score = shared fingerprints / all fingerprints seen in either candidate
  ```

  It reports fuzzy structural matches by filename and line range (text or EDN) so another
  mechanism can evaluate and reduce duplication.
- **Faithful 1:1 port** of `dry4java`: class decomposition, algorithm/heuristics, CLI contract,
  report format, and exit codes preserved. Only the ecosystem adapters change
  (JavaParser → Roslyn, Maven → dotnet, JUnit → xUnit).
- **Test fidelity:** every `dry4java` test gets a faithful C# counterpart with identical
  verification — except where a genuine language gap forces a re-interpretation (flagged below).

## OSS Libraries

Each choice is justified as the 1:1 analog of the corresponding `dry4java` dependency. Items that
are not a clean 1:1, have multiple viable candidates, or are inexact are flagged **`NEEDS-MR-DAS`**.

| Concern | dry4java | dry4csharp | 1:1? | Notes |
|---|---|---|---|---|
| Parser / AST | `com.github.javaparser:javaparser-core` 3.26.3 | **`Microsoft.CodeAnalysis.CSharp` (Roslyn)**, latest 4.x, pinned via lock file | Clean 1:1 | The official, canonical C# syntax parser — the singular JavaParser analog. **Syntax-only** parse (no `Compilation`, no semantic model), mirroring JavaParser used without symbol resolution. `LanguageVersion.Latest` mirrors `LanguageLevel.JAVA_21`. |
| Test framework | JUnit Jupiter 5.11.4 | **xUnit 2.5.3** (+ `Microsoft.NET.Test.Sdk` 17.8.0, `coverlet.collector`) | Clean 1:1 | Already pinned in `Dry4CSharp.Tests.Common.targets`. `@Test` → `[Fact]`. |
| Assertions | `org.junit.jupiter.api.Assertions` | **FluentAssertions 7.x** (Apache-2.0 line, pinned `[7.0.0,8.0.0)`) | Inexact | **Decided (Mr. Das): FluentAssertions** — matches `crap4csharp` house usage (11 `.Should()` calls, zero `Assert.`); already pinned + `Using`-imported in `Dry4CSharp.Tests.Common.targets`. Accepted the assertion-style delta from JUnit `assertEquals`. |
| EDN output | **none** — hand-rolled `StringBuilder` in `Dry4Java.toEdn` | **none** — hand-rolled, byte-for-byte mirroring `dry4java` | Clean 1:1 | Deliberately **library-free**. C# EDN libraries exist but would change the exact output layout; using one would break output fidelity. Reproduce `dry4java`'s manual string construction and escaping exactly. |
| Build/driver | Maven + shade | **`dotnet` / MSBuild**, shared `*.targets` | Adapter | Ecosystem adapter only. Single-file console exe via `dotnet publish` if a jar-equivalent is wanted (defer). |

No other runtime dependencies. (The CRAP-era Coverlet/Cobertura stack is **not** used by the product;
`coverlet.collector` remains only as the standard test-coverage collector for our own test runs.)

## Idiomatic

Adopt-now C# idioms (consistent with `Dry4CSharp.Common.targets`: `Nullable=enable`,
`ImplicitUsings=enable`, `LangVersion=latest`, analyzers-as-errors in Release):

- **Nullable reference types** enabled throughout.
- **`record` types** for the pure data holders: `Location`, `Candidate`, `NormalizedNode`, and
  `Options` (positional/`init` where it preserves the Java constructor arity the tests call).
- **`IReadOnlyList<string>`** for path/collection surfaces (analog of Java `List.copyOf`
  immutability).
- **`file`-scoped namespaces**, expression-bodied members where they read clearly.
- **Pattern matching / `switch` expressions** to replace Java `instanceof` chains in
  candidate-root selection, `tag`, and `markers` dispatch (idiomatic and faithful to the scheme).
- **`CultureInfo.InvariantCulture`** for every numeric parse (`--threshold`, `--min-lines`,
  `--min-nodes`) and every numeric format (`F2` score in text, raw `double` in EDN). Java's
  `Double.parseDouble`/`Integer.parseInt` and `Locale.US` formatting are culture-invariant;
  C# defaults are culture-sensitive, so this is mandatory.
- **Explicit `"\n"`** for all program output (report + `USAGE`), never `Environment.NewLine`.
  Required by the ported tests (e.g. `"No duplicate candidates found.\n"`,
  `"...\n  a.cs:10-14\n  b.cs:20-24"`) and for deterministic cross-platform output.
- **`StringComparer.Ordinal`** for fingerprint set membership and for the result-ordering string
  comparisons (mirrors Java `String.compareTo`/`equals`, avoids culture surprises).
- **Stable sort** for results (LINQ `OrderBy`/`ThenBy` chain) to mirror Java's stable `List.sort`.

Decided by Mr. Das (product decision): **all** C#-only **candidate-root** kinds are IN
(struct, record struct, property, anonymous method, delegate, local function, indexer, event) — see
Fidelity mapping. Deferrals: parallelizing the O(n²) comparison, project/solution-aware scanning,
additional output formats.

## Locked choices

| Aspect | Decision |
|---|---|
| Product | Duplicate-code detector (Roslyn → normalize → fingerprints → Jaccard). No coverage/CRAP. |
| Analysis target | C# source **files** (`.cs`), **syntactic parse only** — no project/solution load, no semantic model. |
| Root namespace | `Microsoft.Dry4CSharp` (tests `Microsoft.Dry4CSharp.Tests`) — from `Dry4CSharp.Common.targets` (`Microsoft.$(MSBuildProjectName)`). |
| Assembly names | `Microsoft.Dry4CSharp`, `Microsoft.Dry4CSharp.Tests`. |
| TFM / SDK | `net8.0`; SDK `8.0.406` (`global.json`, `rollForward: latestFeature`). |
| Parser | Roslyn `Microsoft.CodeAnalysis.CSharp`, `LanguageVersion.Latest`, syntax-only. |
| Test stack | xUnit + `Microsoft.NET.Test.Sdk` + FluentAssertions 7.x (all pinned in `*.Tests.Common.targets`). |
| EDN | Hand-rolled, no library; exact `dry4java` layout + escaping. |
| Culture / newlines | `InvariantCulture` everywhere; explicit `"\n"`; `StringComparer.Ordinal` for sets & ordering. |
| Scan model | Expand path args → `.cs` files (dirs recurse, globally **Ordinal-sorted**), default path `["src"]`; non-existent/non-`.cs` paths silently ignored (Java parity); candidates collected **pre-order DFS**. |
| Parse errors | Roslyn is error-tolerant; **fail-fast**: after `ParseText`, throw if the tree has any `DiagnosticSeverity.Error` (mirrors Java `ParseProblemException` → `IllegalStateException`). |
| Sort stability | Stable `OrderBy` chain (Java `List.sort` is stable; `List.Sort` is not). |
| Class decomposition | `Dry4CSharp` (entry + output), `Options`, `CSharpDuplicateFinder`, `CSharpNormalizer`, `NormalizedNode`, `Candidate`, `Location` — 1:1 with `dry4java`. |
| Visibility | **No `internal`** (guardrail). Java package-private members that tests touch (`FormatCandidate`/`PrintText`/`ToEdn`) become **`public static`**; otherwise keep types `public` or `private` nested (`Entry` stays a `private` nested record). |
| Exit codes | `0` normal & `--help`; **`2`** unknown `--format`; non-zero on uncaught CLI/parse exceptions (fail-fast, mirrors Java). |
| Entry point | Explicit `public static void Main(string[] args)` on a `public static` class (not top-level statements) — needed for direct test access and Java parity. |

## Fidelity mapping (Java → Roslyn)

### Candidate roots
`dry4java` roots (`JavaDuplicateFinder.isCandidateRoot`): `ClassOrInterfaceDeclaration`,
`RecordDeclaration`, `EnumDeclaration`, `AnnotationDeclaration`, `MethodDeclaration`,
`ConstructorDeclaration`, `FieldDeclaration`, `InitializerDeclaration`, `EnumConstantDeclaration`,
`LambdaExpr`.

| Java root | Roslyn analog | Decision |
|---|---|---|
| `ClassOrInterfaceDeclaration` | `ClassDeclarationSyntax`, `InterfaceDeclarationSyntax` | **IN** (direct). |
| `RecordDeclaration` | `RecordDeclarationSyntax` (covers `record class` **and** `record struct`) | **IN** (direct). |
| `EnumDeclaration` | `EnumDeclarationSyntax` | **IN** (direct). |
| `MethodDeclaration` | `MethodDeclarationSyntax` | **IN** (direct). |
| `ConstructorDeclaration` | `ConstructorDeclarationSyntax` (incl. `static` ctor) | **IN** (direct). |
| `FieldDeclaration` | `FieldDeclarationSyntax` | **IN** (direct). |
| `EnumConstantDeclaration` | `EnumMemberDeclarationSyntax` | **IN** (direct). |
| `LambdaExpr` | `SimpleLambdaExpressionSyntax`, `ParenthesizedLambdaExpressionSyntax` | **IN** (direct). |
| `AnnotationDeclaration` (`@interface`) | *none* — C# attribute **types** are ordinary classes (already covered by `ClassDeclarationSyntax`) | **N/A** — no separate root; folded into class. |
| `InitializerDeclaration` (init blocks) | *none* — C# has no instance/static init **blocks**; `static` ctor is a `ConstructorDeclarationSyntax` | **N/A** — no separate root. |
| **C#-only:** `StructDeclarationSyntax` | struct type | **IN (Mr. Das)** — structural sibling of class/record. |
| **C#-only:** `record struct` | already `RecordDeclarationSyntax` | **IN** with records. |
| **C#-only:** `AnonymousMethodExpressionSyntax` (`delegate {}`) | pre-lambda equivalent | **IN (Mr. Das)** — lambda-equivalent. |
| **C#-only:** `PropertyDeclarationSyntax` | C# idiom replacing Java getter/setter+field | **IN (Mr. Das)** — captures a large class of C# duplication (adds a root with no Java analog; results diverge from dry4java by design). |
| **C#-only:** `DelegateDeclarationSyntax` | type decl, no body | **IN (Mr. Das)** — included for completeness (little body structure, so rarely clears `min-nodes`). |
| **C#-only:** `LocalFunctionStatementSyntax` | method-like, nested in a body | **IN (Mr. Das)** — no Java analog; included per Mr. Das. |
| **C#-only:** `IndexerDeclarationSyntax`, `EventDeclarationSyntax`, `EventFieldDeclarationSyntax` | members | **IN (Mr. Das)** — included per Mr. Das (low frequency). |
| Top-level statements / `GlobalStatementSyntax` | not declarations | **OUT.** |

### Normalizer — dropped children (`keepsStructuralChild`)
Java drops children whose simple name is `SimpleName`, `*LiteralExpr`, `Name`, `NameExpr`, or
`Comment`. Roslyn models many of these as **tokens/trivia**, not child `SyntaxNode`s, so they are
*already* excluded from `ChildNodes()`; only a few need an explicit drop:

| Java dropped | Roslyn | Handling |
|---|---|---|
| `SimpleName`, `NameExpr` | `IdentifierNameSyntax` | **Drop** (it is a child node in expression/type position). |
| `Name` (qualified) | `QualifiedNameSyntax`, `AliasQualifiedNameSyntax` | **Drop.** |
| `*LiteralExpr` | `LiteralExpressionSyntax` (numeric/string/char/bool/null/raw) | **Drop.** |
| `Comment` | `SyntaxTrivia` | Already excluded (not a child node) — no rule needed. |
| — | `GenericNameSyntax` (`List<int>`) | **Keep** — its `TypeArgumentList` carries structure to preserve (analog of Java `ClassOrInterfaceType` type-args). *(Decision: keep the wrapper so `<...>` shape survives.)* |
| — | `InterpolatedStringExpressionSyntax` | **Keep** — it is structured (contains sub-expressions), unlike a plain literal. *(Note: diverges from a Java text-block, which is a dropped `*LiteralExpr`.)* |

### Normalizer — tag & markers
- **`tag`** = Roslyn node type name with the `"Syntax"` suffix stripped (e.g.
  `MethodDeclarationSyntax`→`MethodDeclaration`, `BinaryExpressionSyntax`→`BinaryExpression`).
  This is the faithful analog of `getClass().getSimpleName()` and — crucially — keeps the operator
  **out** of the tag (Roslyn otherwise bakes it into `Kind()`), so the Java scheme "general tag +
  separate operator marker" is preserved. Java's one cosmetic special case
  (`EnumConstantDeclaration`→`"EnumConstant"`) is unnecessary and dropped
  (`EnumMemberDeclarationSyntax`→`EnumMemberDeclaration`).
- **`markers`** (emitted as sorted leaf children):

| Java marker | Source | Roslyn source | Emit |
|---|---|---|---|
| `annotation` (per annotation on a `BodyDeclaration`) | `BodyDeclaration.getAnnotations()` | `MemberDeclarationSyntax.AttributeLists` → each `AttributeSyntax` | `"annotation"` per attribute (attribute subtrees also remain as kept children, matching Java). |
| `modifier:NAME` | `NodeWithModifiers.getModifiers()` | `*.Modifiers` (`SyntaxTokenList`) | `"modifier:" + token.ValueText` (e.g. `modifier:public`, `modifier:readonly`). |
| `operator:NAME` | `BinaryExpr`/`UnaryExpr`/`AssignExpr` operator enum | `BinaryExpressionSyntax`, `Prefix`/`PostfixUnaryExpressionSyntax`, `AssignmentExpressionSyntax` | `"operator:" + node.Kind()` (e.g. `AddExpression`, `SimpleAssignmentExpression`, `PreIncrementExpression`) — preserves pre/post & operator identity. |
| `modifier:NAME` (var decls) | `VariableDeclarationExpr.getModifiers()` | `LocalDeclarationStatementSyntax.Modifiers` (`const`/`fixed`/`ref`/…) — modifiers live on the statement, not `VariableDeclarationSyntax` | `"modifier:" + token.ValueText`. **Flag:** marker-source node differs from Java. |
| `primitive:NAME` | `PrimitiveType.getType()` | `PredefinedTypeSyntax.Keyword` | `"primitive:" + keyword.ValueText` (e.g. `int`, `double`, `bool`, `string`, `object`, `void`). **Flag:** C# `string`/`object`/`void` are predefined (get a marker) whereas Java `String` is a class type / `void` is `VoidType` (no marker) — a language divergence. |
| `switch:TYPE` | `SwitchEntry.getType()` | `SwitchSectionSyntax` (colon/statement) vs `SwitchExpressionArmSyntax` (arrow/expression) | `"switch:section"` / `"switch:arm"`. *(Redundant with the distinct tags, but emitted for scheme parity.)* |
| `lambda:parenthesized` | `LambdaExpr.isEnclosingParameters()` | `ParenthesizedLambdaExpressionSyntax` | `"lambda:parenthesized"` (none for `SimpleLambdaExpressionSyntax`). |

**Marker scope (S2 review — decided by Mr. Das).** `modifier:` markers are emitted only for **type
members** (`MemberDeclarationSyntax.Modifiers`) and **local declarations**
(`LocalDeclarationStatementSyntax`) — faithful to `dry4java`'s two marker sources. Modifiers on nodes
Java has no analog for — **local functions** & **lambdas** (`static`/`async`), **accessors**
(`private set`), **parameters** (`ref`/`out`/`in`/`params`) — are deliberately **not** marked.
Rationale: this is a DRY *duplicate detector*; leaving those modifiers unmarked means two
structurally-identical bodies that differ only by such a modifier still fingerprint alike and are
correctly flagged as the duplication they are — marking them would only lower similarity and risk
*hiding* real duplicates (the same reasoning that normalizes away names and literals). Also: C# adds a
local-declaration modifier **once**; `dry4java`'s `VariableDeclarationExpr` double-adds it (an
unintentional Java artifact) — not reproduced, accepted under structural-not-literal.

### Fingerprint / nodeCount parity
`NormalizedNode` is language-agnostic → ported 1:1. `nodeCount` = recursive node total. Fingerprints
= the set of every subtree's fingerprint; leaf → `tag`, internal → `"(" + tag + " " + childFps… + ")"`.
Use `SortedSet<string>` with `StringComparer.Ordinal` (mirrors Java `TreeSet<String>`); similarity
copies into `HashSet<string>` (Ordinal). Set counts are comparer-independent, so scores are unaffected;
Ordinal is chosen for correctness/determinism.

### Similarity / sort / overlap parity
- **Similarity** = `|∩| / |∪|`, `0.0` when the union is empty — 1:1.
- **Overlap** = same file **and** `startLine ≤ other.endLine` **and** `other.startLine ≤ endLine` — 1:1
  (file compared with `Ordinal` equality).
- **Sort** = score **desc**, then `left.file`, `left.startLine`, `right.file`, `right.startLine`
  (files compared `Ordinal`), delivered via a **stable** `OrderBy`/`ThenBy` chain (Java `List.sort`
  is stable; ties must keep insertion order).

### CLI / exit-code parity
`Options.Parse` mirrors the Java `switch`: `--threshold`/`--min-lines`/`--min-nodes` (value parsed with
`InvariantCulture`), `--format F`, `--edn`, `--text`, `--help`/`-h`; unknown tokens → paths; empty →
`["src"]`; missing value → throw (fail-fast). Format is validated at **output** time (as in Java):
`text`/`edn` dispatch, otherwise `Console.Error.WriteLine("Unknown format: " + format)` +
`Environment.Exit(2)`. `--help` prints `USAGE` and returns (exit `0`). Malformed numbers throw
(`FormatException`) and propagate (fail-fast, non-zero exit).

### Output parity
- **Text:** empty → `"No duplicate candidates found."`; else per candidate
  `"DUPLICATE score=" + score.ToString("F2", InvariantCulture) + "\n  " + file:start-end + "\n  " + file:start-end`,
  blocks separated by a blank line. All newlines explicit `"\n"`.
- **EDN:** empty → `"{:candidates []}"`; else the structured form with `:score` as the **raw double**
  (`double.ToString(InvariantCulture)`), `:left`/`:right` `{:file :start-line :end-line}`,
  `:left-nodes`, `:right-nodes`; file strings escape `\` then `"` (in that order). Layout/indentation
  reproduced exactly from `Dry4Java.toEdn`.

## Deliberate departures (approved by Mr. Das)

The CRAP-era departures (fail-fast on no-coverage; augmented cyclomatic complexity; coverage-key by
FQN) are **N/A** — there is no coverage or complexity in this tool. The genuine dry-specific departures:

1. **Fingerprints are structural, not string-identical to `dry4java`.** Roslyn and JavaParser name and
   model nodes differently (tokens vs child nodes; `Kind`-per-operator; identifier-as-token). We
   preserve the *scheme* (tag + sorted markers, names/literals normalized away) and the *algorithm*,
   but fingerprint strings and absolute `nodeCount`s are **not** cross-tool comparable. Cross-tool
   parity is **structural** (same duplicates surface), not literal. Tests therefore assert
   *duplicate-detection behavior* and *C#-sample* line ranges, not Java's absolute counts/strings.
2. **Parse-failure handling.** Roslyn is error-tolerant (never throws on bad syntax); we replicate
   Java's fail-fast by throwing when a parsed tree carries any `DiagnosticSeverity.Error`.
3. **Culture & number formatting.** `InvariantCulture` + explicit `"\n"` (Java relied on `Locale.US`,
   `Locale`-default `%.2f`, `Double.toString`, and platform `System.lineSeparator()`). Note two
   subtle rounding/formatting parity risks (see Risks R5): `%.2f` (Java `HALF_UP`) vs .NET `F2`
   midpoint rounding, and `Double.toString` vs `double.ToString` shortest-round-trip for the EDN
   `:score`.
4. **Visibility widened to `public`** where Java used package-private, because `internal` is banned by
   guardrail and the tests exercise those members directly.
5. **Candidate-root set extended** for C#-only constructs — **all** C#-only roots IN (struct, record
   struct, property, anonymous method, delegate, local function, indexer, event), approved by
   Mr. Das. See Fidelity mapping.
