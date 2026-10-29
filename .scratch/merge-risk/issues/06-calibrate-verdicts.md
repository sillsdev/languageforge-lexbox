# Calibrate verdicts on real PRs

Type: prototype
Status: resolved
Blocked by: 01, 03, 04

## Question

Run a rough version of the skill by hand on a handful of real lexbox PRs. Which verdict scale (including "do X, then merge") and which thresholds match Tim's own call on each PR?

## Answer

Runs in [prototype/calibration.md](../prototype/calibration.md) (#2514 pre-review, #2854, #2821). Tim accepted the verdicts and the scale: Merge / Do X, then merge / Spot-check these hunks / Needs a real review. Cost on #2821: 3 agents, ~215k tokens, ~2.5 min.

- X includes one-off deploy prerequisites (e.g. "create the secret first"), not only code.
- Red CI: check whether develop is red too; if the failure isn't the PR's, the verdict is "merge once CI is green".
- A missing test for a possible data-loss outcome is worth mentioning and can easily become part of X. Leaving it optional (as on #2821's faithful port) is a judgment call, borderline; lean towards X when the test is cheap.
- Output leads with a "Worst things that could happen" list: painful outcomes, not changes, each with likelihood, repair cost, and what would catch it. This replaces the per-change table, so nothing is said twice.
