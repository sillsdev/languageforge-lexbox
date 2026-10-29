# Map: merge-risk skill

Label: wayfinder:map

## Destination

A spec for a lexbox skill (`.claude/skills/<name>/`) that decides whether a PR (often AI-written) can merge without a line-by-line read. It weighs how risky each change is against how well tests and other signals cover it, does the clear to-dos that make the PR mergeable, and ends with a verdict. Once the map is clear, we build the skill in this same effort.

## Notes

- Scope: the whole lexbox repo. Other repos are out of scope for now.
- Settled while charting:
  - Input: the current branch against `develop` by default, or a PR number.
  - Risk: a checked-in tier map by path, adjusted by diff shape (deleted logic, serialized/public contracts, concurrency).
  - Coverage: inferred in v1 (tests added or changed per risky hunk, and existing tests that exercise the changed code), not measured.
  - Tests: run only tests the PR adds or changes, filtered, per AGENTS.md. Everything else comes from CI.
  - Trust signals: CI status and open bot findings (Devin, CodeRabbit, Claude review). Who wrote the code doesn't count.
  - Verdicts include "do X, then it's good to merge" (X can be a small change or a fast test case), along with merge / spot-check these hunks / needs a real review.
  - May do clear to-dos itself and report afterwards. Tim no longer trusts `/polish` as is, so it gets evaluated before we reuse it.
  - Fast: a few minutes and a handful of subagents. Domain agents (`fwlite-sentinel` etc.) only for high-tier files.
- Skills to consult: grilling, domain-modeling (mattpocock/skills).
- Tracker: local markdown in `.scratch/merge-risk/`, committed to the session branch.

## Decisions so far

- [Prior art for PR risk-vs-coverage triage](issues/01-prior-art.md): nobody does the coverage weighing; copy "path floor that evidence can only raise" and keep risk and coverage as separate columns.

## Not yet specified

- How it reads bot findings and CI on both the sillsdev repo and the sandbox fork.
- Skill name, trigger phrasing, and whether it hands off to the fork-stage/promote flow.
- Measuring real line coverage (coverlet / vitest) if inferred coverage turns out too weak.
- How it should test itself (a few saved PRs with known-good verdicts?).

## Out of scope

- Repos other than lexbox (harmony, fwlite-trailmap).
