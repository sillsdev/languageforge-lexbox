# Map: merge-risk skill

Label: wayfinder:map

## Destination

A spec for a lexbox skill (`.claude/skills/<name>/`) that decides whether a PR (often AI-written) can merge without a line-by-line read. It weighs how risky each change is against how well tests and other signals cover it, does the clear to-dos that make the PR mergeable, and ends with a verdict. Once the map is clear, we build the skill in this same effort.

## Notes

- Scope: the whole lexbox repo. Other repos are out of scope for now.
- Settled while charting:
  - Input: the current branch against `develop` by default, or a PR number.
  - Risk: judged by the agent from the change itself, focused on consequences that are painful to repair (see Risk tier map ticket).
  - Coverage: inferred in v1 (tests added or changed per risky hunk, and existing tests that exercise the changed code), not measured.
  - Tests: run only tests the PR adds or changes, filtered, per AGENTS.md. Everything else comes from CI.
  - Trust signals: CI status and open bot findings (Devin, CodeRabbit, Claude review). Who wrote the code doesn't count.
  - Verdicts include "do X, then it's good to merge" (X can be a small change or a fast test case), along with merge / spot-check these hunks / needs a real review.
  - May do clear to-dos itself and report afterwards. Tim no longer trusts `/polish` as is, so it gets evaluated before we reuse it.
  - Fast: a few minutes and a handful of subagents. Domain agents (`fwlite-sentinel` etc.) only where the change looks risky.
- Skills to consult: grilling, domain-modeling (mattpocock/skills).
- Tracker: local markdown in `.scratch/merge-risk/`, committed to the session branch.

## Decisions so far

- [Prior art for PR risk-vs-coverage triage](issues/01-prior-art.md): nobody does the coverage weighing; copy "path floor that evidence can only raise" and keep risk and coverage as separate columns.
- [Risk tier map for lexbox](issues/03-risk-tier-map.md): no tier map or strict rules; the agent judges each change by how painful its worst outcome is to repair (data corruption, breaking app updates, long-term pain). Critical code can still merge.
- [Evaluate /polish on recent PRs](issues/02-evaluate-polish.md): good recall (plus one real bug humans missed), poor precision and ~875k tokens; test-auditor, ripple-detector, fwlite-sentinel earn their keep.
- [How to infer test coverage for a change](issues/04-coverage-inference.md): agent asks "would a test fail if this went wrong?" only for changes that are both plausible and painful to repair; no test asks for unlikely or low-damage ones.
- [Relationship to /polish](issues/05-relationship-to-polish.md): separate skill sharing `_shared/apply-rules.md` and the test-command table; reuses test-auditor, ripple-detector, domain sentinels; not the dispatch matrix.
- [Calibrate verdicts on real PRs](issues/06-calibrate-verdicts.md): verdict scale confirmed; X can be an ops step; red CI checked against develop; missing data-loss tests lean into X; output leads with "Worst things that could happen".
- [Which to-dos the skill does itself](issues/07-auto-fix-boundary.md): writes X's code and tests itself, may check out the branch and commit (no push); asks first for architecture and sync-flow changes; never does ops steps.

## Not yet specified

Settled as defaults in the spec (no ticket needed): CI and bot findings via `gh pr view <url>` (works for sillsdev and the fork); name `merge-risk`; the calibration runs are its reference cases.

## Out of scope

- Measuring real line coverage (coverlet/vitest) in v1.
- Hand-off into the fork-stage/promote flow.

- Fixing `/polish`'s Phase 0 `||` fallback and shallow-clone merge-base bugs (separate small task).

- Repos other than lexbox (harmony, fwlite-trailmap).
