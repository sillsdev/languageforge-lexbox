# ADR 0001: Handling unresolvable media references in CRDT ↔ FwData sync

Status: Accepted

## Context

CRDT ↔ FwData sync can encounter an audio writing-system value that references a media file it cannot resolve at write time. Three legitimate origins:

1. **Scenario 2 (the reported bug)** — a file created in FwLite whose reference synced and pushed but whose binary was never uploaded, so no lexbox `Files` row exists for a real Harmony id. Crashed on **both** create and update.
2. **Scenario 1, not-found sentinel** — a FwData audio file outside `LinkedFilesRootDir`. On the FwData→CRDT read it becomes the sentinel; crashed on the FwData **create** path only (`ShouldSet` guarded update).
3. **Scenario 1, rooted/absolute path** — a rooted FwData audio value crashed earlier still, on the FwData→CRDT **read**.

The crash path: `EntrySync` → `LcmHelpers.SetString` → `FwDataMiniLcmApi.FromMediaUri` → adapter returns `null` → `NotFoundException` → `SyncObjectException` → the whole sync job dies.

### Glossary

- **Unresolved media reference** — an entry field referencing a real media id whose binary isn't resolvable at write time (no `Files` row / not on disk).
- **Not-found sentinel** — the identity-free constant `MediaUri.NotFound` (`Guid.Empty`, authority `not-found`, `sil-media://not-found/00000000-…`). It carries no original path and is unrecoverable.

## Decision

**Never crash on an unresolvable out-of-tree or not-yet-uploaded audio reference.**

1. **Read (FwData→CRDT), rooted path — normalize-then-classify.** A rooted path resolving under `LinkedFilesRootDir/AudioVisual` is relativized and resolved normally; a genuinely out-of-tree rooted path becomes the not-found sentinel. No throw. (`FwDataMiniLcmApi.ToMediaUri`)

2. **Write (CRDT→FwData) — skip the field at the single choke point.** `FromMediaUri` returns `null` for the sentinel and for any unresolvable id; `SetString` then skips the field. Create and update behave identically. The rest of the entry syncs; the reference stays pending in the CRDT. (`FwDataMiniLcmApi.FromMediaUri`, `LcmHelpers.SetString`)

3. **Snapshot survival — the snapshot's audio must reflect FwData's actual content.** The `ProjectSnapshot` is regenerated from the CRDT (issue #1912), so it carries the skipped reference even though FwData does not. Before the FwData→CRDT diff, drop any audio writing-system value the snapshot has but FwData lacks (or holds as the sentinel). This makes the diff a no-op instead of a `remove` that would permanently delete the pending reference from the CRDT. The reverse direction reads the live CRDT, so it re-attempts the write and **heals automatically** once the binary becomes resolvable. Audio FwData genuinely holds is untouched, so a FieldWorks-side audio addition still syncs to the CRDT. (`AudioSnapshotReconciler`, `CrdtFwdataProjectSyncService.SyncInternal`)

4. **Reconcile must preserve pending resources (thread B).** `MediaFileService.SyncMediaFiles` deletes Harmony resources with no `Files` row. A never-uploaded resource (`Remote == false` / `RemoteId == null`) is a pending upload — deleting it emits a synced `DeleteRemoteResourceChange` that kills the client's automatic re-upload. Guard: `if (!lcmResource.Remote) continue;`. Genuine orphans (`Remote == true`, no `Files` row) are still deleted.

5. **No automatic heal for the sentinel.** The sentinel is identity-free and the out-of-tree file never reaches the server, so there is nothing to heal against. Re-attaching such audio is a manual user action. Server warns only.

## Considered and rejected

- **Write the anticipated media path** so FieldWorks self-heals — infeasible: the filename is unreachable at the FwData-write point (no `Files` row; the metadata-bearing Harmony resource is deleted by reconcile one step before the write; upload/read subfolder logic isn't unified, so a wrong path would never heal).
- **Suppress audio removes in the FwData→CRDT diff direction** (an alternative to #3) — would require threading a direction flag through the shared `EntrySync`/`MultiStringDiff`. The snapshot-reconcile placement achieves the same effect with lower blast radius and keeps snapshot regeneration "from CRDT" per #1912.
- **Auto-heal for scenario 1** by copying out-of-tree files into the tree at import, or by preserving the original path in `MediaUri` — both out of scope and either mutate project layout or change the `MediaUri` model/serialization.

## Consequences

- Sync never dies on an unresolvable audio reference; the entry syncs and the reference heals on a later sync once the binary uploads.
- A FieldWorks-side deletion of an audio value is not propagated to the CRDT (it is treated as a still-pending reference). This is a deliberate data-loss-safe bias consistent with the existing "update-path preservation" behavior.
