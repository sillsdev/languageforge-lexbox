---
name: merge-risk
description: Decide whether a PR (often AI-written) can merge without a line-by-line read. Looks only for outcomes that would be painful to repair, checks whether a test or CI would catch them, does the small to-dos that make the PR mergeable (and commits them), then gives a verdict. Use before merging a vibed PR.
when_to_use: User asks "can I merge this", "is this safe to merge", "merge risk", "what could go wrong if I merge", "/merge-risk", or wants a fast merge decision on a PR instead of a full review.
argument-hint: "[PR number or URL; default: the current branch]"
allowed-tools: Bash(git *:*) Bash(gh *:*) Bash(dotnet *:*) Bash(pnpm *:*) Bash(task:*) Bash(.github/scripts/ci-logs.sh:*) Read Write Edit Grep Glob Agent
---

# /merge-risk — can this merge without a line-by-line read?

Care only about outcomes that are **painful to repair**:

- data corruption or loss (CRDT history, sync and FwData writes, hg repos, the DB)
- breaking app updates (old clients, serialized changes, sync and API contracts, migrations)
- long-term pain a new release can't undo

Anything a follow-up release fixes is low stakes: mention it only if it truly helps. Judge the change itself, not which files it touches: critical code can merge when the change is safe or well tested, and a small diff can still be dangerous. Never post to GitHub.

## 1 · Ground

- **Target**: the argument (PR number or URL), else `gh pr view --json number,url` for the current branch, else the current branch against `origin/develop`. Fork PRs need the PR URL with `gh`.
- **Check out** the PR branch if it isn't current (`gh pr checkout <n>`). If the working tree has uncommitted work, use a worktree instead (`git worktree add`).
- **Diff** against the merge base with the base branch. In a shallow clone, `git fetch --deepen=200 origin <base>` until `git merge-base` succeeds; never review an empty diff by mistake.
- **PR body and linked issue**: what is this supposed to do?
- **CI on the current head**: `gh pr view <n> --json headRefOid,statusCheckRollup`. Results for an older head don't count. For red jobs follow `.github/AGENTS.md` (`ci-logs.sh`, then "Flaky or systemic?" against develop).
- **Unresolved bot findings** (Devin, CodeRabbit, Claude review) on the current head, via the review-thread recipe in `.github/AGENTS.md`. They are claims to check, not facts.

## 2 · Find the painful outcomes

Read the diff yourself. For each change ask: what is the worst plausible outcome, and how hard is it to repair? Prompts, not rules:

- serialized shapes: change types, `$type`, JSON names, model versions, anything old clients or stored history must still read
- sync and write paths, including wrappers that must see every write (validation, notify, normalization)
- deletes, moves and resets of user data; EF migrations and schema
- contracts between FwLite clients and the server (API, GraphQL, sync protocol)
- deploy prerequisites: new secrets, config, rollout order, rollback
- behavior moved into another repo or service: read that code too

`backend/FwLite/AGENTS.md` and `backend/FwHeadless/AGENTS.md` say where data loss has happened before. If nothing could be painful, skip to step 5.

## 3 · Dispatch 2-4 agents

Pick by the candidate outcomes, all in one message:

- `test-auditor`: almost always, to answer "would a test fail?"
- `ripple-detector`: effects beyond the diff, and parity when code is ported
- one domain agent only where a candidate sits in its area: `fwlite-sentinel`, `fwheadless-sentinel`, `harmony-sentinel`, `migration-detective`, `deployment-infra`, `graphql`

Brief each in under 150 words: read-only; how to get the diff; the candidates; the "painful to repair" bar above; skip everything else; return at most 5 items as *outcome · cause · likelihood · repair cost · evidence or what would catch it*, or "none". Before a finding goes into the verdict, confirm it yourself in the code.

## 4 · Weigh coverage

For each painful outcome: if it happened, would a test fail? Run only the tests the PR adds or changes, filtered (root `AGENTS.md` → Testing; commands in `_shared/test-commands.md`). Integration tests that only run in CI count when CI ran them green on the current head. A missing test for a data-loss outcome usually belongs in X when it's cheap. Never ask for tests of outcomes that are unlikely or cheap to repair.

## 5 · Verdict

- **Merge**
- **Do X, then merge**: X is small. A fast test, a guard, a one-line fix, or a one-off deploy step (create a secret, roll out A before B). Red CI caused by this PR is part of X.
- **Merge once CI is green**: CI is red for a reason that isn't this PR's.
- **Spot-check these hunks, then merge**: name each hunk and what to look for.
- **Needs a real review**: say why.

## 6 · Do X

Do the code and test parts of X yourself, following `_shared/apply-rules.md`, with two differences for this skill: writing the tests X needs requires no asking, and committing is allowed. Still ask first for the apply-rules "ask before" items (architecture moves, sync-flow code). Never do ops steps (secrets, k8s, deploys); leave them in the report.

Run the new tests, format touched files, and commit on the PR branch with a plain message. Don't push. Then re-check only the outcomes X touched, once, and update the verdict. No second round.

## Output

Plain language, no severity emoji, nothing beyond these sections:

1. **Verdict**, with X in a line or two.
2. **Worst things that could happen**, worst first. Each: outcome · the change that could cause it · likelihood · repair cost · what would catch it (test name, CI job, or "nothing"). Write "Nothing painful" if empty.
3. **Also noted**: one line per item that isn't painful but truly helps. Usually empty.
4. **Done**: commits made, tests run and their results, and steps left for the user (push, ops steps).
