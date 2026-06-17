---
name: implement
description: Implement an issue or task end-to-end. Reads the issue, drafts a plan with domain-agent advisory, implements with iterative testing, self-reviews via the same agent suite as /polish, and prepares the PR (without committing or pushing). Use when handed a fresh issue, branch, or task description.
when_to_use: User asks to "implement #N", "work on issue N", "do this task", "start the work for", "build out the feature", or hands off an issue or task description to be turned into code.
argument-hint: "[issue-number or short task description]"
allowed-tools: Bash(git diff:*) Bash(git log:*) Bash(git status:*) Bash(git show:*) Bash(git branch:*) Bash(git switch:*) Bash(git checkout:*) Bash(git add:*) Bash(git restore:*) Bash(git stash:*) Bash(gh api:*) Bash(dotnet *:*) Bash(pnpm *:*) Bash(node *:*) Bash(task:*) Bash(grep:*) Bash(rg:*) Read Write Edit Grep Glob Agent
disable-model-invocation: true
---

# /implement — End-to-end implementation of an issue

Plans the work with domain awareness, makes the changes, tests
iteratively, self-reviews using the same agent suite as `/polish`.
Communicates with the user in the team's collective voice
(*"let's …"*) per `.claude/skills/_shared/reviewer-glossary.md`.

## Phase 0 · Understand the issue

Identify the issue from the user's prompt (e.g. *"implement #1234"*),
branch name (`fix-1234-foo`), or task description:

```bash
# numeric → pull the issue body
gh api repos/sillsdev/languageforge-lexbox/issues/<N>
```

For a freeform description: use it directly.

**Restate the requirement in 2–3 sentences** back to the user before
proceeding. If anything is ambiguous, **ask** — root `AGENTS.md` is
explicit:

> When handling a user prompt ALWAYS ask for clarification if there
> are details to clarify, important decisions that must be made first
> or the plan sounds unwise.

Don't guess at intent; one short clarifying question saves an hour of
wrong work.

## Phase 1 · Pre-flight check

Per root `AGENTS.md`:

> Before implementing any change that will touch many files or is in
> a 🔴 Critical area (FwLite sync, FwHeadless) do a "Pre-Flight Check"
> and list every component in the chain that will be touched (e.g.,
> MiniLcm -> LcmCrdt -> FwDataBridge -> SyncHelper).

Enumerate the affected components by domain. Common chains:

- **New MiniLcm field** → `MiniLcm/Models/<Type>.cs` (with `Copy()`,
  `GetReferences()`, `RemoveReference()`) → `LcmCrdt/Objects/<Type>.cs`
  → `LcmCrdt/Changes/<NewChange>.cs` → `LcmCrdt/LcmCrdtKernel.cs`
  → `LcmCrdt/CrdtMiniLcmApi.cs`
  → `FwDataMiniLcmBridge/Api/FwDataMiniLcmApi.cs`
  → `MiniLcm/SyncHelpers/<Type>Sync.cs` → tests in `MiniLcm.Tests`,
  `LcmCrdt.Tests`, `FwDataMiniLcmBridge.Tests`,
  `FwLiteProjectSync.Tests`.
- **New GraphQL endpoint / field** → resolver / extension method
  in `backend/LexBoxApi/GraphQL/...` → projection composition check
  → schema additive concern → frontend `.NET` type regeneration if
  consumed by viewer.
- **Viewer UI change** → component(s) in `frontend/viewer/src/lib/...`
  → state / context layer → i18n strings (with `pnpm run i18n:extract`)
  → Playwright test (for new user-facing interaction) → generated
  `.NET` types if backing changes.

If the diff will touch a 🔴 Critical area, **say so plainly** in the
pre-flight output.

## Phase 2 · Plan + agent advisory

Draft a numbered plan: step N — file path — what changes — *why*.
Concrete, not aspirational. Each step references a real file.

Then dispatch the **path-relevant domain agents in plan-validation
mode** — per `.claude/skills/_shared/dispatch-matrix.md`, which says
which agents are meaningful against a plan (the diff-only mechanical
ones — `diff-hygiene`, `rename-detector`, `pr-narrative`,
`intent-check` — are skipped here). Give them the plan, not a diff.
Invocation pattern:

> Here is a proposed implementation plan (**not yet executed**).
> Walk your standards and report:
> - Steps that are missing relative to your checklist (e.g. for
>   `fwlite-sentinel`, missing fanout sites).
> - Sequencing concerns (what to do first to keep the build green
>   at each step).
> - Risks the plan doesn't acknowledge.
>
> [plan text]

Choose agents per `.claude/skills/_shared/dispatch-matrix.md` based on
the paths the plan touches. Spawn in parallel.

After collecting agent advisory:

1. Update the plan with their findings (add missing steps, reorder for
   sequencing safety, note risks).
2. **Present the refined plan to the user** and **ask for approval**.
3. Don't proceed to code until the user approves. If the user wants
   changes, iterate on the plan, not on the code.

## Phase 3 · Implement

Execute the plan step-by-step. After each meaningful chunk:

1. Run the **narrowest relevant test** for what you just changed. The
   canonical test-command table is `polish/SKILL.md` Phase 5;
   `fwlite-sentinel.md` adds sync-specific commands.
2. If a test fails, **diagnose and fix the root cause** before moving
   on. Root `AGENTS.md` §VIGILANCE is explicit:
   > NEVER "fix" a failure by removing assertions, commenting out code,
   > or changing data to match a broken implementation.
3. If you hit ambiguity not covered by the plan, **stop and ask** —
   don't power through and discover the wrong choice three steps
   later.

Per AGENTS.md, prefer IDE diagnostics over CLI tools for identifying
compiler / lint errors. Fixing diagnostics is part of completing the
step.

Follow root `AGENTS.md` §"Code comments" as you write: prefer a clearer
name or an extracted function over a comment; add one only for what the
code can't carry (a workaround, a non-obvious invariant, a
Chesterton's-fence). Don't narrate your reasoning into the file.

When the plan deviates mid-implementation (it usually does): surface
the change and the reason in your next user-facing update. Don't
silently rewrite the plan.

## Phase 4 · Self-review (same engine as `/polish`)

**Build the affected project(s)** (`dotnet build <csproj>` /
`pnpm --filter <pkg> run check`) and run the immediate tests — a green
build is the precondition for self-review:

1. Dispatch the full agent suite per
   `.claude/skills/_shared/dispatch-matrix.md` on the current diff
   (default "reviewing a diff" mode).
2. Aggregate findings; sort by severity.
3. Apply per `.claude/skills/_shared/apply-rules.md`.
4. Re-validate (hard cap 2 cycles).

Don't skip this phase even if you're confident — the agents catch
classes of issues that are easy to miss while in the implementation
flow (rename stragglers, missing fanout sites, swallowed errors).

## Phase 5 · Wrap

- Draft PR title and body via `pr-narrative`.
- Verify the CI checks listed in `pr-narrative.md` are likely to pass
  (the affected-project build + targeted tests were run in Phase 3 / 4;
  full CI suites run on the PR — don't run them locally).
- Produce a summary for the user:
  - What was done (one-paragraph lede).
  - What's pending manual verification (e.g. Android device test,
    deployment validation).
  - What follow-up issues this surfaced, if any.
  - The recommended next test command if more confidence is wanted
    before requesting review.

**Don't commit, push, or open the PR** without explicit user approval
(root `AGENTS.md`).

## Working style

- **Communicate decisions and checkpoints, not internal deliberation.**
- **Ask early.** *"Should I add a new column or extend the existing
  one?"* beats two hours in the wrong direction.
- **Reference existing patterns by file name.** *"For the read path,
  copying `UpdateSense` will keep this aligned with the CRDT projection
  pattern."*
- **Each phase has a clear handoff.** Tell the user when you're moving
  from plan to implementation, from implementation to review.
- **Don't conflate progress with completion.** If you've coded but
  haven't run tests, say "coded, not yet tested" — not "done".
