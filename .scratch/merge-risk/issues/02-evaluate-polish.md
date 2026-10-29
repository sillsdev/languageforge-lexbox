# Evaluate /polish on recent PRs

Type: task
Status: resolved
Blocked by:

## Question

Run `/polish` (or its dispatch matrix and agents) against 2-3 recent lexbox PRs whose review outcome we know. Record what it caught, what it missed, false positives, runtime, and which agents earned their keep. This produces facts for "Relationship to /polish"; it decides nothing itself.

## Answer

Facts in [research/02-polish-eval.md](../research/02-polish-eval.md). Across #2514 and #2618, `/polish`'s reviewers caught or partly caught every substantive human review point and one real bug humans missed (validation wrapper gap, fixed later in #2519), but produced ~70 mostly duplicate or nit findings at ~875k tokens. test-auditor, ripple-detector and fwlite-sentinel did the useful work; pr-narrative, diff-hygiene, i18n-completeness and rename-detector added noise for this purpose. Severity did not track repair cost. Two skill bugs: a dead `||` fallback in Phase 0 and shallow-clone merge-base failures.
