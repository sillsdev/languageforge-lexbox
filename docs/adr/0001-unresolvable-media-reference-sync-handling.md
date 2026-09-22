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

**Fix the root cause at the media layer: make a not-yet-uploaded reference RESOLVE instead of compensating in the sync engine.**

1. **Read (FwData→CRDT), rooted path — normalize-then-classify.** A rooted path resolving under `LinkedFilesRootDir/AudioVisual` is relativized and resolved normally; a genuinely out-of-tree rooted path becomes the not-found sentinel. No throw. (`FwDataMiniLcmApi.ToMediaUri`)

2. **Reconcile reserves a pending `Files` row for a not-yet-uploaded resource.** The harmony reconcile (`MediaFileService.SyncMediaFiles(projectId, ...)`) meets each Harmony resource that has no `Files` row. A never-uploaded resource (`Remote == false` / `RemoteId == null`) is a pending upload. When it carries usable metadata (a non-empty filename) the reconcile CREATES a pending `Files` row (`Revision == 0`) whose `Filename` is the **anticipated path** — the exact path an eventual upload will use: `LinkedFiles/{subfolder}/{fileId}/{filename}`, subfolder guessed from the mime type, reusing the upload endpoint's own convention. This makes the media reference resolve. A resource with no usable metadata is left untouched (nothing to reserve a path from).

3. **Write (CRDT→FwData) — record the anticipated path; FieldWorks tolerates the dangling link.** With the pending row present, `FromMediaUri` resolves the id (`PathFromMediaUri` needs only a `Files` row, not a file on disk) and `SetString` writes the anticipated relative path into FwData. FieldWorks tolerates a link whose file isn't there yet. When the binary is later uploaded to that same reserved path the link self-heals — no sync-layer special-casing. A bare unresolved reference with **no** Harmony resource (nothing to reserve a row from), and the not-found sentinel, still resolve to `null` and are **skipped** on write by the retained guard rather than crashing. (`FwDataMiniLcmApi.FromMediaUri`, `LcmHelpers.SetString`)

4. **Read-side adapter resolves by `Files` row, not by file existence.** `LexboxFwDataMediaAdapter.MediaUriFromPath` looks the path up via a non-throwing `Files`-row lookup and drops the old `File.Exists` gate: a pending row's anticipated path (no file on disk yet) reads back to the **same** `MediaUri` the CRDT holds, so the FwData→CRDT diff is a no-op and the pending reference is never reverted. A path with neither a row nor a file still reads as the not-found sentinel, exactly as the no-row case did before.

5. **Upload finalizes a pending row.** An upload to a pending row keeps the row's reserved `Filename`, writes the binary there, and advances `Revision` (0 → 1 on the first upload, and again on each later replacement) — the row becomes a normal, backed file. A pending (revision 0) row is exempt from the hg-reconcile deletion (`SyncMediaFiles(LcmCache)`) that removes `Files` rows lacking a physical file, since it legitimately has no file yet. Genuine orphans (`Remote == true`, no `Files` row) are still deleted.

6. **No automatic heal for the sentinel.** The sentinel is identity-free and the out-of-tree file never reaches the server, so there is nothing to heal against. Re-attaching such audio is a manual user action. Server warns only.

## Considered and rejected

- **Neutralize the skipped reference in the sync layer (a snapshot overlay / `AudioSnapshotReconciler`).** The earlier approach skipped the field on write and then, before the FwData→CRDT diff, dropped audio the CRDT snapshot held but FwData lacked, to stop the diff reverting the pending reference. It works but keeps compensating logic inside the shared sync engine and — critically — **cannot distinguish a skipped-pending reference from a genuine FieldWorks-side deletion**, so it suppresses (and its reverse direction even resurrects) real user deletions. Option D removes it: because FwData now holds a row-resolvable anticipated path, snapshot and FwData agree and there is nothing to reconcile.
- **A FwData placeholder file** written to the anticipated path so FieldWorks always has a real file — rejected: it mutates project layout with dummy bytes that would then sync through hg, and the placeholder has to be reliably distinguished from a real upload.
- **Auto-heal for scenario 1** by copying out-of-tree files into the tree at import, or by preserving the original path in `MediaUri` — both out of scope and either mutate project layout or change the `MediaUri` model/serialization.

## Consequences

- Sync never dies on an unresolvable audio reference; a pending-with-metadata reference resolves and writes its anticipated path immediately, and self-heals when the binary uploads. A reference with no resource (or the sentinel) is skipped and heals on a later sync once resolvable.
- The compensating sync-layer machinery is gone, so a genuine FieldWorks-side deletion of an audio value now propagates to the CRDT as an ordinary diff (it is no longer mistaken for a pending reference and suppressed).
- Net complexity moves OUT of the shared sync engine (`CrdtFwdataProjectSyncService` / `EntrySync`) and INTO the media layer (`MediaFileService` reconcile + `LexboxFwDataMediaAdapter`), where the resource metadata needed to reserve the path actually lives. A schema change (`MediaFile.Revision` + migration) is the cost; the revision count also lays groundwork for handling file replacements (revision 2+) later.
