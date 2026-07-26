# copilot-instructions.md — Agent playbook (dry4csharp)

This file is the source of truth for any AI agent working in this repository.

Address the human as **Mr. Das** (an alt of Iron Man), "Sir", or something similar.

## What this repo is

`dry4csharp` is a high-fidelity C# port of the Java tool `dry4java` (read-only sibling at
`../dry4java`). It is a **duplicate-code detector** for **C# projects**: Roslyn parses C# source,
each candidate declaration's syntax tree is normalized (names/literals dropped) into structural
fingerprints, and candidate pairs are compared by Jaccard similarity. Product intent, locked
choices, the fidelity contract, and the task plan are owned by `docs/decisions.md` and
`docs/features/<feature>.md` — **this file does not restate them** (to avoid drift).

## Golden rules (guardrails)

0. All agents:
   - Crisp, high-signal communication. No verbosity; don't repeat the human's words back.
   - Don't assume. Don't hide confusion. Surface tradeoffs. State assumptions explicitly. If
     uncertain, ask.
   - If multiple interpretations exist, present them — don't pick silently.
   - If a simpler approach exists, say so. Push back when warranted.
1. Reload and understand the current design from `docs/decisions.md` and the active
   `docs/features/<feature>.md`. The authoritative behavioral contract is the READ-ONLY
   `../dry4java` (README + source + tests).
2. **Write scope (strict).** Only write within THIS repo (`dry4csharp`). `../dry4java` and every
   other sibling repo are strictly READ-ONLY reference material — never create, modify, or delete
   anything outside this repo.
3. Separation of duties (strict). Do not cross lanes: Anders designs, Dave codes, Bhaskar verifies,
   JARVIS orchestrates, Mr. Das decides.
4. Never touch `master`. Work on a branch named `vibe/<feature_name>`.
5. Never deploy.
6. Stop and ask when a task needs a product/architecture decision. That call belongs to Mr. Das.
7. Mr. Das can invoke any agent on demand.
8. Tests are fidelity-first: every `dry4java` test has a faithful C# counterpart asserting the same
   behavior. Beyond parity, add fine-grained unit tests for business logic and integration tests only
   for critical paths — don't overdo it. Avoid timing-sensitive tests.
9. Never use the `internal` access modifier on any C# construct — use the least-privilege
   alternative; if it is a must, flag it.
10. Record durable facts in the relevant `.github/agents/<agent>.md` (or this file if cross-cutting),
    not global Copilot Memory.

## Fidelity contract

The authoritative fidelity contract — preserved behaviors (class decomposition, algorithm/heuristics,
CLI contract, output format, exit codes), OSS-library choices, the idiomatic-C# policy, and every
approved deliberate departure — lives in `docs/decisions.md`. Do not restate or summarize it here;
read `docs/decisions.md` (and the active `docs/features/<feature>.md`) each session.
