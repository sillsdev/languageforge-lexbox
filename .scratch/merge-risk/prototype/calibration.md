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

Pending agents (parity of ported ops, rollout order, test coverage). Checked by hand: hgresume keeps the 5-day reset-backup cleanup minimum and strips uploaded hgrc. CI fully green, including GHA integration tests. lexbox's `ResetCleanupAgeDays` setting is now unused (hgresume reads its own env var, default 31).

Tim's call:
