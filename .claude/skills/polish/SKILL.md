---
name: polish
description: Pre-merge review of the current branch. Dispatches focused subagent reviewers in parallel, aggregates findings by severity, applies unambiguous fixes confidently (strictly scoped to touched files), re-validates after fixes, asks before changing anything contested. Use before opening a PR, before requesting review, or whenever you want a thorough self-review pass.
when_to_use: User asks to "polish my branch", "get this ready to merge", "self-review", "pre-PR check", "what would the team flag here", or before opening/requesting review on a PR.
allowed-tools: Bash(git diff:*) Bash(git log:*) Bash(git status:*) Bash(git show:*) Bash(git branch:*) Bash(gh api:*) Bash(dotnet format:*) Bash(dotnet build:*) Bash(dotnet test:*) Bash(task:*) Bash(pnpm:*) Bash(node:*) Read Grep Glob Agent Edit
---

# /polish — Pre-merge review of the current branch

Open prescriptive findings with *"let's …"*, cite the source, prefer
questions to commands when uncertain. Voice in
`.claude/skills/_shared/reviewer-glossary.md`. Defer formatting and lint
diagnostics to the tools themselves rather than re-reviewing them here.

## Phase 0 · Ground

!`git status --short`
!`git branch --show-current`
!`git log --oneline origin/develop..HEAD 2>/dev/null | head -30 || git log --oneline -15`
!`git diff origin/develop...HEAD --stat 2>/dev/null | tail -40 || git diff --stat HEAD`

If the diff is empty or trivial, say so and stop. Otherwise identify
the touched top-level areas and the linked issue (branch name, existing
PR, `Resolves #N` in commits).

## Phase 1 · Dispatch parallel reviewers

Per `.claude/skills/_shared/dispatch-matrix.md`. Spawn all relevant
agents **in one message** in default "reviewing a diff" mode.

## Phase 2 · Aggregate & rank

Merge findings, dedupe, sort. Header:

```
## /polish report

**Touched:** backend/FwLite, frontend/viewer
**PR / Issue:** #2300 / Resolves #2150
**Findings:** 2 blocking · 3 important · 4 nit · 2 praise
**Verdict:** 🟡 One pass away
```

Then the table grouped by severity.

## Phase 3 · Apply

Per `.claude/skills/_shared/apply-rules.md`.

## Phase 4 · Re-validate (loop, hard cap 2)

Per `.claude/skills/_shared/apply-rules.md` §Re-validate.

## Phase 5 · Verdict & next test

- 🟢 **Ready to request review** — no blocking, no important remain.
- 🟡 **One pass away** — important findings or open questions.
- 🔴 **Substantive work remaining** — blocking findings.

Recommend the right narrow test command; offer to run it.

| Touched | Command |
| --- | --- |
| FwLite sync (`MiniLcm/SyncHelpers/**`, `FwLiteProjectSync/**`) | `dotnet test backend/FwLite/FwLiteProjectSync.Tests --filter Sena3` (~2 min) |
| FwLite (other) | `task fw-lite:test-quick` |
| viewer | `cd frontend && pnpm --filter viewer run check && pnpm --filter viewer run test:unit` |
| lexbox frontend | `cd frontend && pnpm run check && pnpm run test:unit` |
| LexBox API | `dotnet test LexBoxOnly.slnf --filter "Category!=Integration&Category!=FlakyIntegration&Category!=RequiresDb"` |
| LexBox API + GraphQL schema | also `task api:generate-gql-schema` |
| viewer + .NET types | `dotnet build backend/FwLite/FwLiteShared/FwLiteShared.csproj` |

Don't suggest integration tests unless the user asks.
