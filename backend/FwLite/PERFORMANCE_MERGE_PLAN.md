# FwLite Sync Performance — Merge Plan

This file is a **temporary** planning doc. It should be deleted once the changes
have all been split into their own PRs and merged.

The branch `sync-perf-wip` (and submodule branch `backend/harmony @ perf-all-changes`)
contains a pile of related performance changes that together drop a sync that
previously timed out (>30 min) to ~21s. They were developed as one experiment;
they need to be split, individually justified, and reviewed.

The companion docs `PERFORMANCE_NOTES.md` and `PERFORMANCE_BENCHMARK_RESULTS.md`
are the investigation log + numbers — those should be **deleted before merge**
(their own preamble says so), but they are useful while we're splitting things up.

---

## All performance improvements in the WIP branch

Every distinct change in the branch, with its risk level for merging:

- 🟢 **R7 — `Commits` compound index** *(EF migration)*
  Adds `IX_Commits_DateTime_Counter_Id` over `(DateTime DESC, Counter DESC, Id DESC)`.
  Speeds up the `CurrentSnapshots` CTE window function and `DeleteStaleSnapshots`
  (~10% on first-sync; 150ms→1ms on stale-snapshot purge). Pure additive.

- 🟢 **R5 — `DiffCollection.DiffPositions` fast-path**
  Returns `null` early when before/after lists have identical IDs in identical
  positions. O(n) compare; trivially safe. Tiny but harmless.

- 🟢 **R6 — `EntrySync.SyncComplexFormsAndComponents` two-phase pass**
  Iterate every entry's `Components` first (the authoritative side), then every
  entry's `ComplexForms`. Correctness fix for dedup ordering under bulk batches.
  Outside a batch the behaviour is identical (same set of changes get queued
  in a slightly different order, but no dedup happens).

- 🟢 **`AlwaysValidate` consistency fix in `DataModel.AddManyChanges`** *(harmony)*
  `AddManyChanges` was unconditionally calling `ValidateCommits`; `Add` only
  calls it when `AlwaysValidate == true`. Make them consistent. Default is
  `true`, so no behaviour change in production.

- 🟡 **R4 — `OrderPicker.BatchOrderScope` + batch-aware order picking**
  New optional `BatchOrderScope` parameter on `PickOrder`; lets the picker
  resolve `between` anchors against in-flight batched moves and produces an
  HWM-aware append order. With `batch=null` it's a no-op pass-through. Adds
  surface area but is opt-in.

- 🟡 **`CrdtMiniLcmApi.BeginBulkChangeBatch`** *(major new internal API)*
  Defers `IChange` objects into an in-memory list and flushes via one
  `DataModel.AddManyChanges` on dispose. Wraps every `Create*` / `Update*` /
  `Delete*` method in an `IsBatching` short-circuit. Rich set of sharp edges
  (deferred reads, metadata-must-be-outside-scope, etc.) — see the XML doc in
  the source. **Load-bearing structural change.**

- 🟡 **FTS interceptor `SuppressUpdates` flag + post-batch `RegenerateEntrySearchTable`**
  Coarse on/off switch. Set by `BulkChangeBatchScope` on enter; full rebuild on
  successful flush. Today the interceptor knows nothing about which changes
  were applied; the scope just trusts the rebuild. **This is the part the user
  wants to make smarter — see the FTS plan section below.**

- 🟡 **`CrdtFwdataProjectSyncService.SyncInternal` restructure**
  Wraps entry sync in a `BeginBulkChangeBatch` scope. Metadata syncs (WS, POS,
  Publication, SemDom, ComplexFormType) stay outside. Depends on the bulk-batch
  scope existing.

- 🔴 **H1 — Harmony `PreloadAllCurrentSnapshots`** *(submodule, the big win)*
  When a batch's total change count is `> 50`, preload every current snapshot
  into a `Dictionary<Guid, ObjectSnapshot>` once. `GetSnapshot` and
  `GetSnapshotsWhere` then read in-memory instead of running the
  `CurrentSnapshots` CTE per cascade step. **40–63× on delete- and cflink-heavy
  workloads.** Memory cost: ~50 MB on Sena-3, scales to a transient 300 MB–1 GB
  for very large mobile projects.

- 🟢 **Test scaffolding**
  - `SyncBenchmark.cs`, `SyncMutationBenchmark.cs` — per-profile sync benches.
  - `BulkChangeBatchTests.cs`, `BulkChangeBatchSharpEdgesTests.cs` — 26 tests
    pinning the bulk-batch contract.
  - `AddManyChangesTests.cs` (harmony) — 8 tests for the previously-uncovered
    `AddManyChanges` path.
  - `DataModelReferenceTests.cs` — small additions.

- ⚪ **`LcmDebugger/Program.cs`** — profiling harness wiring; not for merge.

---

## The painless ones (merge first, in any order)

These can ship as small individual PRs with very little reviewer worry. They're
not load-bearing for the headline win, but each is a self-contained improvement
or correctness fix that earns ~1–10% on its own and keeps the PR queue moving.

1. **R7 Commits index** — `backend/FwLite/LcmCrdt/Migrations/20260418043016_AddCommitsOrderIndex.cs`
   - Pure raw-SQL migration (`migrationBuilder.Sql(...)`).
   - One-liner Up/Down: `CREATE INDEX IF NOT EXISTS …` / `DROP INDEX IF EXISTS …`.
   - Zero code coupling. Reviewer just needs to confirm the column order matches
     the CTE's `ORDER BY` (which it does: `DateTime DESC, Counter DESC, Id DESC`).

2. **R5 DiffCollection fast-path** — [DiffCollection.cs](backend/FwLite/MiniLcm/SyncHelpers/DiffCollection.cs)
   - 14-line fast-path inside `DiffPositions`.
   - Existing diff tests cover correctness; this just skips work when there's nothing to diff.

3. **`AlwaysValidate` consistency fix** — [harmony/src/SIL.Harmony/DataModel.cs](backend/harmony/src/SIL.Harmony/DataModel.cs)
   - One-line change inside `AddManyChanges` to mirror `Add`'s gating.
   - Defaults preserve current behaviour; it's a correctness/consistency fix
     that only matters to tests setting `AlwaysValidate = false`.

4. **R6 `SyncComplexFormsAndComponents` two-phase split** — [EntrySync.cs](backend/FwLite/MiniLcm/SyncHelpers/EntrySync.cs)
   - Splits one combined diff call into two passes (Components, then ComplexForms).
   - The XML-doc explains *why*; the single-entry overload (`SyncComplexFormsAndComponents(beforeEntry, afterEntry, ...)`)
     is unchanged.
   - Worth running `Sena3SyncTests` once before merging just to confirm no regression.

5. **R4 OrderPicker `BatchOrderScope` (param-only, no callers)** — [OrderPicker.cs](backend/FwLite/LcmCrdt/OrderPicker.cs)
   - The `batch` parameter is optional and pass-through when null.
   - **Caveat:** `OrderPicker.PickOrder` itself can ship without callers using
     it. **Do not** ship `CrdtMiniLcmApi.PickBatchAwareOrder` and
     `_batchOrderScopes` until the full bulk-batch system lands — those
     references rely on `BeginBulkChangeBatch`'s scope. Keep `OrderPicker.cs`
     and the rest of `CrdtMiniLcmApi` separable.

6. **Benchmark scaffolding (test-only)** — `SyncBenchmark.cs`, `SyncMutationBenchmark.cs`
   - These are pure additions and useful even before the perf changes land —
     they let us guard against regressions on `develop` going forward.
   - One small risk: `SyncMutationBenchmark` mutates Sena-3 and may be slow on
     CI. Confirm they're not in the default test run, or marked `[Trait]`/skipped
     by default.

Suggested ordering for these: 1 → 2 → 3 → 4 → 5 → 6, but they're effectively
independent. Each PR should be ~10–50 lines of code change.

---

## What's left after the easy wins (not in scope here, just to anchor)

Everything below this line needs careful thought, design review, and probably
multiple PRs each:

- **H1 preload + the >50 gate** — needs a separate doc covering memory
  characterization, the mobile/MAUI risk, and probably a `CrdtConfig` opt-out.
- **`BeginBulkChangeBatch` + the deferred-write API** — large surface area;
  needs a careful contract review and probably a "v0 internal-only" landing
  path.
- **`CrdtFwdataProjectSyncService.SyncInternal` restructure** — depends on the
  bulk-batch scope landing first.
- **The smarter FTS interceptor** — see plan immediately below.

---

## The FTS interceptor: smart deferral plan

This is the user-requested design that we'd like to land **independently** of
the bulk-batch / preload work. It stands on its own: any sync (or any
explicit caller) can open an FTS scope and get correct, cheap, per-batch FTS
maintenance — even with no other batching in place.

### Goal

Replace the current binary `SuppressUpdates` flag with a scope that:

1. **Buffers** per-entry FTS updates while the scope is open.
2. **Detects "wide-impact" changes** as they flow past the interceptor and
   escalates the scope to a full-refresh-on-flush mode.
3. **Flushes once on dispose**:
   - if escalated → `RegenerateEntrySearchTable()`,
   - else → applies the buffered incremental updates in one batched call.

Crucially: **outside a scope, the interceptor behaves exactly as today** —
incremental per-`SaveChanges`. No global state, no surprise.

### Why this is independent of the rest of the perf work

- The bulk-batch system happens to also need this (and currently uses the dumb
  `SuppressUpdates` flag), but it's not the only consumer. Any sync orchestrator
  can open an FTS scope around its writes regardless of how those writes are
  structured underneath.
- The interceptor only sees `DbContext.ChangeTracker` entries on `SaveChanges`,
  so it's already orthogonal to whether `DataModel.AddChange` or
  `AddManyChanges` produced them.
- It does not need preload, batch-order scopes, or the deferred-change list to
  work. It only needs: "I'm in a scope, here's a SaveChanges, accumulate or
  escalate."

### Proposed shape

```csharp
public class UpdateEntrySearchTableInterceptor : ISaveChangesInterceptor
{
    private FtsScope? _activeScope;

    internal FtsScope BeginScope() { /* reject nesting; create scope */ }

    internal sealed class FtsScope : IAsyncDisposable
    {
        // Per-entry buffer (entries to upsert) and removal buffer.
        public HashSet<Guid> TouchedEntryIds { get; } = [];
        public HashSet<Guid> RemovedEntryIds { get; } = [];
        public List<WritingSystem> NewWritingSystems { get; } = [];

        // Escalation flag — once set, per-entry buffers are no-ops because
        // we're going to rebuild on dispose anyway.
        public bool NeedsFullRebuild { get; set; }

        // Optional: which kind of change escalated us, for logging/telemetry.
        public string? EscalationReason { get; set; }

        public async ValueTask DisposeAsync() { /* flush per below */ }
    }
}
```

The interceptor's `SavingChangesAsync` becomes:

```csharp
private async Task UpdateSearchTableOnSave(DbContext? dbContext)
{
    if (dbContext is null) return;

    if (_activeScope is { } scope)
    {
        if (scope.NeedsFullRebuild) return; // already escalated, ignore everything
        InspectAndAccumulate(dbContext, scope);
        return; // do NOT update the FTS table now — wait for scope dispose
    }

    // Default path (no scope): unchanged from today.
    await UpdateSearchTableOnSaveImmediate(dbContext);
}
```

### Detection rules — when to escalate to full rebuild

The interceptor watches `dbContext.ChangeTracker.Entries()`. Today it only
notices `Entry`, `Sense`, and added `WritingSystem`. We extend it to detect
**wide-impact** changes:

| Trigger | Escalates? | Why |
|---|---|---|
| `Entry` add/update/delete | No — buffer entryId | local; per-entry update suffices |
| `Sense` add/update/delete | No — buffer entry-of-sense | local; per-entry update suffices |
| `WritingSystem` add | No — pass through to update batch | already handled today; per-entry recompute uses the new WS |
| `WritingSystem` modify (e.g. order, code, type) | **Yes** | order affects every Headword's tie-break |
| `WritingSystem` delete | **Yes** | every entry's per-WS columns now stale |
| `MorphType` add/modify/delete | **Yes** | morph-type affixation chars shape every Headword |
| `PartOfSpeech`, `SemanticDomain`, `ComplexFormType`, `Publication` | No | not in the FTS record's text |

The "future PR that refreshes all entries of a certain morph-type" — the user's
example — would write a `MorphType` modification through EF. The interceptor
sees it during a `SaveChangesAsync`, escalates the active scope, and stops
accumulating per-entry buffers. On scope dispose: one `RegenerateEntrySearchTable`.
The PR that adds the MorphType refresh logic doesn't have to know anything
about the FTS scope — it just has to be invoked inside one (or call
`SaveChanges` while a scope is open, which the sync orchestrator handles).

### Where the scope is opened

Today: `BulkChangeBatchScope` flips `SuppressUpdates`.

After this change, callers do:

```csharp
await using var ftsScope = ftsInterceptor.BeginScope();
// ... existing code that calls SaveChanges many times ...
// Dispose flushes: rebuild OR batched incremental, whichever applies.
```

For the existing bulk-batch consumer (`BulkChangeBatchScope.DisposeAsync`), the
contract changes from "I do the regenerate myself" to "I open an FTS scope at
construction; the scope figures out what to flush." That's a small
simplification.

For the future morph-type-rename PR, the caller similarly just opens a scope
around its work. No need to know whether escalation will happen.

### Default = sync? No — default = no scope

We deliberately do **not** make every sync auto-open a scope. The caller
decides. This keeps the rule simple:

> **No scope → default per-`SaveChanges` behaviour. Inside a scope → buffer +
> escalate-on-detect + flush-on-dispose.**

This avoids surprising any unrelated FTS-touching code paths (e.g., a single
`UpdateEntry` call from the UI, which already runs the per-`SaveChanges` path
just fine).

### Flush logic on dispose

```csharp
public async ValueTask DisposeAsync()
{
    var scope = _interceptor._activeScope;
    _interceptor._activeScope = null;

    if (NeedsFullRebuild)
    {
        // Reason for not catching exceptions here: regenerate is itself
        // transactional; if it throws, the FTS table is left in its last good
        // state (the *previous* RegenerateEntrySearchTable's commit) and the
        // caller's surrounding transaction has already committed. Surface the
        // failure — the sync orchestrator can decide what to do.
        await _entrySearchService.RegenerateEntrySearchTable();
        return;
    }

    if (TouchedEntryIds.Count == 0 && RemovedEntryIds.Count == 0) return;

    // Batched incremental: load just the touched entries with senses, build
    // search records, upsert in one go. Reuses the existing
    // EntrySearchService.UpdateEntrySearchTable(IEnumerable<Entry>) overload.
    var entries = await dbContext.Set<Entry>()
        .Include(e => e.Senses)
        .Where(e => TouchedEntryIds.Contains(e.Id))
        .ToListAsync();
    await EntrySearchService.UpdateEntrySearchTable(
        entries, RemovedEntryIds, NewWritingSystems, dbContext);
}
```

### Error handling

- **Inside the scope, a `SaveChanges` throws:** the scope's buffers are dirty
  but the DB transaction rolls back. On dispose, we should recognize "we may
  have buffered work corresponding to rolled-back state" and conservatively
  treat it as `NeedsFullRebuild`. Easiest implementation: capture
  `dbContext.Database.CurrentTransaction` at scope start; if any
  `SavingChangesAsync` is followed by a non-success outcome (use
  `SavedChangesAsync` / `SaveChangesFailedAsync`), set `NeedsFullRebuild = true`.
- **Scope dispose runs without ever seeing a `SaveChanges`:** flush is a no-op.
- **Scope dispose runs after only escalating but before the escalating
  `SaveChanges` committed:** same treatment as above — assume we don't know,
  do a full rebuild. (Rebuild is correct, just expensive.)

### What to guard with tests

- `BeginScope` rejects nesting (matches today's `SuppressUpdates` invariant).
- Inside a scope, individual entry edits result in zero FTS writes until
  dispose.
- On dispose with only entry/sense changes, FTS reflects exactly those entries
  (no full rebuild ran).
- A `MorphType` (or `WritingSystem.Order`) modification inside the scope
  escalates → on dispose, `RegenerateEntrySearchTable` ran exactly once and
  per-entry updates were not redundantly issued.
- A `SaveChangesFailedAsync` callback inside the scope flips
  `NeedsFullRebuild`; subsequent successful saves still flush as a full
  rebuild.
- Outside a scope, behaviour matches today's interceptor exactly (regression
  guard for the UI path).

### Migration from the existing `SuppressUpdates` flag

- The `SuppressUpdates` flag and the `BulkChangeBatchScope.RegenerateEntrySearchTable`
  call go away.
- `BulkChangeBatchScope`'s ctor opens an FTS scope; its `DisposeAsync` disposes
  it before disposing itself. (Or simply takes ownership of the scope.)
- Consumers that *only* want FTS-scope behaviour (the morph-type PR) get the
  new `BeginScope` API and don't care about the bulk-change machinery.

This means the FTS scope can land **first**, before the bulk-batch / preload
work. The current `SuppressUpdates` API can stay during the transition (mark
`[Obsolete]` on it), and `BulkChangeBatchScope` keeps using the old flag until
the bulk-batch work is ready.

### Order to ship

1. Land the new `FtsScope` API + detection rules + dispose flush logic with
   tests, while leaving `SuppressUpdates` in place and unused except by the
   existing (still-WIP) bulk-batch scope. **This PR alone solves the
   morph-type-refresh use case.**
2. (Later, when the bulk-batch work merges) switch
   `BulkChangeBatchScope` to use `FtsScope` instead of `SuppressUpdates`.
   Delete the flag.

---

## Open questions / things to decide

- **Does `MorphType` actually live on EF as its own entity in LcmCrdt?**
  Quick check before finalizing the detection table — if MorphType is embedded
  in Entry as a value, we may need to detect it via JSON-patch inspection
  rather than ChangeTracker entity-state. (Today the interceptor only inspects
  entity state; JSON-patch contents aren't visible to it.) If so, the
  escalation predicate may need a different signal — e.g., a marker change
  type or a hint flag that the caller sets on the scope.

- **Is "modify a `WritingSystem.Order`" a real operation today?**
  If WS order is immutable in practice, we can drop it from the trigger list
  and only escalate on add/delete (which today is sometimes also a non-event
  for FTS but worth re-examining).

- **Should the morph-type-refresh PR set the scope's `NeedsFullRebuild` flag
  explicitly** rather than relying on auto-detection? An explicit
  `scope.MarkNeedsFullRebuild("morph-type rename")` API is dead simple, has
  no detection edge cases, and works for any future "global" change without
  the interceptor needing to know about it. Auto-detect handles the
  surprise case (someone modifies a MorphType outside the morph-type-rename
  flow); explicit handles the planned case. Both are easy and orthogonal —
  ship explicit first, add auto-detect as a safety net.
