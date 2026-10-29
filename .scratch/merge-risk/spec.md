# Spec: /merge-risk

Collapsed from [map.md](map.md); each decision's detail lives in its ticket.

## What it does
Decides whether a lexbox PR (often AI-written) can merge without a line-by-line read. It looks only for outcomes that are painful to repair (data corruption or loss, breaking app updates, long-term pain a release can't undo), checks whether anything would catch them, does the small to-dos that make the PR mergeable, and ends with a verdict.

## Behavior
- **Input**: PR number or URL, else the current branch's PR, else the current branch vs `develop`. Checks out the PR branch itself (worktree if the tree is dirty).
- **Risk**: agent judgment on the change itself; paths and diff shapes are prompts, never rules. Critical code can still merge. ([03](issues/03-risk-tier-map.md))
- **Coverage**: "if this went wrong, would a test fail?", asked only for outcomes that are plausible and painful. Runs only tests the PR adds or changes; CI-only tests count if green on the current head. ([04](issues/04-coverage-inference.md))
- **Signals**: CI on the current head (red is compared with develop) and unresolved bot findings, treated as claims to verify.
- **Agents**: 2-4 picked by the candidate outcomes: test-auditor, ripple-detector, and a domain sentinel only where relevant. Not `/polish`'s dispatch matrix. ([02](issues/02-evaluate-polish.md), [05](issues/05-relationship-to-polish.md))
- **Verdicts**: Merge · Do X, then merge · Merge once CI is green · Spot-check these hunks, then merge · Needs a real review. X can be code, a cheap data-loss test, or a one-off deploy step. ([06](issues/06-calibrate-verdicts.md))
- **Acting**: does X's code and tests itself and commits (no push); asks first for architecture and sync-flow changes; never does ops steps; one re-check round. ([07](issues/07-auto-fix-boundary.md))
- **Output**: Verdict and X → Worst things that could happen (outcome · cause · likelihood · repair cost · caught by) → Also noted → Done.

## Shared with /polish
`_shared/apply-rules.md` (with the overrides above) and `_shared/test-commands.md` (moved out of `/polish` Phase 5).

## Reference cases
[prototype/calibration.md](prototype/calibration.md): #2514 (pre-review), #2854, #2821.
