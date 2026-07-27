# Feature: dry4csharp — post-port improvements & dogfood follow-ups
**Branch:** vibe/dry4csharp-improvements (TBD)
**Status:** Backlog

## Requirements

Follow-up work after the faithful `dry4java` → `dry4csharp` port (feature `dry4csharp-port`). The port
is functionally complete, hardened (S5.5 — the independent-eval fixes), and validated end-to-end on real
code (S6 dogfood). This feature collects the refinements and deferred items that were consciously **not**
done during the port, to be designed (with Anders) and prioritized by Mr. Das. Nothing here is a
commitment yet — it is the candidate backlog.

## Context — S6 dogfood evaluation (carried over from `dry4csharp-port`)

Ran the built tool against three real C# repos — `../crap4csharp`, `../mutate4csharp`, `../dry4csharp`
(self):
- **Deterministic** (identical candidate counts across repeated runs), **robust** (`exit 0` on every
  tree, including generated `obj` code — the fail-fast parse path never tripped), and **fast**.
- Real duplication found: a genuine production dup in `crap4csharp` `ReportFormatter.cs` (58-66 ≡ 68-76,
  1.00); repetitive blocks in `mutate4csharp` `AstMutationScanner`; and `dry4csharp`'s own `src/` is
  **dup-free** (0 candidates). The bulk of full-tree hits are repetitive **test** methods (expected).
- `obj/bin` generated `.cs` are scanned but **never surface** in output — screened by the
  `min-lines`/`min-nodes` filters — so the deferred generated-noise concern is empirically negligible.
- **No bugs or regressions surfaced.** The tool is production-usable.

## Candidate backlog (to design & prioritize)

### Robustness / usability
- **B1 — `bin`/`obj` exclusion + `.gitignore` respect** (was `dry4csharp-port` D2). Skip generated
  `.cs`; consider a smarter default scan scope so pointing at a repo root doesn't drown signal in test
  duplication.
- **B2 — Graceful parse handling.** Reconsider hard fail-fast on a single unparseable file for large
  real trees (skip-with-warning option vs abort); keep the faithful default.

### Fidelity-tightening (currently **accepted departures** — revisit only if wanted)
- **B3 — EDN `:score` formatting** (Risk R5): match Java `Double.toString` (`1.0` vs C# `1`; scientific
  notation cases) for byte-exact EDN output.
- **B4 — Finer C# distinctions.** Optionally distinguish `record class` vs `record struct`, parameter
  `ref`/`out`/`in`/`params`, and accessor `set`/`init` — deliberately collapsed under the DRY Option-A
  ruling (Mr. Das). Revisit only if finer discrimination is desired.

### Performance / scale
- **B5 — Parallelize the O(n²) comparison** (was D1) for large repos, preserving determinism &
  sort-stability.
- **B6 — Incremental / streaming** analysis for very large trees (was D5).

### Features / packaging
- **B7 — Additional output formats** (JSON / SARIF) and a single-file / AOT published binary (was D4).
- **B8 — Cosmetic:** align the published executable name (`Microsoft.Dry4CSharp`) with the `dry4csharp`
  program name in the usage string.

## Notes
- These are **candidates**, not commitments. Convene Anders to design; Mr. Das prioritizes and scopes a
  slice plan when this feature is picked up.
- Source of truth for the completed port: `docs/features/dry4csharp-port.md` + `docs/decisions.md`.
