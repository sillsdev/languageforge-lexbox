# FwLite / CRDT sync — pre-merge checklist

The single highest-data-loss surface in the repo. Read this in full before
filing findings on any `backend/FwLite/**` or `backend/FwHeadless/**` diff.

The authoritative AGENTS.md is `backend/FwLite/AGENTS.md`. This file is the
*review* checklist — concrete grep patterns, decision rules, and pointers to
the most common regressions.

## Invariants that must hold

### 1. Two-pass sync ordering

`FwLiteProjectSync/CrdtFwdataProjectSyncService.cs` must:

1. **Pass 1:** apply FwData → CRDT changes (FwData is read-only during this
   pass; CRDT receives them).
2. **Pass 2:** apply CRDT → FwData changes.
3. Save FwData explicitly (`fwdataApi.Save()`).
4. Regenerate `ProjectSnapshot` **from CRDT**, not FwData (issue #1912 —
   regenerating from FwData causes round-trip data loss when FwData has
   stale state).

Sync order within a pass matters: `WritingSystems → Publications →
PartsOfSpeech → SemanticDomains → ComplexFormTypes → Entries`. Reordering
without comment is a red flag.

Complex forms are two-phase: `SyncWithoutComplexFormsAndComponents` then
`SyncComplexFormsAndComponents`. Don't collapse into one.

### 2. No `DeletedAt is null` filters on projected DbSets

`LcmCrdtDbContext` exposes Harmony's projected snapshot tables. They already
contain only the latest, un-deleted state. Adding `.Where(x => x.DeletedAt
== null)` is dead code in production and hides real bugs when reviewers
later assume the filter is meaningful (PR #2286).

Where it's *legitimate* to look at tombstones: query change/commit tables
directly via `HistoryService.cs` or `SnapshotAtCommitService.cs`.

Grep: `\.DeletedAt\s*(==|is)\s*null` in any file under `LcmCrdt/` other than
those two services → blocking finding.

### 3. Validation lives in the Validation wrapper

Wrapper chain (read top-down): Client → Write Normalization → **Validation**
→ Read Normalization → Core API.

- Add new validation in `MiniLcm/Validation/` (or the existing validation
  wrapper).
- **Don't** add validation in the API class itself (`CrdtMiniLcmApi`,
  `FwDataMiniLcmApi`).
- **Don't** add validation in sync helpers (`MiniLcm/SyncHelpers/`) — the
  sync path doesn't re-validate, because existing FwData projects contain
  data that wouldn't pass current validation, and we still need to sync them.

If a finding suggests "add validation here" — verify it's in the wrapper
layer, not the API or sync layer.

### 4. New MiniLcm model field — full fanout

Adding a property to `Entry`, `Sense`, `ExampleSentence`, etc. touches all of
these. Missing any one = silent data loss.

| # | File / location | What |
|---|---|---|
| 1 | `MiniLcm/Models/<Type>.cs` | Add property |
| 2 | same | Update `Copy()` (deep copy with `[..coll]` or `.Select().ToArray()`) |
| 3 | same | Update `GetReferences()` if the field references other entities |
| 4 | same | Update `RemoveReference()` if it can hold a foreign key |
| 5 | `LcmCrdt/Objects/<Type>.cs` | Mirror property + entity config in `LcmCrdtDbContext` |
| 6 | `LcmCrdt/Changes/` | New Change class (prefer `JsonPatchChange<T>` for simple fields) |
| 7 | `LcmCrdt/LcmCrdtKernel.cs` → `ConfigureCrdt()` | Register the Change class |
| 8 | `LcmCrdt/CrdtMiniLcmApi.cs` | Read/write for new field |
| 9 | `FwDataMiniLcmBridge/Api/FwDataMiniLcmApi.cs` (~1700 lines) | Map to/from FieldWorks LCM (search for similar existing field) |
| 10 | `MiniLcm/SyncHelpers/<Type>Sync.cs` | Diff/sync the new field — **CRITICAL**, missing this = silent data loss |
| 11 | `MiniLcm.Tests/` base class | Test (will run against both implementations) |
| 12 | `FwLiteProjectSync.Tests/` | Sync round-trip test |
| 13 | `FwLiteProjectSync.Tests/Sena3SyncTests.cs` if it exercises a code path the field affects | May need a `Sena3` snapshot update — but **at most 1 new snapshot per PR** |

Grep: any new public property in `MiniLcm/Models/` → check items 2–10 are
all in the diff or already exist.

### 5. New Harmony Change class

For any new file under `LcmCrdt/Changes/`:

- Inherits `CreateChange<T>`, `EditChange<T>`, or `Change`.
- **Constructor parameter names match property names** (camelCase → PascalCase
  matching). JSON deserialization relies on this; an off-by-one rename breaks
  loading old commits.
- Has a private `[JsonConstructor]` overload if needed.
- Guards deleted references: `if (entity?.DeletedAt is not null) return;`.
- Registered in `LcmCrdtKernel.ConfigureCrdt()`.
- Has a test in `LcmCrdt.Tests/Changes/UseChangesTests.cs`.

### 6. SignalR / cache lifetime

Reconnect-handling code (`FwLiteShared/Sync/`, `FwLiteWeb/` hubs) has a
documented cluster of past bugs (PR #2174):

- Register event handlers **before** the first `SendAsync` (race window
  otherwise — a message arrives before the handler is attached).
- `+=` always paired with `-=` on cached connections, or duplicate handlers
  accumulate over reconnects.
- Resolve dependencies at invocation time, not captured in closures, when
  the underlying object can be disposed/recreated.
- Per-project cache keys, not per-server. Otherwise project switches reuse
  the wrong cached state.
- Reconnect handlers wrapped in per-iteration `try/catch` so one failure
  doesn't skip remaining handlers.

Grep for these patterns when reviewing SignalR-touching code:
- `connection.On<` followed by `connection.SendAsync` — handler must come
  first.
- `+=` not paired with `-=` in the same class.
- `static` caches keyed by anything coarser than project ID.

### 7. EF Core migrations

- `migrationBuilder.CreateTable()` can't express `ON CONFLICT IGNORE`. If
  you need it, hand-write the CREATE TABLE via `migrationBuilder.Sql()` and
  pair with a parallel `CreateIndex` so the model snapshot stays consistent
  (PR #2192).
- Use named GUIDs (constants) for predefined-data seed commits so they're
  stable across re-runs (PR #2278).
- Reversibility: `Down()` should actually undo `Up()`, not be a stub.

## Test commands (safe, fast)

| When | Command | Time |
|---|---|---|
| Touched sync helpers or `CrdtFwdataProjectSyncService.cs` | `dotnet test backend/FwLite/FwLiteProjectSync.Tests --filter Sena3` | ~2 min |
| Touched any FwLite code, smoke | `task fw-lite:test-quick` | ~45 s |
| Pre-PR thorough | `dotnet test FwLiteOnly.slnf --filter "Category!=Slow&Category!=Integration"` | ~3 min |
| Snapshot regen needed (verified intentional change) | `task fw-lite:reverify` then inspect `*.verified.txt` diff | ~3 min |

`dotnet test FwLiteOnly.slnf` (no filter, all tests) is ~10 min — only for
final pre-merge confidence pass.

## Known landmines (cite these in findings when relevant)

- **Auto-download stops** — known P1 (issue lb-8mg). Don't introduce
  long-lived connection state without considering reconnect.
- **Legacy snapshots missing morph-type support** — patched in
  `CrdtFwdataProjectSyncService` lines ~83–90. Don't remove that patch.
- **Race condition opening entry in FW** — fixed in PR #2265. Adding new
  entry-open paths needs the same thread guard.
- **Losing senses that are moved** — fixed in PR #2252. Sync code that
  iterates senses must use the post-move IDs, not captured ones.
- **CRDT sync on UI thread** — moved off in PR #2248. Don't re-block on
  sync from UI code.

