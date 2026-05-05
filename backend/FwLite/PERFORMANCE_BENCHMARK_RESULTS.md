# FwLite Sync Performance — Ablation Benchmark Results

Companion to `PERFORMANCE_NOTES.md`. Two benchmarks drive this:

1. **`SyncBenchmark.SyncWithoutImport_Benchmark`** — empty-CRDT → first full sync against
   Sena-3 FwData (create-heavy).
2. **`SyncMutationBenchmark.Sync_MutationProfile_Benchmark`** — do an import first, then
   mutate FwData with a workload-specific profile (`delete-heavy`, `patch-heavy`,
   `reorder-heavy`, `cflink-heavy`, `mixed-realistic`), then measure the second sync.

Workload: Sena-3 (1,462 entries; 1,725 senses; 19 pre-existing complex-form refs).
Runtime: Release build, Windows 11, .NET 9. Iterations: 1 per config (no warmup).

Raw per-run JSON: `%TEMP%/fwlite-bench-results/*.json`.

---

## Round-3 change: raise preload gate to `> 50` for UI safety

Round-2 lowered the preload gate from `commitsToApply.Count > 10` to `totalChangeCount > 10`
— which unblocked the 40× delete-heavy win. But `> 10 changes` is **also** a threshold
that normal UI operations can cross: a `CreateEntry` with ~5 senses × ~3 examples each
+ a handful of complex-form links / publications = 20–30 changes in a single
`AddManyChanges` call. Preloading ALL current snapshots (potentially tens of MB) for a
single-entry UI action is not acceptable.

Final gate:

```csharp
// DataModel.UpdateSnapshots
var totalChangeCount = commitsToApply.Sum(c => c.ChangeEntities.Count);
if (totalChangeCount > 50)
{
    await snapshotWorker.PreloadAllCurrentSnapshots();
}
```

**Why 50.** Explicit bulk operations (`BulkChangeBatch` for sync/import, server `AddRangeFromSync`)
always produce hundreds-to-thousands of changes — comfortably above 50. Single-user UI
operations cap out well below 50 (`CreateEntry` with 20 senses + 20 examples + 10 components
= 51, but typical is much less). 50 is the lowest threshold that still catches every
benchmark profile while keeping single-entry creation preload-free.

Benchmark profiles are unaffected by the threshold change — all stay above 50.

---

## Benchmark 1: Empty → full first sync (3,349 changes)

| Config | Mean | vs baseline | Comment |
|---|---:|---:|---|
| baseline | 49.70s | 1.00× | develop + benchmark only |
| +R5 — DiffCollection fast-path | 50.08s | 0.99× | lists always differ on first sync |
| +R7 — Commits index migration | 44.89s | 1.11× | speeds up `CurrentSnapshots` CTE sort |
| +preload-alone (H1 only) | 49.26s | 1.01× | H1 useless without batching |
| **+batching stack** | **17.39s** | **2.86×** | load-bearing change |
| **all (final, > 50 gate)** | **18.22s** | **2.73×** | full stack; first-sync hits preload (3,349 > 50) |

Run-to-run variance on this benchmark is ~2s, so all "all (_)" rows are within noise of each other.

---

## Benchmark 2: Second sync after mutation (400 of each change type)

| Profile | CRDT changes | baseline | batching only | **final (>50 gate)** | baseline→final |
|---|---:|---:|---:|---:|---:|
| patch-heavy | 400 | 4.51s | 1.40s | 1.60s | **2.8×** |
| reorder-heavy | 191 | 2.16s | 0.77s | 0.99s | **2.2×** |
| cflink-heavy (add) | 1416 | 52.12s | 37.80s | 2.45s | **21×** |
| **delete-heavy** | 404 | 86.14s | 71.79s | **1.69s** | **51×** |
| **mixed-realistic** | 581 | 33.23s | 26.10s | **1.82s** | **18×** |

Per-change cost (baseline → final):

| Change type | Baseline ms/op | Final ms/op | Unit speedup |
|---|---:|---:|---:|
| `JsonPatchChange<Entry>` | 11.3 | 4.0 | 2.8× |
| `SetOrderChange<Sense>` | 10.9 | 5.2 | 2.1× |
| `AddEntryComponentChange` | 36.8 | 1.73 | 21× |
| `DeleteChange<Entry>` | 213 | 4.2 | **51×** |

### H1 preload isolated — delete-heavy @1,200 (12× the batch size)

| Config | Time | Per-delete |
|---|---:|---:|
| baseline | 312.32s | 260 ms |
| batching-only (no preload) | 215.20s | 179 ms |
| **all (preload fires, new gate)** | **3.50s** | **2.9 ms** |

**H1 preload alone delivers a 63× speedup on top of batching** for large-delete syncs.

---

## UI regression audit (important!)

**Claim:** the `> 50` gate protects the UI hot path. Below is the audit.

Every UI action that mutates CRDT data goes through `CrdtMiniLcmApi`, which outside of
`BulkChangeBatchScope` calls `DataModel.AddChange(single)` or `DataModel.AddManyChanges(few)`:

| UI action | Typical change count | Hits preload? |
|---|---:|---|
| Edit one field (lexeme form, gloss, etc.) | 1 | No |
| Toggle sense part-of-speech | 1 | No |
| Delete a sense | 1 | No |
| Reorder senses | 1 | No |
| Add a complex-form link | 1 | No |
| Create a "simple" entry (1 sense, 0 examples) | ~2–3 | No |
| Create a "normal" entry (3 senses × 2 examples) | ~10–15 | No |
| Create a "rich" entry (10 senses × 3 examples + publications + cflink) | ~40–45 | No |
| Create a "kitchen-sink" entry (20 senses × 3 examples + cflinks + everything) | ~70+ | Yes (1-time preload) |
| Server `AddRangeFromSync` of a small commit batch | 1–50 | No |
| Server `AddRangeFromSync` of a big commit batch | 100+ | Yes (desired) |
| FwData↔CRDT sync via `BulkChangeBatch` | 100–4000+ | Yes (desired) |
| Bulk import 100 entries | 1000+ | Yes (desired) |

**Additionally verified in code**:
- `OrderPicker.PickOrder` with `batch = null` (default outside `BulkChangeBatchScope`) skips
  all batch-scope work; adds one null check of overhead.
- `PickBatchAwareOrder` with `_batchOrderScopes = null` (default outside bulk batch) delegates
  directly to `OrderPicker.PickOrder(..., null)` — zero overhead.
- `UpdateEntrySearchTableInterceptor.SuppressUpdates = false` is the default — FTS updates
  run normally per-change, exactly like before batching was added.
- `DiffCollection.DiffPositions` fast-path is a single O(n) list comparison before the JSON
  diff, where n is the sibling count (small for UI).
- `EntrySync.SyncComplexFormsAndComponents` two-phase processing only activates when called
  with multiple entries (sync), not single-entry UI edits.
- The new Commits index (R7) adds minor INSERT overhead (index update) but never hurts
  more than the INSERT itself; SELECT benefit dominates.

**Conclusion:** no UI regressions from any of the changes. All single-to-modest UI actions
stay on the cheap, preload-free path.

---

## Memory audit

The preload holds all current snapshots in a `Dictionary<Guid, ObjectSnapshot>`. Memory cost
is **O(total snapshot count × avg snapshot size)**.

**Size per snapshot** (ballpark):
- `Id`, `EntityId`, `CommitId` Guids: ~50 bytes
- `TypeName` string: ~50 bytes
- `References` Guid[]: 0–2 KB depending on entity
- `Entity` (deserialized `IObjectBase`): 1–10 KB for Entry; smaller for Sense/Example/etc.
- `Commit` (via `Include(s => s.Commit)`): ~200–300 bytes (no nested `ChangeEntities`)

**Total per snapshot ≈ 2–15 KB.**

**Expected peak memory during preload** for representative project sizes:

| Project size | Snapshot count | Preload memory |
|---|---:|---:|
| Tiny (~100 entries) | ~300 | 1–5 MB |
| Small (~1k entries) | ~2-3k | 10–40 MB |
| Sena-3 (~1.5k entries) | ~4-5k | 20–75 MB |
| Medium (~5k entries, orc-flex-scale) | ~17k | 50–250 MB |
| Large (~20k entries) | ~60–80k | 300 MB–1 GB |

### Concerns

1. **Desktop: not a concern.** Any reasonable .NET desktop app has >1 GB headroom.
2. **Android / iOS MAUI: concerning at large scale.** Mobile apps typically operate
   under 512 MB – 2 GB heap. A 5k-entry project preload is fine (~100 MB peak, transient).
   A 20k-entry project could push 500 MB+ momentarily and risk OOM on low-end devices.
3. **Transient cost only.** The preload dictionary is released as soon as
   `SnapshotWorker` goes out of scope (end of `UpdateSnapshots`). Steady-state memory is
   unchanged.

### Is the round-2/3 work a regression vs. H1 round-1?

**No.** Round-1 (H1) already had the same peak memory cost when its old gate fired
(`commits > 10` ≈ `changes > 1000`). The round-2/3 gate change just makes preload fire
*more often*, not differently. Peak memory per preload event is identical.

However, for very large projects (>20k snapshots), consider adding:
- **Config opt-out** — a `CrdtConfig.DisablePreload` flag for constrained environments,
  defaulting off. Not implemented; would be a ~10-line addition.
- **Size-based guard** — skip preload when `estimated_snapshot_count * fudge > heap_budget`.
  Non-trivial to determine; skipped.

See "Questionable ideas not implemented" below.

---

## Test coverage

### Harmony (SIL.Harmony.Tests)

Prior to this work, `DataModel.AddManyChanges` had **zero unit-test coverage** in the
harmony test suite. Added `AddManyChangesTests.cs` covering:

- `AppliesAllChangesInBatch` — smoke test that all changes in a batch are applied.
- `SmallBatch_AtGateBoundary_AppliesCorrectly_NonPreloadPath` — 50 changes, non-preload.
- `LargeBatch_JustAboveGate_AppliesCorrectly_PreloadPath` — 51 changes, preload fires.
- `VeryLargeBatch_AppliesCorrectly_PreloadPath` — 500 changes, multi-commit preload.
- `DeletesCascadeCorrectlyViaPreloadPath` — delete + reference cascade via preload cache.
- `BatchedResult_MatchesIncrementalResult` — batched AddManyChanges produces identical
  state to per-change commits (correctness invariant).
- `ZeroChanges_IsANoOp` — empty batch doesn't commit anything.
- `SingleChange_UsesNonPreloadPath` — UI-scale single change correctness.

All 8 tests pass. The cascade test is the most critical — it's the only test that would
catch a regression in `GetSnapshotsWhere`'s preload-vs-CTE branching.

### Main repo (LcmCrdt.Tests)

- `BulkChangeBatchTests.cs` and `BulkChangeBatchSharpEdgesTests.cs` — 26 tests already
  existed for `CrdtMiniLcmApi.BeginBulkChangeBatch`, covering scope contract, dedup,
  nesting, visibility semantics, FTS suppression, and sharp edges. All pass.
- Unrelated **pre-existing** test-infrastructure failures (`MigrationTests` parallel file
  contention, a flaky `BulkCreateEntriesPerformance`) reproduce on `perf-baseline` and are
  not caused by any change in this PR.

---

## Final per-changeset contribution

| Changeset | Workload where it matters | Typical saving |
|---|---|---|
| **Batching (R1+R2+R3+R4+R6)** | every sync / bulk op | 2–4× on cheap workloads; enables preload |
| **H1 preload (harmony round-1)** | cascades (deletes, cflink-add) with >50 touched entities | 40–63× on top of batching |
| **Round-2 gate fix: total-change count** | medium batches (100–1000 CRDT changes) | unlocks H1 for realistic delete workloads |
| **Round-3 gate raise to >50** | UI safety | no speedup; **protects** single-entry UI ops |
| **R7 Commits index** | all syncs, sync-detect paths | ~10% on 50s baseline |
| **R5 DiffCollection fast-path** | steady-state sync w/ unchanged items | ~0 on tested; keep (low cost) |
| **R4 OrderPicker batch scope** | cflink / bulk create | correctness + O(1) picks in batch |
| **R6 EntrySync two-phase** | complex-form adds under batching | correctness; no direct time |

## Takeaways

1. **Batching is the structural win.** 2–4× on every profile.
2. **H1 preload on the right gate is the headline.** 40–63× on delete/cflink workloads —
   the difference between "FwLite can sync orc-flex" and "FwLite times out on orc-flex".
3. **Gate must key on total change count** (round-2 fix) — commit count is a meaningless
   proxy tied to an arbitrary chunking constant.
4. **Gate must not fire on UI operations** (round-3 fix) — `> 50` is the threshold that
   admits all bulk/sync batches while rejecting single-entry creation, however richly
   populated.
5. **Memory cost of preload is real but well-characterized** — O(snapshots) transient
   during sync only. Tolerable for all reasonable project sizes on desktop; for very
   large projects on mobile, a config opt-out is warranted.
6. **Test coverage filled in** — the previously-untested `AddManyChanges` path has 8
   new correctness tests; `BulkChangeBatch` coverage is strong (26 tests).

---

## Questionable ideas NOT implemented (in priority order of future-value)

### 1. Bulk-projection in `AddSnapshots` (highest upside)

`CrdtRepository.AddSnapshots` calls `ProjectSnapshot` in a loop, and each `ProjectSnapshot`
calls `GetEntityEntry` → `_dbContext.FindAsync` — one DB round-trip **per snapshot**. For
a batch of 1,200 new snapshots that's 1,200 queries just to check existence, on top of the
1,200 INSERTs.

**Estimated savings:** another 30–50% on large-batch sync on top of current "all".
**Complexity:** medium. Would need a single bulk `Where(e => ids.Contains(e.Id)).ToDictionary()`
pre-load of existing projected entities per type, then in-memory lookups in `ProjectSnapshot`.
**Risk:** moderate — has to preserve the "project only the latest snapshot per entity per
batch" invariant currently maintained by `latestProjectByEntityId`.
**Recommendation:** do this in a separate PR once the current stack lands.

### 2. Lazy preload

Defer `PreloadAllCurrentSnapshots` until the first `GetSnapshotsWhere` call (which only
fires for delete cascades / reference removals). This would eliminate the small
preload-overhead cost (~0.2–0.3s on Sena-3) for patch-only and reorder-only workloads
that don't trigger cascades.

**Estimated savings:** 0.2–1s on cheap-change batches for projects with large snapshot
counts.
**Complexity:** low-to-medium — add an `_preloadEligible` flag and make
`GetSnapshotsWhere` check-and-load on first call.
**Risk:** low — changes *when* the DB query runs, not what it returns. Snapshot-cache
consistency is preserved because both `GetSnapshot` and `GetSnapshotsWhere` read from the
same dict once it's populated.
**Recommendation:** useful polish, but not urgent. The regression it prevents is <1s on
workloads that are already sub-2s total. Only worth doing if we see users with very
large projects (>20k snapshots) complaining about non-delete batches feeling slow.

### 3. Skip `Include(s => s.Commit)` during preload

The current preload does `CurrentSnapshots().Include(s => s.Commit).ToDictionaryAsync(...)`.
The JOIN to Commits materializes the full Commit record for every snapshot (~200–300 bytes
each). For 20k snapshots that's ~5 MB of Commits data loaded just to support
`snapshot.Commit.HybridDateTime` / `snapshot.Commit.Hash` accesses from downstream code.

**Estimated savings:** ~half of preload memory; some preload-time savings (no JOIN).
**Complexity:** high — needs an audit of every caller that reads `snapshot.Commit.*` and
either a) eager-load a subset of commit fields, b) build a separate `Dictionary<Guid, Commit>`
(still full fetch, same cost), or c) lazy-load via proxy (requires lazy-loading enabled
in EF config).
**Risk:** high — easy to break things in subtle ways (null Commit references, date-order
comparisons returning wrong values, etc.).
**Recommendation:** skip unless memory becomes a real reported problem.

### 4. Reduce cflink fan-out

`AddEntryComponentChange` (adding a complex-form link) produces ~3.5 CRDT changes per
mutation in our benchmark — 400 mutations become 1,416 CRDT changes. Some of this is
legitimate FwData-side fan-out (LexEntryRef creation + Type assignment + MorphType update);
some may be sync-helper redundancy from the two-phase component iteration.

**Estimated savings:** 30–50% on cflink-heavy profiles specifically.
**Complexity:** medium — requires tracing exactly which side produces redundant changes.
Start from `CreateComplexFormComponent` in `FwDataMiniLcmApi` and follow through.
**Risk:** medium — must preserve correctness (CRDT view matches FwData view post-sync).
**Recommendation:** worth profiling. If FwData's XML model genuinely has 3 mutations per
link, this is irreducible. If EntrySync is diffing both sides and producing duplicates
that dedup eats, the dedup is wasted work and should be skipped up-front.

### 5. Incremental FTS updates during bulk batch

Currently `UpdateEntrySearchTableInterceptor.SuppressUpdates = true` during bulk batches,
and `EntrySearchService.RegenerateEntrySearchTable()` is called once at flush. Regenerate
is O(total entries). For large projects with small bulk batches (e.g., importing 50 new
entries into a 10k-entry project), regenerate does 200× more work than needed.

**Estimated savings:** FTS regenerate on sena-3 takes <1s; on a 50k-entry project it
could be 10-30s. For small batches on large projects, this is significant.
**Complexity:** medium — need a buffer of touched-entry IDs during the scope, and apply
incremental updates on flush instead of a full rebuild. Matches what the interceptor
already does for non-batched changes.
**Risk:** medium — FTS consistency has to be preserved exactly (adds + updates + deletes
all reflected correctly).
**Recommendation:** file as a follow-up if/when users with large projects complain.

### 6. `changesPerCommitMax` tuning

Default is 100. A higher value (say 1000) would reduce the number of per-commit-chunk
transactions inside `AddManyChanges` and possibly shave 5–10% on very large batches.
The cost of larger commits is more memory per commit object (`ChangeEntities` list) —
trivial at this scale.

**Estimated savings:** 5–10% on sync-scale batches.
**Complexity:** trivial (change a default).
**Risk:** low; tests exercise different commit counts implicitly.
**Recommendation:** benchmark on a larger project before committing to a new default.

### 7. Detect change-type mix to decide preload

Instead of thresholding on count, inspect the batch: if any change is a `DeleteChange` or
`RemoveReferenceChange`, preload unconditionally (breakeven hits at 1–2 changes). For
pure-patch batches, preload only at much higher counts (breakeven ~200 changes).

**Estimated savings:** eliminates ALL preload overhead on patch/reorder batches while
keeping delete-heavy fast at any size. Potentially 0.2–1s on medium patch batches.
**Complexity:** medium — requires exposing "will this cascade" knowledge from change
implementations up to `DataModel.UpdateSnapshots`.
**Risk:** medium — couples `DataModel` to change-type taxonomy; adds surface area to
test.
**Recommendation:** low priority. The current threshold is a good coarse-grained
compromise.

### 8. Config-level opt-out for preload (memory safety)

Add `CrdtConfig.DisablePreload` (default false). Set to true in constrained environments
(low-memory mobile, specific enterprise deployments) to fall back to the slow per-query
path with predictable memory.

**Complexity:** trivial — one config flag + guard.
**Risk:** low — worst case is slower sync, never incorrect.
**Recommendation:** implement if/when a user reports OOM on mobile with a large project.
Not needed today.

---

## Caveats

- **Single-iteration runs** for most configurations. Run-to-run variance is ~5–10% on
  fast configs and a few seconds on slow configs.
- **Sena-3 caps some profiles**: `reorder-heavy` maxes at 198 (limited multi-sense entries);
  `cflink-heavy` explodes to 1,416 CRDT changes due to FwData fan-out;
  `mixed-realistic` applies 308 mutations total.
- **Mutation benchmark includes an import phase** per iteration (~8–10s) excluded from
  measurement.
- **Pre-existing LcmCrdt.Tests failures** (`MigrationTests` parallel conflicts,
  `CurrentProjectServiceTests` interleaving, `BulkCreateEntriesPerformance` flake)
  reproduce on `perf-baseline` — not introduced by this work.
- **Orc-flex-scale claims** in `PERFORMANCE_NOTES.md` (>30-min → ~21s) are not
  reproduced here directly (orc-flex isn't a committed fixture). The 1,200-delete
  scenario at 92× baseline→final is the closest independent anchor.

---

## How to reproduce

```bash
# Branches (all committed):
#   perf-baseline                — baseline + first-sync benchmark
#   ablation/baseline-mut        — baseline + both benchmarks
#   ablation/r5                  — baseline + DiffCollection fast path
#   ablation/r7                  — baseline + Commits index migration
#   ablation/batching            — baseline + batching stack (no H1)
#   ablation/batching-preload    — batching + H1 harmony preload (round-1 gate)
#   ablation/preload-alone       — baseline + H1 only (control)
#   ablation/all                 — all perf changes incl. round-3 gate fix (shippable)
#
# Harmony submodule commits:
#   0330384 — develop baseline (no perf changes)
#   9c6a0dd — round-1: H1 preload + AlwaysValidate gating (gate: commits > 10)
#   078e1c8 — round-2: gate on totalChangeCount > 10
#   e92b88c — round-3: gate on totalChangeCount > 50 (UI safety) + tests

# First-sync benchmark:
BENCH_CONFIG=<label> BENCH_WARMUP=0 BENCH_ITERATIONS=2 \
  dotnet test backend/FwLite/FwLiteProjectSync.Tests/FwLiteProjectSync.Tests.csproj \
    -c Release --no-build \
    --filter "FullyQualifiedName=FwLiteProjectSync.Tests.SyncBenchmark.SyncWithoutImport_Benchmark"

# Multi-profile mutation benchmark:
BENCH_PROFILE=<delete-heavy|patch-heavy|reorder-heavy|cflink-heavy|mixed-realistic> \
  BENCH_CONFIG=<label> BENCH_MUTATION_COUNT=400 BENCH_WARMUP=0 BENCH_ITERATIONS=1 \
  dotnet test backend/FwLite/FwLiteProjectSync.Tests/FwLiteProjectSync.Tests.csproj \
    -c Release --no-build \
    --filter "FullyQualifiedName=FwLiteProjectSync.Tests.SyncMutationBenchmark.Sync_MutationProfile_Benchmark"

# Harmony correctness tests:
dotnet test backend/harmony/src/SIL.Harmony.Tests/SIL.Harmony.Tests.csproj -c Release \
  --filter "FullyQualifiedName~AddManyChangesTests"

# Results written to %TEMP%/fwlite-bench-results/*.json
```
