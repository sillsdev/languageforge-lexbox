# /polish retro-eval: status and ground truth

The first subagent could not spawn agents, so it picked the PRs and gathered ground truth; the 16 reviewers were then dispatched from the root session exactly as `/polish` Phase 1 would (8 per PR, parallel, short briefs). Raw findings: [02-polish-raw.md](02-polish-raw.md). Phases 2+ (aggregate, fix) were not run.

## PRs used

| PR | What | Size | Pre-review commit | Base for diff |
| --- | --- | --- | --- | --- |
| #2514 | Standardize dry-run sync records, remove `Submit*` interface defaults (backend/FwLite) | 7 files, +222/-94 at pre-review state | `90b31627` | `a8df2260` |
| #2618 | Sort entry list by vernacular writing system (frontend/viewer) | 18 files, +198/-7 | `fb8c7769` | `373f5426` |

Worktrees: `/tmp/claude-0/-home-user/9eb351ce-7c95-5a17-852b-f4e950ea2fa9/scratchpad/polish-eval/2514`
and `.../2618` (detached HEAD at the pre-review commit). Local refs `refs/eval/{2605,2470,2514,2618}`
exist in the main repo from the fetch.

Why these two: most merged PRs in the window were too large (#2649 +1915, #2433 +2340,
#2470 +1853, #2605 +2471) or had only bot review. Both picks are small enough to
review whole, but the human-review signal is thin (see below), so they are a weak
test of recall. A better backend pick would need a PR under ~800 lines with human
review that forced fixes; none turned up in the top-commented list.

## Ground truth (what review actually raised)

### #2514 (backend)
Pre-review state had no tests. Follow-up commits: `32e18ff2`, `0e91e728`, `631bea87`, `c20f8253`.
- Patch summary could span multiple lines (values with CR/LF, FF, NEL, U+2028/9) and break
  per-line log search. Raised by CodeRabbit; fixed twice (first in `Summarize`, then moved
  to `RunRecord` because other records interpolate headwords/glosses directly).
- A neighbourless position was logged differently from "at the end" (fix `631bea87`, author-driven).
- A human reviewer said the new line-ending test mostly tests `String.ReplaceLineEndings`;
  author cut the cases in half.
- The `Submit*` default removal, the wrapper's 11 inherited defaults and the FwData forwarders
  drew no review comment.

### #2618 (frontend)
Follow-up: `f0c0012a` (rename only).
- Two human comments: (1) `sortedHeadword` should be `bestHeadword`; (2) doubt that the list
  should fall back to other writing systems at all (sort order can look wrong; FLEx and the
  other lists in the viewer do not fall back). Outcome: rename done, fallback kept, maybe a WS
  prefix later.
- Approval review: unclear whether the sort WS should propagate to other places (badges,
  editor header, delete prompt, entry picker). Deferred.
- No bug-class findings. Everything was design or naming.

### Observations about the ground truth
- Both PRs yield mostly design and naming feedback, which a diff reviewer can only partly
  anticipate. The one concrete defect class (multi-line text breaking a one-line log
  invariant) came from CodeRabbit, not a human.
- Candidates #2605 and #2470 have richer bot findings (nested `<button>`, stale draft in
  `$effect`, duplicate submit, path-prefix `..` check) but are over the size cap.

## Per PR: caught / missed / noise

**#2514 (backend/FwLite)**
- Caught: no tests on the pre-review state (4 agents, test-auditor named the exact tests to write). The multi-line defect CodeRabbit found (fwlite-sentinel and rename-detector, but only as 💭 nits).
- Caught, beyond human review: the validation wrapper silently skipping validation for `SubmitCreateSense`/`SubmitCreateExampleSentence` (4 agents). Real: fixed later in follow-up PR #2519. Also a *possible live bug today*: `MiniLcmApiNotifyWrapper` has no `Submit*` overrides, so a linked complex-form entry may not get a change notification (ripple-detector; not built or reproduced).
- Missed: the neighbourless-position logging fix (author found it).
- Noise: "check all 12 Submit* exist" (the build catches that), several unverified "check whether" items, async-wrapper and comment nits.

**#2618 (viewer)**
- Caught: the human "doubt the fallback" concern, best by ripple-detector with backend evidence (sort key has no fallback, rows look unsorted). The approval-review "should the ws propagate elsewhere" (4 agents). A never-failing test assertion and no test of sort order at all (test-auditor).
- Partly caught: the `sortedHeadword` rename (intent-check flagged the confusing near-duplicate name; viewer-watcher praised the name).
- Noise: empty msgstr in non-en locales, change-relative comment nits, one wrong worry (viewer-watcher's reload concern, cleared by ripple-detector).

## Cost and runtime
- 16 agents, about 875k subagent tokens (2618: ~320k, 2514: ~555k). Per agent 11 s to 218 s; wall clock about 4 min with all in parallel (fwlite-sentinel slowest).

## Agents: keep / drop (for a merge-risk skill)
- Keep: **test-auditor** (best on both PRs, concrete cheap tests), **ripple-detector** (found the deepest real issues), **fwlite-sentinel** (domain depth on the risky PR).
- Useful but overlapping: intent-check, dotnet-stylist, viewer-watcher.
- Drop for this purpose: pr-narrative, diff-hygiene, i18n-completeness, rename-detector. rename-detector and pr-narrative ignored their specialty and did a general review, which is most of the duplication.

## Skill problems found
- Phase 0's `|| fallback` after a pipe never fires, so grounding can come back empty and the skill stops on "empty diff".
- Shallow clones break the merge-base (needed `git fetch --deepen`). Base is hardcoded to `origin/develop`.
- Needs a root session: subagents cannot run it.
- Always-on agents drift out of their lane, so the same finding arrives 3-4 times; aggregation has to dedupe heavily.
- Severity is uncalibrated for merge risk: a real one-line-log defect came back as 💭 nit while several unverified guesses were ⚠️ important.

## Bottom line
- Recall on real issues was good: every human-raised substantive point was caught or partly caught, plus one real bug humans missed.
- Precision is poor for a "merge fast" goal: ~70 findings across 2 PRs, mostly nits and duplicates, with severity not tracking repair cost.
- Cost is high (~875k tokens, 16 agents). A merge-risk skill should run 2-4 agents chosen by what the change does, not 8 by path.
