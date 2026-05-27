---
name: polish
description: Pre-merge polish for the current branch. Dispatches focused subagent reviewers in parallel, aggregates findings by severity, applies unambiguous fixes confidently (scoped strictly to touched files), re-validates after fixes, asks before changing anything contested. Use before opening a PR, before requesting review, or whenever you want a thorough self-review pass.
when_to_use: User asks to "polish my branch", "get this ready to merge", "self-review", "pre-PR check", "what would the team flag here", or before opening/requesting review on a PR.
allowed-tools: Bash(git diff:*) Bash(git log:*) Bash(git status:*) Bash(git show:*) Bash(git branch:*) Bash(gh api:*) Bash(dotnet format:*) Bash(pnpm:*) Bash(node:*) Read Grep Glob Agent Edit
disable-model-invocation: true
---

# /polish — Pre-merge review of the current branch

Open prescriptive findings with *"let's …"*, cite the source, prefer
questions to commands when uncertain. See
`references/reviewer-glossary.md` for voice. Defer formatting and lint
diagnostics to the tools themselves rather than re-reviewing here.

## Phase 0 · Ground

!`git status --short`
!`git branch --show-current`
!`git log --oneline origin/develop..HEAD 2>/dev/null | head -30 || git log --oneline -15`
!`git diff origin/develop...HEAD --stat 2>/dev/null | tail -40 || git diff --stat HEAD`

If the diff is empty or trivial, say so and stop. Otherwise identify the
touched top-level areas (drives dispatch) and the linked issue (branch
name, existing PR, `Resolves #N` in commits).

## Phase 1 · Dispatch parallel reviewers

Spawn all relevant agents **in one message** via `Agent` with the
`subagent_type` listed. Each agent is self-contained — its system prompt
+ your invocation prompt is all it gets. Keep invocation prompts under
~150 words; include the diff scope and the finding format.

**Always on:**

| Agent | Purpose |
|---|---|
| `polish-diff-hygiene` | Debug prints, scratch files, secrets, lonely TODOs. |
| `polish-rename-detector` | Stale generated TS / snapshot fixtures after renames; blast-radius reporting. |
| `polish-test-auditor` | Assertion quality, regression-test coverage, BeSubsetOf vs BeEquivalentTo. |
| `polish-pr-narrative` | Title/body audit; drafts if no PR exists. |
| `polish-intent-check` | Linked-issue vs diff — addresses claim? scope drift? |

**Conditional (only if path block is touched):**

| Touched | Agent |
|---|---|
| `backend/FwLite/**`, `backend/FwHeadless/**` | `polish-fwlite-sentinel` |
| `frontend/viewer/**` | `polish-viewer-watcher` |
| `**/*.cs` | `polish-dotnet-stylist` |
| `**/Migrations/**` | `polish-migration-detective` |
| `**/*.sh`, `**/Dockerfile*` | `polish-bash-discipline` |

Severity ladder: 🚫 blocking · ⚠️ important · 💭 nit · ✨ praise. Finding
format:

```
🚫 blocking · path/file.cs:142
  Concise why-it-matters with a citation (AGENTS.md §, PR #).
```

## Phase 2 · Aggregate & rank

Merge findings, dedupe, sort. Report header:

```
## /polish report

**Touched:** backend/FwLite, frontend/viewer
**PR / Issue:** #2300 / Resolves #2150
**Findings:** 2 blocking · 3 important · 4 nit · 2 praise
**Verdict:** 🟡 One pass away
```

Then the table grouped by severity.

## Phase 3 · Apply (strictly scoped)

**Apply directly** anything unambiguous and AGENTS.md-backed:

- Remove debug prints, unused imports, commented-out code blocks.
- Remove a `try/catch` in `frontend/viewer/**` that just logs / swallows.
- Remove a `DeletedAt is null` filter on a projected DbSet.
- Replace `.Result` / `.Wait()` with `await` in async-reachable paths.
- Fix missed renames in generated TS / snapshot fixtures (rebuild rather
  than hand-edit when possible).
- Replace `BeSubsetOf` with `BeEquivalentTo` where set-equality is
  clearly intended.

**Formatter runs are strictly scoped to touched files**, never the whole
solution / filter. Skip entirely if the diff touched `.editorconfig` or
prettier config (changing config + running format is the unrelated-
changes anti-pattern).

`.cs`:

```bash
dotnet format whitespace --verify-no-changes --include <touched .cs files>
# if non-zero:
dotnet format whitespace --include <touched .cs files>
```

Viewer / frontend:

```bash
pnpm --filter viewer exec prettier --check <touched files>
# if non-zero:
pnpm --filter viewer exec prettier --write <touched files>
```

**Ask** on anything a reasonable reviewer might disagree about:
- Architecture moves.
- New tests beyond a one-liner.
- Comment additions.
- Public API renames.
- Edits to an already-open PR body.
- Anything in `FwLiteProjectSync/CrdtFwdataProjectSyncService.cs` or
  `MiniLcm/SyncHelpers/`.

**Don't, without explicit user approval:** commit, push, open a PR, run
integration tests (root `AGENTS.md`), touch `backend/harmony` submodule,
modify deployment state.

## Phase 4 · Re-validate after fixes (loop, hard cap 2)

After applying fixes in Phase 3, run a narrow re-validation:

- Only the agents whose domain you touched in fixes.
- Only the *files* you modified.
- Hard cap: 2 cycles. If cycle 2 finds new findings, surface them but
  **don't auto-fix** — that's how feedback loops happen.

Common regressions to look for:
- Removed a `try/catch` → exposed a different error path. Re-run
  `polish-viewer-watcher`.
- Completed a rename → new straggler now visible. Re-run
  `polish-rename-detector` on touched files.
- Applied dotnet format → a comment moved off the line it referenced.
  Re-run touched-file `polish-diff-hygiene`.

Phase 4 surfaces nothing → proceed to verdict.

## Phase 5 · Verdict & next test

- 🟢 **Ready to request review** — no blocking, no important remain.
- 🟡 **One pass away** — important findings or open questions.
- 🔴 **Substantive work remaining** — blocking findings.

Recommend the right narrow test command; offer to run it.

| Touched | Command |
| --- | --- |
| FwLite sync (`MiniLcm/SyncHelpers/**`, `FwLiteProjectSync/**`) | `dotnet test backend/FwLite/FwLiteProjectSync.Tests --filter Sena3` (~2 min) |
| FwLite (other) | `task fw-lite:test-quick` |
| viewer | `pnpm --filter viewer run check && pnpm --filter viewer run test:unit` |
| lexbox frontend | `cd frontend && pnpm run check && pnpm run test:unit` |
| LexBox API | `dotnet test LexBoxOnly.slnf --filter "Category!=Integration&Category!=FlakyIntegration"` |
| LexBox API + GraphQL schema | also `task api:generate-gql-schema` |
| viewer + .NET types | `dotnet build backend/FwLite/FwLiteShared/FwLiteShared.csproj` |

Don't suggest integration tests unless the user asks.
