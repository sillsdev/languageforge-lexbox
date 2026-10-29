# Calibration runs (prototype, by hand)

Skill draft: [SKILL.md](SKILL.md). Tim's call goes in the last line of each run.

## #2514 at its pre-review commit (backend/FwLite)

**Verdict: Do X, then merge.** X = make `MiniLcmApiValidationWrapper` validate `SubmitCreateSense` / `SubmitCreateExampleSentence` (explicit overrides, like its existing `SubmitUpdatePublication`), with one test that a sync create of an invalid sense throws.

| Change | What could go wrong | Likelihood | Repair cost | Covered by |
|---|---|---|---|---|
| Submit* defaults removed; BeaKona wrapper now forwards Submit* unvalidated | sync writes invalid senses/examples into CRDT and FwData | likely (every synced create) | high: CRDT history is permanent | nothing |
| normalization wrapper's Submit* now delete-wins | a UI edit to something deleted concurrently is silently dropped | rare | low (user redoes it) | nothing; fine |

Not scary, one line: the notify wrapper may skip notifying a linked complex-form entry (stale view until reload; a release fixes it).

Reality: merged without X; the gap was fixed in follow-up PR #2519.

Tim's call:

## #2854 (integration-test upload helper)

**Verdict: Do X, then merge.** X = get CI green: `Build API / publish-api` and `C# Unit Tests` are red on the current head. Cause not checked in this run (the real skill must check whether develop is red too).

No scary changes: test-only code plus removing an unused test package. Nothing ships to users.

Tim's call:

## #2821 (hg repo ops move to hgresume's manage API)

**Verdict: Do X, then merge.** X = create a non-empty `hgresume` secret in the develop, staging and production namespaces before this deploys (no overlay creates it; without it the rollout stalls). Optional follow-up in hgresume: a cleanup test that a recent reset backup, a non-reset deleted project and a dry run all survive.

| Change | What could go wrong | Likelihood | Repair cost | Covered by |
|---|---|---|---|---|
| reset-backup cleanup moved to hgresume | deletes backups it shouldn't | low: same regex, same 5-day floor, line-for-line port | high: permanent delete of user data | only "old reset folder is deleted"; nothing checks what must survive |
| ResetRepo / FinishReset moved | old repo lost during reset | low: same order, no cancel point between delete and move | high | FinishReset bad-zip tests; nothing checks the reset backup exists |
| repo ops need a new secret | rollout stalls on develop/staging/prod | medium | low (create secret or roll back) | n/a |

Not scary, one line: soft-deleting a project whose repo folder is missing now fails instead of logging (map 404 to `DirectoryNotFoundException`); lexbox's `ResetCleanupAgeDays` setting is now ignored.

Checked by hand: hgresume keeps the 5-day reset-backup cleanup minimum and strips uploaded hgrc. CI fully green, including GHA integration tests. lexbox's `ResetCleanupAgeDays` setting is now unused (hgresume reads its own env var, default 31).

Agent notes:
- deployment-infra: no data-loss path. Rollback safe; no repo writes left in lexbox. Two low-repair-cost items: the `hgresume` secret isn't created by any develop/staging/production overlay (rollout stalls until someone creates it), and lexbox-before-hgresume rollout briefly fails repo operations.
- test-auditor: two irreversible ops have no test that would fail: reset-backup cleanup deleting the wrong folders (only "old reset folder is deleted" is tested; nothing checks recent resets, non-reset deleted projects, the 5-day minimum, or dryRun survive) and ResetRepo keeping the old repo as a backup (no hgresume test). Both ports look line-for-line. FinishReset bad zip, CopyRepo non-empty guard and backup round-trip are covered. Both missing tests belong in hgresume, not this PR.
- ripple-detector: faithful port of every data-handling op; no item meets the bar.

Tim's call:
