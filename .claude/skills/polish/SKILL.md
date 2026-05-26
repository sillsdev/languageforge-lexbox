---
name: polish
description: Polish the current branch so it's maximally ready for merge. Walks the diff, dispatches focused subagent reviewers (sync correctness, viewer conventions, .NET style, tests, PR narrative), aggregates findings by severity, confidently applies unambiguous fixes, and asks before changing anything reasonable people would disagree on. Use before opening a PR, before requesting review, or whenever you want a thorough self-review pass.
when_to_use: User asks to "polish my branch", "get this ready to merge", "self-review", "pre-PR check", "what would the team flag here", "make this PR-ready", or before opening/requesting review on a PR.
allowed-tools: Bash(git diff:*) Bash(git log:*) Bash(git status:*) Bash(git show:*) Bash(git branch:*) Bash(gh api:*) Bash(dotnet format:*) Bash(grep:*) Bash(rg:*) Read Grep Glob Agent Edit
disable-model-invocation: true
---

# /polish — Pre-merge review of the current branch

Bring this branch to the standard the team loves to review. Encode the taste of
@myieye and @rmunn, the hazards documented in the layered `AGENTS.md` files,
and the post-merge regressions we've actually had.

## Phase 0 · Ground (cheap, runs eagerly)

The frontmatter already injected the basics:

!`git status --short`
!`git branch --show-current`
!`git log --oneline origin/develop..HEAD 2>/dev/null | head -30 || git log --oneline -15`
!`git diff origin/develop...HEAD --stat 2>/dev/null | tail -40 || git diff --stat HEAD`

If the diff is empty or trivial (a single typo, a version bump with no
semantic effect), say so plainly and stop. Don't theatre.

Otherwise, identify the touched top-level areas (e.g. `backend/FwLite/`,
`backend/FwHeadless/`, `backend/LexBoxApi/`, `frontend/viewer/`, `frontend/`,
`deployment/`, `.github/workflows/`). This drives Phase 1.

Try to find the linked issue: branch name, PR description (if a PR exists for
this branch), and `Resolves #N` references in commit messages. Knowing the
*intent* lets the polish pass detect "you also changed X — was that
intentional?"

## Phase 1 · Read only the AGENTS.md that apply

Read `AGENTS.md` (root) always — vigilance rules and integration-test
prohibition. Then read **only** the per-area files for the areas the diff
actually touches. Don't preload all nine.

| Area touched | Also read |
| --- | --- |
| `backend/FwLite/**` | `backend/FwLite/AGENTS.md` |
| `backend/FwHeadless/**` | `backend/FwHeadless/AGENTS.md` |
| `backend/LexBoxApi/**` or `backend/LexData/**` | `backend/LexBoxApi/AGENTS.md`, `backend/AGENTS.md` |
| `frontend/viewer/**` | `frontend/viewer/AGENTS.md`, `frontend/viewer/I18N_CONTEXT_GUIDE.md` (when i18n strings change) |
| `frontend/**` (not viewer) | `frontend/AGENTS.md` |
| `.github/workflows/**` or `deployment/**` | `.github/AGENTS.md` |
| `platform.bible-extension/**` | `platform.bible-extension/AGENTS.md` |

## Phase 2 · Dispatch parallel reviewers

Spawn the workers below **in one message** (parallel) using `Agent` with
`subagent_type=Explore`. Each gets:
1. The diff scope (relevant file globs or the full diff if small).
2. The area-specific `AGENTS.md` excerpts and reference docs it needs
   (from `${CLAUDE_SKILL_DIR}/references/`).
3. The finding format below.

Keep each worker prompt under ~300 words. Workers must be self-contained —
they don't see this conversation.

### Always on

- **diff-hygiene** — debug prints (`Console.WriteLine`, `console.log`,
  `print(`, `dbg!`), commented-out code blocks, scratch files, accidental
  config (`.env`, `.DS_Store`, IDE settings), credentials/tokens/keys,
  lonely `TODO`/`FIXME` with no issue link, unused imports.
- **rename-detector** — for each symbol that *appears* renamed in the diff
  (defined under a new name + a similar identifier deleted elsewhere), grep
  the whole repo (including
  `frontend/viewer/src/lib/dotnet-types/generated-types/` and
  `FwLiteProjectSync.Tests/Snapshots/`) for stragglers. Stale generated TS
  and snapshot fixtures are a recurring miss (see PR #2202).
- **test-auditor** — see `references/dotnet-style.md` §Tests. New behavior
  has a test; bug fix has a regression test that fails without the fix;
  assertions are non-trivial; prefer `BeEquivalentTo` over `BeSubsetOf`
  where sets really should be equal; new enum/`RegressionVersion` values
  have parallel `[InlineData]` rows.
- **pr-narrative** — see `references/pr-narrative-style.md`. Pull current PR
  if any (`gh api repos/sillsdev/languageforge-lexbox/pulls?head=USER:BRANCH`).
  Audit title, body, screenshots, test plan, issue link. Suggest a rewritten
  title and body if missing or weak.

### Conditional — spawn only if the path block is touched

- **fwlite-sentinel** (any `backend/FwLite/**` or `backend/FwHeadless/**`) —
  highest-value worker. Read
  `${CLAUDE_SKILL_DIR}/references/fwlite-sync-checklist.md` and walk every
  item. Touches the highest-data-loss risk surface in the repo.

- **viewer-watcher** (any `frontend/viewer/**`) — read
  `${CLAUDE_SKILL_DIR}/references/viewer-conventions.md`. The single highest-
  priority check: **no new `try/catch` or `.catch(...)` around async
  handlers** (the global error handler covers them). Also Svelte 5 runes,
  i18n context comments, regenerated `.NET` types.

- **dotnet-stylist** (any `**/*.cs`) — read
  `${CLAUDE_SKILL_DIR}/references/dotnet-style.md`. Async/nullable/records
  hygiene; also runs `dotnet format LexBoxOnly.slnf --verify-no-changes` or
  `FwLiteOnly.slnf` as appropriate.

- **migration-detective** (any `**/Migrations/**`):
  - If you need `ON CONFLICT IGNORE`, hand-write CREATE TABLE via
    `migrationBuilder.Sql()` and pair with `CreateIndex` so the model
    snapshot stays consistent. EF Core's `CreateTable()` can't express it
    (see PR #2192).
  - Reversibility considered; `Down()` does the right thing.
  - Named GUIDs for predefined-data seed commits (see PR #2278).

- **bash-discipline** (any `**/*.sh` or `**/Dockerfile*`):
  - Paths via `dirname "$(readlink -f "$0")"`, not `$CWD`.
  - Exec bit explicit in Dockerfile when copying scripts (csproj `<None>`
    doesn't preserve it — PR #2245).
  - `[[ ]]` over `[ ]` where reasonable; `||` not `-o`.
  - `set -e` rather than `set -o pipefail` with `grep` (grep exits 1 on no
    match and false-fails the pipeline).
  - `grep -c` counts lines, not matches — use `grep -o … | wc -l` for total.

### Finding format (all workers use this)

```
🚫 blocking · backend/FwLite/LcmCrdt/Foo.cs:142
  Added `DeletedAt is null` filter on the projected Entry DbSet. Harmony
  already strips tombstones from projected tables (backend/FwLite/AGENTS.md
  §Harmony Projected Tables, PR #2286) — this filter is dead code at best,
  hides real bugs at worst.
```

Severities:
- 🚫 **blocking** — must fix before merge. Data-loss hazards, validation
  layered wrong, incomplete rename, try/catch in viewer, failing tests,
  secrets in diff.
- ⚠️ **important** — fix before requesting review unless explicitly waived.
  Missing tests, missing PR sections, weak assertions, misleading names.
- 💭 **nit** — taste; reviewer would mention but not block.
- ✨ **praise** — genuinely good things in the diff. Use this. It's not
  flattery — it calibrates the report and makes the team happy to read it.

## Phase 3 · Aggregate, rank, present

Merge all subagent findings into one table. Dedupe. Sort: blocking →
important → nit → praise. Output as a single readable report.

Top of report:

```
## /polish report

**Touched areas:** backend/FwLite, frontend/viewer
**Linked issue/PR:** #2300 (or "not yet opened")
**Findings:** 2 blocking · 3 important · 4 nit · 2 praise
**Verdict:** 🟡 One pass away
```

Then the findings table, grouped by severity.

## Phase 4 · Apply confidently, ask on the rest

Two passes:

**Pass A — apply without asking.** Any change where the right answer is
unambiguous and supported by an `AGENTS.md` rule, a documented pattern, or a
historical regression. Examples:
- Remove `Console.WriteLine` / `console.log` debug calls.
- Remove unused imports.
- Remove commented-out code blocks.
- Run `dotnet format` on touched files if `--verify-no-changes` fails.
- Run `pnpm run format` on touched viewer files.
- Remove a `try/catch` in `frontend/viewer/**` that just logs/swallows
  (`AGENTS.md` global error handler rule).
- Remove a `DeletedAt is null` filter on a projected `DbSet`
  (`AGENTS.md` §Harmony Projected Tables).
- Replace `.Result` / `.Wait()` / `.GetAwaiter().GetResult()` with `await`
  in async-reachable code paths.
- Fix missed renames — update generated TypeScript, snapshot fixtures, and
  string interpolations to match the new symbol name.
- Replace `BeSubsetOf` with `BeEquivalentTo` where the test clearly expects
  set equality.
- Tighten PR title/body if there is no PR yet — propose, but don't push.

After Pass A, briefly list what was changed. Don't re-describe each finding;
the table already covered it.

**Pass B — ask before applying.** Anything where a reasonable reviewer might
prefer a different answer:
- Architecture moves (e.g. promoting a validation check into the Validation
  wrapper).
- New tests beyond a one-liner — show the proposed test, ask.
- Comment additions — show the proposed comment, ask.
- Renaming a public API or symbol referenced widely.
- Editing the PR body on an already-open PR.
- Anything touching the sync flow in
  `backend/FwLite/FwLiteProjectSync/CrdtFwdataProjectSyncService.cs` or
  `MiniLcm/SyncHelpers/`.

Ask one decision at a time. Use `AskUserQuestion` for binary or short-list
decisions; otherwise plain text.

**Never** (without explicit user approval):
- Commit, push, open a PR.
- Run integration tests (root `AGENTS.md` rule).
- Touch `harmony` submodule contents.
- Modify `.git/`, `deployment/` PVCs, or any infrastructure state.

## Phase 5 · Verdict & next-test recommendation

End with one of:

- 🟢 **Ready to request review.** No blocking, no important remain after
  Pass A/B.
- 🟡 **One pass away.** Important findings remain (or important questions
  the user just answered). List the concrete next steps.
- 🔴 **Substantive work remaining.** Blocking findings remain. Show them
  first.

Then recommend the right narrow test command. Offer to run it.

| Area touched | Command (fast, safe) |
| --- | --- |
| FwLite sync-affecting (`MiniLcm/SyncHelpers/**`, `FwLiteProjectSync/**`) | `dotnet test backend/FwLite/FwLiteProjectSync.Tests --filter Sena3` (~2 min, gold standard) |
| FwLite (other) | `task fw-lite:test-quick` |
| viewer touched | `pnpm --filter viewer run check && pnpm --filter viewer run test:unit` |
| lexbox frontend | `cd frontend && pnpm run check && pnpm run test:unit` |
| LexBox API | `dotnet test LexBoxOnly.slnf --filter "Category!=Integration&Category!=FlakyIntegration"` |
| LexBox API + GraphQL schema | also `task api:generate-gql-schema` to confirm no diff |
| viewer + .NET types | `dotnet build backend/FwLite/FwLiteShared/FwLiteShared.csproj` to regenerate TS types |

**Never** suggest integration tests unless the user asks (root `AGENTS.md`).

## Working style

- **Explain *why*, not just *what*.** "Missing rename in generated TS — has
  slipped past CI twice (#2202, #2270)" beats "RENAME INCOMPLETE."
- **Don't manufacture findings.** A clean diff gets a clean report. Padding
  with nits to look thorough erodes the team's trust in the skill.
- **Praise is a real severity.** Use it sparingly but use it.
- **Subagent prompts must be self-contained** — they don't see this
  conversation. Include the diff scope, the relevant `AGENTS.md` excerpt or
  reference path, and the finding format.
- **Reviewer voice to emulate**, in order of weight: @myieye (architecture,
  frontend idioms, taste — uses ⛏️/🔧/❓/🤔 prefixes) and @rmunn (sync,
  .NET, naming rigor — both prefer questions over commands when uncertain).
  See `references/reviewer-glossary.md` for the prefix system.

## Reference docs (loaded by subagents on demand)

- `${CLAUDE_SKILL_DIR}/references/fwlite-sync-checklist.md`
- `${CLAUDE_SKILL_DIR}/references/viewer-conventions.md`
- `${CLAUDE_SKILL_DIR}/references/dotnet-style.md`
- `${CLAUDE_SKILL_DIR}/references/pr-narrative-style.md`
- `${CLAUDE_SKILL_DIR}/references/reviewer-glossary.md`
