---
name: Do Next Issue
description: This skill should be used when the user says "/do-next-issue", "pick an issue and do it", "find me a small issue to work on", "grab the next good issue", "do the next manageable issue", "find something users would celebrate and implement it", or otherwise asks Claude to autonomously discover a small, recently-filed, unclaimed GitHub issue and implement it. Use it only when the user has *not* named a specific issue — when an issue number or title is given, work on that one directly instead.
version: 0.1.0
---

# Do Next Issue

Find one recently-filed, unclaimed, well-scoped GitHub issue worth doing, get user buy-in, then implement it. The goal is a **manageable win** — bug fix, papercut, small maintenance task, or something users will celebrate — not a heroic feature.

## Hard constraints

- **Target size: ≤ ~200 lines of code changed.** If during implementation the scope explodes, stop and report back rather than ploughing on.
- **Never commit, push, or open a PR without explicit user approval** (per repo AGENTS.md).
- **Steer clear of 🔴 critical-path code** unless the change is genuinely tiny and surgical: FwLite sync, FwHeadless Mercurial/FwData processing, anything flagged in `backend/FwLite/AGENTS.md` or `backend/FwHeadless/AGENTS.md`.
- **Do not run integration tests** unless the user asks; rely on focused unit tests.
- Respect the project's branch naming and AGENTS.md guidance throughout.

## Workflow

The skill runs in four phases. **Stop and ask the user between phases 2 and 3, and between phases 3 and 4.** Do not skip these stops.

### Phase 1 — Discover candidates

Pull a pool of ~15–25 recent, plausible-looking issues. Use whichever GitHub access is available in this environment (in order of preference):

1. `mcp__github__list_issues` / `mcp__github__search_issues` — available in remote/web Claude Code sessions
2. `gh issue list` — available in local terminals where `gh` is installed

Filter criteria for the pool:

- `state: open`
- Created within the **last 60 days** (widen to 120 days only if the 60-day pool is too thin)
- No assignee
- Not labelled: `wontfix`, `duplicate`, `invalid`, `question`, `discussion`, `epic`, `blocked`, `needs-design`, `breaking-change`
- Sort by most recent reactions / comments where possible — engagement is a signal of real user pain

Then **for each candidate, confirm it is genuinely unclaimed**:

- No linked PRs (check the issue's timeline / `closed_by` / `pull_request` linkage)
- No remote branch whose name contains the issue number (`git ls-remote origin | grep -E "(^|/)$ISSUE_NUM(-|$)"` or `mcp__github__list_branches`)
- No open PR title or body referencing the issue number

Drop any candidate that fails these checks.

### Phase 2 — Triage in parallel with subagents

For the top ~6–10 surviving candidates, **launch parallel `Explore` (or `general-purpose`) subagents in a single message** — one per candidate. This protects the main context window from long issue bodies and lets triage happen concurrently.

Each subagent's prompt should be self-contained and ask for **under 200 words back**. Give it:

- The issue number, title, body, and most relevant comments
- Instructions to locate the likely-affected files in the codebase
- The scoring rubric below
- The reminder that this repo has critical zones (FwLite sync, FwHeadless) to flag

**Scoring rubric** (each 0–3, higher is better, except Risk which is inverted):

| Dimension     | What it measures                                                   |
|---------------|--------------------------------------------------------------------|
| **Clarity**   | Is the desired outcome obvious? Repro steps? Acceptance criteria?  |
| **Value**     | Will users notice? Bug severity, papercut frequency, polish, etc.  |
| **Size-fit**  | Realistic LOC estimate ≤ 200? Single-area change?                  |
| **Risk** (↓)  | Critical path? Schema/migration? Cross-cutting? Auth/security?     |

Subagents return: `{issue_num, one-line summary, estimated files, estimated LOC, scores, blockers, recommended approach}`.

### Phase 3 — Present and confirm

Summarise the **top 3** candidates for the user in a compact table or short list. For each, give:

- Issue number + one-line title (with link)
- Why it scored well
- Rough plan (1–2 sentences)
- Estimated size

Then use **`AskUserQuestion`** to let the user pick. Include the top recommendation first, labelled "(Recommended)". Always include "None — show me different options" as a fallback. Do not start implementing until the user picks.

### Phase 4 — Implement (only after user picks)

1. **Create the branch.** Use the project's branch convention (often `claude/<slug>` or `feat/<issue>-<slug>`). Confirm naming if ambiguous.
2. **Read the relevant AGENTS.md files** for every directory you'll touch (and their parents) — non-negotiable per repo rules.
3. **Pre-Flight Check**: if the change touches 🔴 critical code or many files, list the chain of components affected before editing. (See repo AGENTS.md.)
4. **Implement** the change. Stay within the ≤200 LOC envelope. If scope creeps, stop and report.
5. **Run focused checks**: relevant unit tests (filtered, not the full suite), type checking, lint for the touched files. Do **not** run integration tests unless asked.
6. **Show the user the diff and a brief summary.** Then ask, via `AskUserQuestion`, whether to commit, whether to push, and whether to open a PR — as **separate** decisions, in that order. Do not bundle them.

## When to abort and report instead

Be honest and stop early rather than forcing a bad outcome. Report back to the user and stop if:

- No candidate scores well across the rubric (everything is either vague, huge, risky, or already claimed)
- The chosen issue turns out, on closer inspection, to require design decisions, a schema migration, or cross-cutting refactors
- Implementation would force "fixes" that look like cheating — disabling tests, deleting assertions, papering over a real bug (see repo AGENTS.md "🛡️ VIGILANCE")
- Required context is missing and a maintainer needs to clarify the issue first

## Anti-patterns to avoid

- ❌ Picking the *first* plausible issue without triaging alternatives
- ❌ Loading every candidate's full body into the main context instead of using subagents
- ❌ Selecting feature requests dressed up as bugs
- ❌ Committing or pushing because "it's obviously fine" — the user's explicit approval is the rule
- ❌ Expanding scope mid-implementation ("while I'm here, let me also…")
- ❌ Picking a stale-looking issue where a maintainer has already said "I'll do this" in comments
- ❌ Touching FwLite CRDT sync or FwHeadless without strong justification and tiny scope

## Good candidate signatures (positive signals)

- Bug report with a reproducible steps section and a clear expected vs actual
- Typos, broken links, log noise, missing null-checks, off-by-one, error message clarity
- Small a11y or i18n fix
- Dead code removal where ownership is unambiguous
- Test that was disabled with a `// TODO` and is now easy to re-enable
- Dependency bump that's just a version string change with no API delta
- User-reported papercut with multiple thumbs-up reactions

## Notes on tool selection in this repo

- This repository runs in environments where `gh` may **not** be available (remote Claude Code on the web). Prefer `mcp__github__*` tools when present; fall back to `gh` only when running locally.
- When pulling PR review comments via `gh`, use `gh api` (per repo AGENTS.md) — `gh pr view` skips review comments.
- The user's email (`tim_haasdyk@sil.org`) and today's date are available from session context; don't fabricate them into commits.
