# Feature: dry4csharp — faithful C# port of dry4java
**Branch:** vibe/dry4csharp-port
**Status:** Planning

## Requirements

Convert the read-only Java tool `dry4java` (`../dry4java`) into its C# equivalent, `dry4csharp`,
with **high fidelity**. The C# tool analyzes **C# projects**. Preserve class decomposition, the original 
formulas / heuristics, CLI contract, report format, and exit codes; adapt only the ecosystem adapters. Every Java test 
gets a faithful C# counterpart with identical verification. Use popular OSS libraries for equivalents (escalate 
any non-1:1 / multi-candidate / inexact choice to Mr. Das). In addition to these suggest adaptations or enhancements 
for idiomatic C# code. Locked choices, the augmented complexity node set, and approved deliberate departures 
are in `docs/decisions.md`.

## Design Options (Ox)

### O1 — Faithful structural port (Roslyn syntax-only, scheme-preserving normalizer) — RECOMMENDED
- Description: 1:1 class decomposition (`Dry4CSharp`, `Options`, `CSharpDuplicateFinder`,
  `CSharpNormalizer`, `NormalizedNode`, `Candidate`, `Location`). Roslyn **syntax-only** parse
  (no `Compilation`/semantic model), mirroring JavaParser-without-symbols. Normalizer preserves the
  Java scheme (tag = node type name; sorted markers for annotations/modifiers/operators/predefined
  types/switch/parenthesized-lambda; names & literals dropped). Fingerprints/Jaccard/sort/overlap
  ported verbatim.
- Pros: Highest fidelity to `dry4java`'s decomposition & algorithm; minimal dependencies (Roslyn +
  xUnit only); deterministic; each slice builds+tests green independently.
- Cons: Fingerprint strings & node counts are not literally comparable to `dry4java` (inherent,
  language-level); a few marker sources map imperfectly (var-decl modifiers, predefined types).
- Risk: Low-medium (isolated to the normalizer). Effort: Medium.

### O2 — "Literal fingerprint parity" (rename Roslyn kinds to JavaParser names) — REJECTED
- Description: Attempt to make fingerprints byte-identical to `dry4java` by translating Roslyn node
  kinds into JavaParser class names.
- Pros: Superficially "closer" strings.
- Cons: Ill-defined and unmaintainable — the languages have non-isomorphic grammars (structs, records,
  properties, top-level statements; no annotation-type or init-block analogs). A translation table
  would be arbitrary and fragile, and still could not make counts match.
- Risk: High. Effort: High. **Reject** — chases an impossible goal.

### O3 — Semantic-model analysis (Roslyn `Compilation` + symbols) — REJECTED
- Description: Build a `Compilation`, resolve symbols, normalize using semantic info.
- Pros: Could enable smarter, type-aware matching later.
- Cons: Diverges from `dry4java` (JavaParser is used without symbol resolution); needs reference
  assemblies/build context, is far heavier and slower, and is pure YAGNI for a fuzzy structural
  detector.
- Risk: Medium-high. Effort: High. **Reject** for the port.

**Recommended: O1** — faithful, minimal, deterministic; the only defensible reading of the fidelity
contract. (Sub-decision resolved by Mr. Das — **all** C#-only candidate roots are IN; see Risks R3.)

## Slices (Sx)

| Slice | Outcome | Depends on |
|-------|---------|------------|
| S1 | **Scaffolding.** `dry4csharp.sln`, `src/Dry4CSharp` (Exe, imports `..\..\Dry4CSharp.Common.targets`), `tests/Dry4CSharp.Tests` (imports `..\..\Dry4CSharp.Tests.Common.targets`, references src), trivial `Main` + one smoke test. Restores with lock files; builds + tests green. | - |
| S2 | **Analyzer core.** `Location`, `Candidate`, `NormalizedNode`, `CSharpNormalizer`, `Options`, `CSharpDuplicateFinder` (scan → collect → normalize → fingerprint → similarity → sort). Unit tests for normalizer/fingerprints/similarity/options. Builds + tests green. | S1 |
| S3 | **CLI + output.** `Dry4CSharp` entry (`Main`, format dispatch, `Environment.Exit(2)`, `USAGE`/`--help`), `PrintText`, `FormatCandidate`, `ToEdn`. Output/exit-code unit tests. Builds + tests green. | S2 |
| S4 | **Test-fidelity port + parity audit.** Faithful 1:1 counterparts of all 9 JUnit tests under the same names; confirm every `dry4java` test maps; align README/docs. Builds + tests green. | S3 |

## Tasks (Tx)

| #  | Slice | Task | Status  | Commit |
|----|-------|------|---------|--------|
| T1 | S1 | Create `dry4csharp.sln`. | Pending | - |
| T2 | S1 | `src/Dry4CSharp/Dry4CSharp.csproj` (`OutputType=Exe`, `net8.0`, import `..\..\Dry4CSharp.Common.targets`) + minimal `public static class` with `Main`. | Pending | - |
| T3 | S1 | `tests/Dry4CSharp.Tests/Dry4CSharp.Tests.csproj` (import `..\..\Dry4CSharp.Tests.Common.targets`, `ProjectReference` to src) + one smoke `[Fact]`. | Pending | - |
| T4 | S1 | Verify `dotnet restore` (lock files) + `dotnet build` + `dotnet test` green. | Pending | - |
| T5 | S2 | `Location` record (`file`, `startLine`, `endLine`). | Pending | - |
| T6 | S2 | `Candidate` record (`score`, `left`, `right`, `leftNodes`, `rightNodes`). | Pending | - |
| T7 | S2 | `NormalizedNode` (`NodeCount`, `Fingerprints` via `SortedSet<string>` Ordinal, `ToFingerprint`) + unit tests. | Pending | - |
| T8 | S2 | `CSharpNormalizer`: `tag` (type name minus `Syntax`), drop-set (`IdentifierName`/`QualifiedName`/`AliasQualifiedName`/`LiteralExpression`; keep `GenericName`/interpolated), markers (attributes/modifiers/operators/predefined-type/switch/parenthesized-lambda) + unit tests. | Pending | - |
| T9 | S2 | `Options` (record; ctor arity matching Java; `Parse` with `InvariantCulture`; `Defaults`) + unit tests. | Pending | - |
| T10 | S2 | `CSharpDuplicateFinder`: `.cs` enumeration (recurse, Ordinal global sort, silent-ignore), fail-fast on `Error` diagnostics, pre-order `collectEntries`, `isCandidateRoot` (core analogs **+ all C#-only roots**: struct, record struct, both lambdas, anonymous method, property, delegate, local function, indexer, event), `entry` (line span), `similarity`, stable `OrderBy` sort + unit tests. | Pending | - |
| T11 | S3 | `Dry4CSharp.Main`: parse → `--help`/`USAGE` → finder → format `switch` (`text`/`edn`/unknown→stderr+`Exit(2)`). | Pending | - |
| T12 | S3 | `PrintText` (explicit `"\n"`, empty message). | Pending | - |
| T13 | S3 | `FormatCandidate` (`F2` InvariantCulture, `"\n"`). | Pending | - |
| T14 | S3 | `ToEdn` (empty + structured; `\`/`"` escape order; raw `double` InvariantCulture) + unit tests. | Pending | - |
| T15 | S4 | Port JUnit `parsesCommandLineOptionsAndPaths`, `defaultsToSrcWhenNoPathsAreProvided`. | Pending | - |
| T16 | S4 | Port `formatsTextOutputWithLineRanges`, `printsClearMessageWhenNoTextCandidatesExist`, `printsEdn` (use `.cs` paths). | Pending | - |
| T17 | S4 | Port `reportsStructuralDuplicateCandidatesWithFileAndLineRanges`, `matchesRecordsWithDifferentNamesAndLiteralValues`, `filtersCandidatesShorterThanTheMinimumLineCount` with C# sample sources (assert the C#-sample line ranges — see A2). | Pending | - |
| T18 | S4 | Port `matchesEnumsAndConstantsStructurally` via **R2 option (a)** — a real C# `enum` with several members + relaxed thresholds (asserts enum/`EnumMember` roots match; verifies same intent). | Pending | - |
| T19 | S4 | Parity audit: confirm all 9 counterparts present & assertions mapped; align README/docs. | Pending | - |

## Risks (Rx)

- **R1 — Normalizer fidelity is the crux.** Roslyn vs JavaParser model the AST differently (tokens vs
  child nodes; operator-in-`Kind`; identifier-as-token; no annotation-type/init-block). Consequence:
  fingerprint strings and absolute `nodeCount`s are **not** comparable to `dry4java`. Mitigation:
  preserve the *scheme* (Fidelity mapping in `decisions.md`), lock the drop-set & marker sources, and
  test *structural detection* (duplicates surface) rather than absolute counts.
- **R2 — `matchesEnumsAndConstantsStructurally` has no 1:1 port.** C# enums cannot declare
  constructors, fields, or methods, so the Java sample (enum constants with args + private field +
  constructor) cannot be reproduced, and a minimal C# enum falls below `min-lines 3` / `min-nodes 8`.
  **Resolved (Mr. Das): option (a)** — a real C# `enum` with several members + relaxed thresholds
  (asserts enum/`EnumMember` roots match). Documented as verifying the same *intent* (enum + constant
  roots match structurally), not the Java sample's exact structure.
- **R3 — C#-only candidate roots change the result set.** struct / record struct / property /
  anonymous method / delegate / local function / indexer / event have no Java analog. **Resolved
  (Mr. Das): all IN** — struct, record struct, both lambdas, anonymous method, property, delegate,
  local function, indexer, event. Results diverge from `dry4java` by design (accepted).
- **R4 — Roslyn error-tolerance vs Java fail-fast.** Roslyn never throws on bad syntax. Mitigation:
  throw on any `DiagnosticSeverity.Error`; risk of over/under-failing vs JavaParser on edge inputs is
  accepted and documented.
- **R5 — Number-format parity.** `%.2f` (Java `HALF_UP`) vs .NET `F2` midpoint rounding may differ at
  exact half-way values; `Double.toString` vs `double.ToString` shortest-round-trip may differ for the
  EDN `:score` in rare cases. Mitigation: `InvariantCulture` everywhere; apply an explicit
  `MidpointRounding` for the `F2` score if a divergence is found; treat the EDN score *string* as
  structural-not-literal (the underlying double value is faithful). No ported test asserts a non-empty
  EDN score, so this is a README-example concern, not a test blocker.
- **R6 — Sort stability.** `List.Sort` is unstable; Java `List.sort` is stable. Mitigation: LINQ
  `OrderBy`/`ThenBy` chain (stable) so ties keep insertion order.
- **R7 — Line-ending / culture drift.** Mitigation: explicit `"\n"`, `InvariantCulture`,
  `StringComparer.Ordinal` (all locked).
- **R8 — Generated `.cs` noise.** Scanning a path containing `obj/`/`bin/` (e.g. `.`) would parse
  generated files (`*.g.cs`, `AssemblyInfo`), same as `dry4java` scanning `target/`. Mitigation: none
  now (faithful); see D2.

## Assumptions (Ax)

- **A1 — Syntactic-only analysis** (no `Compilation`/semantic model), mirroring JavaParser without
  symbol resolution.
- **A2 — Ported finder tests assert the C#-sample's actual line ranges.** C# sample layout differs from
  the Java sources (no `package`, different imports/using), so counterparts assert the range the C#
  sample actually produces; the preserved behavior is "a cross-file structural duplicate is detected
  with the correct range," not Java's literal `4-7`.
- **A3 — FluentAssertions** for assertions (decided by Mr. Das — matches `crap4csharp` usage: 11
  `.Should()` calls, zero `Assert.`); xUnit as the JUnit analog.
- **A4 — Visibility widened to `public`** where Java used package-private, because `internal` is banned
  and tests exercise `FormatCandidate`/`PrintText`/`ToEdn` directly.
- **A5 — EDN is hand-rolled** (no library), reproducing `dry4java`'s exact layout & escaping.
- **A6 — Scan** targets `.cs`, default `["src"]`, recursive, **Ordinal**-sorted; non-existent/non-`.cs`
  paths are silently ignored (Java parity).
- **A7 — `LanguageVersion.Latest`** (analog of `JAVA_21`).

## Deferrals (Dx)

- **D1 — Parallelizing the O(n²) pairwise comparison** (PLINQ). YAGNI, and it would threaten
  determinism/sort-stability.
- **D2 — Project/solution-aware scanning**, `bin/obj` exclusion, `.gitignore` respect.
- **D3 — Additional C#-only candidate roots** beyond the greenlit core (revisit after parity).
- **D4 — Additional output formats** (JSON/SARIF) and a single-file/AOT published binary
  (jar-equivalent).
- **D5 — Incremental/streaming** for very large repositories.

## Notes & Decisions

- **Resolved by Mr. Das:** R3 — all C#-only candidate roots IN; R2 — enum test via option (a) (enum +
  relaxed thresholds); A3 — FluentAssertions; R1 — structural-not-literal fingerprints accepted. No
  open blockers.
- `.github/copilot-instructions.md` slimmed to durable cross-cutting rules + a pointer to
  `docs/decisions.md` (the stale CRAP/coverage wording removed).
