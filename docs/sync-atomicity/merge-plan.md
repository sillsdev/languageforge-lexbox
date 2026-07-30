# Merge plan

How to get [README.md](README.md) into `develop` in pieces a reviewer can actually
hold in their head. Eight PRs, each one buildable, testable and shippable on its own, in this order.
Nothing before PR 5 changes what a sync does.

The whole change is currently one branch. Splitting it is mechanical (the files barely overlap) but it
is work; the ordering below is chosen so each PR's diff stands alone without forward references.

| # | title | files | net lines | risk | reviewer needs to judge |
|---|---|---|---|---|---|
| 1 | Document the target sync architecture | docs only | +280 | none | is the invariant right? |
| 2 | Add a sqlite copy helper and let a project copy be handed over | `LcmCrdt` (2 files) | ~+50 | none | nothing behavioural |
| 3 | Record which CRDT commit a merge base was read from | `MiniLcm`, `FwLiteProjectSync`, 1 test | ~+80 | low | is the stamp read after the content? |
| 4 | Add a merge-base health check | `FwLiteProjectSync` + tests | ~+180 | none (unused) | is "sync-authored" the right signal? |
| 5 | Run the CRDT side of a sync against a staged copy | `FwLiteProjectSync` + tests | ~+400 | 🔴 high | the journal, and the file moves |
| 6 | Commit the CRDT database and merge base together in FwHeadless | `FwHeadless`, `Testing` | ~+250 | 🔴 high | phase order, scope lifetimes |
| 7 | Report a stale merge base at sync time | `FwHeadless`, `LexCore`, generated TS | ~+60 | low | Warn as the default |
| 8 | Document what a stale merge base does to real data | tests only | ~+170 | none | do the scenarios match the report? |

## 1. Document the target sync architecture

`docs/sync-atomicity/README.md` and the README link. Merge this first and separately: it is
the only place the reasoning lives, and every later PR gets to cite it instead of re-explaining
itself in comments. If the invariant is wrong, everything after it is wrong, and that argument is much
cheaper to have over a document than over a diff.

## 2. Add a sqlite copy helper and let a project copy be handed over

- `CrdtProjectsService`: pull the `BackupDatabase` dance out of `OpenTempProjectCopy` into
  `CopyProjectDatabase`, add `OpenProjectCopy(source, path)` for a copy at a caller-chosen path, and
  expose `DeleteDatabaseFile`.
- `TempCrdtProjectCopy`: gains `DbPath`, `Services`, and `CloseWithoutDeleting()`.

Pure refactor plus new unused surface. `OpenTempProjectCopy` keeps its behaviour, so the dry-run path
is unaffected. Reviewable in one sitting and it removes the only real coupling PR 5 would otherwise
have to introduce.

## 3. Record which CRDT commit a merge base was read from

- `ProjectSnapshot.Provenance` (`SnapshotProvenance(CrdtCommitId, TakenAt)`), with
  `[JsonIgnore(WhenWritingNull)]` so existing snapshot files round-trip byte for byte, which
  `ProjectSnapshotSerializationTests` proves.
- `CrdtHistoryHeadService` in `LcmCrdt`: head commit, find commit, commits after.
- `ProjectSnapshotService.TakeMergeBase` stamps it; `RegenerateProjectSnapshot` and
  `RegenerateProjectSnapshotAtCommit` go through it.
- `Sena3SyncTests.LiveSena3Sync` verifies the snapshot with provenance stripped, since a commit id
  and a timestamp can't be pinned.

Nothing reads the stamp yet. Two things worth a reviewer's attention: the head is read *after* the
contents (the other order could make a good base look stale), and `CrdtHistoryHeadService` resolves
its db context factory lazily because building one reads `CurrentProjectService.Project`, which isn't
set when the service is constructed.

## 4. Add a merge-base health check

`MergeBaseHealthService` plus `MergeBaseHealthServiceTests` (6 tests, ~2s, no fwdata). Nothing calls
it, so this PR cannot break a sync; it is purely "here is a question we can now answer". Splitting it
from PR 7 (which acts on the answer) keeps the "is the author name a good enough signal for
sync-authored?" discussion away from the "should we refuse to sync?" discussion.

## 5. Run the CRDT side of a sync against a staged copy

`SyncJournal`, `SyncStagingArea`, `SyncStagingService`, `SyncStagingTests` (9 tests, ~5s). Still not
wired into any caller, so `develop` behaves identically after merging it. This is the PR to read
slowly, and it is small enough to: three types, one of which is a two-state record.

What to look at:
- why both orders of the two file moves are unsafe, hence the journal (the argument is in the
  `SyncJournalState` doc comment);
- redo-from-the-top replay, and why skipping a move whose source is gone makes it idempotent;
- deleting the replaced database's sqlite sidecars before moving the new file over it;
- `ClearPool` plus a retry loop, because a pooled connection will block a move on Windows;
- the head-didn't-move check in `Commit`, which turns "nothing else writes to this database during a
  sync" from an assumption into a checked fact.

## 6. Commit the CRDT database and merge base together in FwHeadless

The behaviour change. `SyncWorker.ExecuteSync` gains a recovery step before it reads the merge base,
runs the sync against `staged.CrdtApi`, prepares the base from the staged database, and commits after
the fwdata push. Import splits out into `ExecuteImport` and deliberately keeps running against the
real database (no merge base yet means no second pass, and it keeps `MiniLcmImport` resumable). The
final Harmony push moves into `PushCrdtCommits`, in a fresh scope, because the database file was
replaced under the old one.

The test harness change is most of the diff and worth its own read: `SyncWorkerTestHarness` stops
mocking `ProjectSnapshotService` and the staging area and uses the real ones against a real sqlite
project, so the 19 existing tests now assert against what actually reaches the disk. Only the
CRDT↔fwdata sync stays mocked. Then `SyncWorkerInterruptionTests` adds 8 tests, of which these two are
the point of the whole change:

- a sync that throws after writing to the CRDT leaves the database and the merge base untouched;
- a sync whose fwdata push fails does the same.

Both fail on today's `develop`.

Expect two questions. First: the existing tests' expected step lists change, because a real merge
base file on disk makes `HasSyncedSuccessfully` true, so the pre-sync Harmony sync now runs in tests
as it always did in production. Second: a 268 MB database gets copied per sync. That is seconds
against a sync measured in hours, and the dry-run path has always done it.

## 7. Report a stale merge base at sync time

`FwHeadlessConfig.StaleMergeBaseAction` (`Warn` default, `Fail` available),
`SyncJobStatusEnum.StaleMergeBase` and its regenerated TS, and the check in `SyncWorker`. Three tests.

`Warn` is the default on purpose: we don't yet know how many projects have a stale base, and `Fail` on a
fleet-wide problem is a fleet-wide outage. Rollout is: ship it warning, read the logs, repair what turns up, then flip the default. The
repair itself is a judgement call, which is why nothing here tries to automate it.

## 8. Document what a stale merge base does to real data

`StaleMergeBaseDamageTests` (10 tests, ~3s, real fwdata via `SyncFixture`). Deliberately last: by now the
reader knows why a stale base can't happen any more, so these read as "and here is what we prevented"
rather than "here are some failing tests".

Three of them assert data loss on purpose (an entry deleted in FLEx comes back, a sense field cleared in
FLEx gets refilled, a renamed part of speech is reverted to the CRDT's old name). The others pin cases
where a stale base turns out to be harmless, so we notice if that ever changes. The table in
[README.md](README.md#what-a-stale-base-actually-does) is the summary.

## Sequencing notes

- **1–5 are safe to merge as fast as they get reviewed.** None of them changes a sync. If PR 6 turns
  out to need rework, nothing needs reverting.
- **6 is the one to deploy on its own** and watch. The log lines to grep are "Staging the CRDT side of
  the sync", "Recovered from an interrupted sync", and "Sync commit complete".
- **6 also wants a manual check** that no other FwHeadless code path writes to `crdt.sqlite` during a
  sync. `Commit` refuses if the head moved, so such a path turns into a failed sync rather than lost
  commits, but a failed sync is still a bad way to find out.
- 7 can merge before or after 6; it reads the stamp from PR 3, not the staging area from PR 5.
- If PRs 5 and 6 have to be one PR (they share no files, so they shouldn't), review 5's contents first
  regardless.

## What this plan does not cover

A separate data audit found that most of the destruction on the project that prompted this came from
chorusmerge resolving unmergeable FieldWorks files in FwHeadless's favour, and from FwHeadless committing
on a stale parent. Neither is touched by these eight PRs, and neither should be bolted onto them: they are
Mercurial-side problems with their own open questions. The ordering question is which goes first, and the
audit's answer is chorusmerge, because it is the one that has already destroyed a user's work. These PRs
are safe to merge in parallel with that work; they share no files.
