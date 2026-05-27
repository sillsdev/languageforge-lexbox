---
name: polish
description: Pre-merge polish for the current branch. Walks the diff, dispatches focused subagent reviewers in parallel, aggregates findings by severity, applies unambiguous fixes confidently, asks before changing anything contested. Use before opening a PR, before requesting review, or whenever you want a thorough self-review pass.
when_to_use: User asks to "polish my branch", "get this ready to merge", "self-review", "pre-PR check", "what would the team flag here", or before opening/requesting review on a PR.
allowed-tools: Bash(git diff:*) Bash(git log:*) Bash(git status:*) Bash(git show:*) Bash(git branch:*) Bash(gh api:*) Bash(dotnet format:*) Bash(grep:*) Bash(rg:*) Read Grep Glob Agent Edit
disable-model-invocation: true
---

# /polish — Pre-merge review of the current branch

Encode the hazards in the layered `AGENTS.md` files and the post-merge
regressions we've actually had. Open prescriptive findings with *"let's
…"*, cite the source, prefer questions to commands when uncertain. See
`references/reviewer-glossary.md` for voice. Defer formatting and lint
diagnostics to the tools themselves rather than re-reviewing here.

## Phase 0 · Ground

!`git status --short`
!`git branch --show-current`
!`git log --oneline origin/develop..HEAD 2>/dev/null | head -30 || git log --oneline -15`
!`git diff origin/develop...HEAD --stat 2>/dev/null | tail -40 || git diff --stat HEAD`

If the diff is empty or trivial, say so and stop. Otherwise identify the
touched top-level areas (drives Phase 1) and try to find the linked
issue (branch name, existing PR, `Resolves #N` in commit messages).

## Phase 1 · Read only the AGENTS.md that apply

Root `AGENTS.md` always. Then only the per-area files for touched paths.

| Touched | Also read |
| --- | --- |
| `backend/FwLite/**` | `backend/FwLite/AGENTS.md` |
| `backend/FwHeadless/**` | `backend/FwHeadless/AGENTS.md` |
| `backend/LexBoxApi/**` or `backend/LexData/**` | `backend/LexBoxApi/AGENTS.md`, `backend/AGENTS.md` |
| `frontend/viewer/**` | `frontend/viewer/AGENTS.md`, `I18N_CONTEXT_GUIDE.md` if i18n changes |
| `frontend/**` (not viewer) | `frontend/AGENTS.md` |
| `.github/workflows/**` or `deployment/**` | `.github/AGENTS.md` |
| `platform.bible-extension/**` | `platform.bible-extension/AGENTS.md` |

## Phase 2 · Dispatch parallel reviewers

Spawn the workers below in one message via `Agent` with
`subagent_type=Explore`. Each receives the diff scope, its reference
doc(s), and the finding format. Workers are self-contained.

**Always on:**
- **diff-hygiene** — debug prints, commented-out code, scratch files,
  accidental config, secrets, lonely `TODO`/`FIXME`, unused imports.
- **rename-detector** — for each apparent rename, grep the whole repo
  (especially `frontend/viewer/src/lib/dotnet-types/generated-types/`
  and `FwLiteProjectSync.Tests/Snapshots/`) for stragglers (PR #2202).
- **test-auditor** — reads `references/dotnet-style.md` §Tests.
- **pr-narrative** — reads `references/pr-narrative-style.md`.

**Conditional (only if the path block is touched):**
- **fwlite-sentinel** (`backend/FwLite/**`, `backend/FwHeadless/**`) — reads `references/fwlite-sync-checklist.md`. Highest-data-loss surface.
- **viewer-watcher** (`frontend/viewer/**`) — reads `references/viewer-conventions.md`.
- **dotnet-stylist** (`**/*.cs`) — reads `references/dotnet-style.md`. Also runs `dotnet format LexBoxOnly.slnf --verify-no-changes` (or `FwLiteOnly.slnf`).
- **migration-detective** (`**/Migrations/**`) — `ON CONFLICT IGNORE` needs `Sql()` + parallel `CreateIndex` (PR #2192); reversible `Down()`; named GUIDs for seeds (PR #2278).
- **bash-discipline** (`**/*.sh`, `**/Dockerfile*`) — `dirname "$(readlink -f "$0")"` not `$CWD`; chmod in Dockerfile (PR #2245); `[[ ]]` over `[ ]`; `set -e` not `set -o pipefail` with grep; `grep -c` ≠ match count.

**Finding format:**

```
🚫 blocking · path/file.cs:142
  Concise why-it-matters with a citation (AGENTS.md §, PR #).
```

Severities: 🚫 blocking · ⚠️ important · 💭 nit · ✨ praise. Detailed
severity-to-pattern mapping lives in the per-area reference docs.

## Phase 3 · Aggregate, rank, present

Merge findings, dedupe, sort by severity. Header:

```
## /polish report

**Touched:** backend/FwLite, frontend/viewer
**PR:** #2300 (or "not yet opened")
**Findings:** 2 blocking · 3 important · 4 nit · 2 praise
**Verdict:** 🟡 One pass away
```

## Phase 4 · Apply, ask, never push

**Apply directly** anything unambiguous — debug prints, unused imports,
formatting, a `try/catch` in `frontend/viewer/**` that just logs, a
`DeletedAt is null` filter on a projected DbSet, `.Result`/`.Wait()` in
async-reachable code, missed renames in generated TS/snapshots,
`BeSubsetOf` where set-equality is clearly intended. Briefly list what
changed.

**Ask** on anything where a reasonable reviewer might disagree —
architecture moves, new tests beyond a one-liner, comment additions,
public API renames, edits to an already-open PR body, anything touching
`FwLiteProjectSync/CrdtFwdataProjectSyncService.cs` or
`MiniLcm/SyncHelpers/`.

**Without explicit user approval, don't:** commit, push, open a PR, run
integration tests (root `AGENTS.md`), touch `backend/harmony` submodule,
modify deployment state.

## Phase 5 · Verdict & next test

- 🟢 **Ready to request review** — no blocking, no important remain.
- 🟡 **One pass away** — important findings or open questions. List
  concrete next steps.
- 🔴 **Substantive work remaining** — blocking findings; show first.

Recommend the right narrow test command; offer to run it.

| Touched | Command |
| --- | --- |
| FwLite sync (`MiniLcm/SyncHelpers/**`, `FwLiteProjectSync/**`) | `dotnet test backend/FwLite/FwLiteProjectSync.Tests --filter Sena3` (~2 min) |
| FwLite (other) | `task fw-lite:test-quick` |
| viewer | `pnpm --filter viewer run check && pnpm --filter viewer run test:unit` |
| lexbox frontend | `cd frontend && pnpm run check && pnpm run test:unit` |
| LexBox API | `dotnet test LexBoxOnly.slnf --filter "Category!=Integration&Category!=FlakyIntegration"` |
| LexBox API + GraphQL schema | also `task api:generate-gql-schema` |
| viewer + .NET types | `dotnet build backend/FwLite/FwLiteShared/FwLiteShared.csproj` to regenerate TS types |

Don't suggest integration tests unless the user asks.

Subagent prompts must be self-contained (they don't see this
conversation) — include the diff scope, the reference doc path, and the
finding format.
