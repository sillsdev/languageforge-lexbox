# /polish retro-eval: status and ground truth

**Result: the reviewer phase did NOT run.** This was executed by a subagent that has
no `Agent` tool (all 16 dispatch calls failed with "No such tool available: Agent").
No reviewer agent produced a finding, so there are no caught/missed numbers.
Nothing below is a simulated review. What is here: the PR picks, the pinned
pre-review commits, the human-review ground truth, and skill problems found by
reading SKILL.md and the matrix. The eval needs re-dispatch from a session that
can spawn agents; worktrees are left in place for that.

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

## Per PR: caught / missed / noise, runtime, agents run

| | #2514 | #2618 |
| --- | --- | --- |
| Agents the matrix selects | 8: diff-hygiene, rename-detector, ripple-detector, test-auditor, pr-narrative, intent-check, fwlite-sentinel, dotnet-stylist | 8: diff-hygiene, rename-detector, ripple-detector, test-auditor, pr-narrative, intent-check, viewer-watcher, i18n-completeness |
| Agents actually run | 0 | 0 |
| Caught / missed / noise | not measured | not measured |
| Runtime | not measured | not measured |

Matrix selection was derived from the touched paths and is deterministic. Not selected
in either: graphql, harmony-sentinel, fwheadless-sentinel, lexbox-frontend, migration-detective,
bash-discipline, ci-workflow, deployment-infra.

## Agents: keep / drop

No evidence collected. Only a structural note: `i18n-context-writer` and
`i18n-translation-reviewer` are not in the dispatch matrix (they are used by
`crowdin-merge`), so `/polish` never runs them. Not a defect, but the agents directory
and the matrix do not match one-to-one.

## Skill problems found (by reading, not by a run)

1. **Phase 0 fallbacks never fire.** `git diff origin/develop...HEAD 2>/dev/null | tail -40 || git diff --stat HEAD`:
   the pipeline's exit status is `tail`'s, so `||` is never taken. Same for the `git log ... | head -30 || ...` line.
   When `origin/develop` is missing or unrelated, the grounding is silently empty and the
   skill's rule "if the diff is empty, say so and stop" would stop a real review.
2. **Shallow clones break it.** This cloud checkout is shallow. `git merge-base origin/develop <pr>`
   returned nothing until I ran `git fetch --deepen=400 origin develop`. Phase 0 has no
   step to detect or fix that, and (1) hides the failure.
3. **Base is hardcoded to `origin/develop`.** Fine for the normal case, but wrong for
   stacked branches (AGENTS.md says stacked PRs are the norm) and for a worktree whose
   `origin/develop` has moved: the diff then includes unrelated develop commits.
4. **Needs a root session.** `allowed-tools` lists `Agent`, and Phase 1 spawns up to ~10
   agents. Run from inside a subagent it cannot work and the skill has no fallback or
   message for that case.
5. **Linked issue / PR lookup has no concrete step.** Phase 0 says to find the PR and
   `Resolves #N` but gives no command (only `gh api` in allowed-tools). `intent-check` and
   `pr-narrative` depend on that input and the invocation contract does not say what to pass
   when no PR exists yet.
6. **"New `$t` / `msg` strings" trigger is vague.** The matrix does not say how to detect
   them (grep the diff for `$t\``? check `.po` changes?). Easy to skip or over-dispatch.
7. **Verdict emoji vs severity wording.** The report header example uses "blocking / important /
   nit / praise" counts, while Phase 5 maps verdicts to "blocking" and "important" only; nits
   never affect the verdict. Probably intended, but unstated.
8. Phase 5 test table commands exist (`task fw-lite:test-quick` in `backend/FwLite/Taskfile.yml`,
   `task api:generate-gql-schema` resolves via `backend/Taskfile.yml`). No staleness found there.
   Not checked: whether `--filter Sena3` still matches a test name.

## Bottom line

- The eval could not be completed here: subagents cannot spawn agents, so 0 of 16 planned reviewer runs happened.
- Two PRs are pinned and ready (#2514 backend, #2618 frontend) with worktrees, base SHAs and
  the matrix-derived agent list (8 each). Re-dispatch is about 16 `Agent` calls plus aggregation.
- Ground truth for both is thin: mostly naming and design, one concrete defect (multi-line
  record text) found by a bot. Recall numbers from these two will be low-signal; a third
  PR with real bug-class review would help.
- Two real skill defects are already confirmed by reading: the dead `||` fallbacks in Phase 0
  and no handling of shallow clones.
- Cleanup not done on purpose (worktrees needed for the retry). Remove with
  `git worktree remove --force <path>` for each, then `git update-ref -d refs/eval/<n>`.
