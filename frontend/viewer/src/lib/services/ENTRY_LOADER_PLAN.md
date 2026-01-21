# Entry Loader Service Plan

## Overview

Refactor `EntriesList.svelte` to use virtual scrolling with on-demand entry loading via the new `<Delayed>` component.

## Goals

- **Performance**: Fast initial render using count-only fetch
- **Memory efficiency**: Only cache entries in visible/nearby batches
- **Smooth UX**: Skeleton placeholders during load, instant display of cached entries

## Architecture

### V1 (Current Implementation)

1. **Fetch count** on mount / filter change
2. **VList renders indices**: `Array.from({length: count}, (_, i) => i)`
3. **Each item wrapped in `<Delayed>`**:
   - `getCached`: Returns entry from cache by index
   - `load`: Fetches batch containing index, caches all entries in batch
4. **Event handling** (simple):
   - `onEntryDeleted`: If in cache → remove + shift indices. Otherwise → full reset.
   - `onEntryUpdated`: If in cache → update. Otherwise → full reset.

### V2 (Future Enhancement)

✅ **Backend API complete**: `GetEntryIndex(entryId, query?, options?) → int` (returns -1 if not found)

Use `getEntryIndex` to enable:
- Jump-to-entry by ID (e.g., reload with `?entryId=X`, clearing filter with entry selected)
- Smart insert for new entries (know exact position)
- Quiet reset for events affecting entries not in cache

#### UI Reactivity for Cache Updates

**Problem**: `Delayed.svelte` watches function references `[load, getCached]`. When `EntryLoaderService` updates the cache, these references don't change, so the component doesn't re-render.

**Solution**: Version-based VList keys.

```typescript
// EntryLoaderService
#entryVersions = new SvelteMap<number, number>();

getVersion(index: number): number {
  return this.#entryVersions.get(index) ?? 0;
}

// In updateEntry():
this.#entryVersions.set(index, (this.#entryVersions.get(index) ?? 0) + 1);
```

```svelte
<!-- EntriesList.svelte -->
<VList
  data={indexArray}
  getKey={(index) => entryLoader.getEntryByIndex(index)?.id ?? `skeleton-${index}`}
>
  {#snippet children(index)}
    {#key entryLoader.getVersion(index)}
      <Delayed
        getCached={() => entryLoader.getEntryByIndex(index)}
        load={() => entryLoader.loadEntryByIndex(index)}
        delay={250}
      >
        <!-- entry row content -->
      </Delayed>
    {/key}
  {/snippet}
</VList>
```

**Two-layer reactivity:**
1. **`getKey` (entry ID)**: Ensures VList tracks items by identity, not position. On insert/delete, items keep their correct keys. Unloaded items use `skeleton-{index}` until loaded.
2. **`{#key}` (version)**: Forces re-render when an entry is updated in-place. When version increments, `{#key}` destroys and recreates `Delayed`, which re-queries `getCached()`.

> **Note**: Key changes from `skeleton-X` to entry ID on load. This is fine — the item was a skeleton anyway, and re-mounting a freshly loaded item is expected.

## Data Structures

```typescript
class EntryLoaderService {
  // Reactive state
  totalCount: number | undefined;
  loading: boolean;
  error: Error | undefined;

  // Cache (private)
  #entryCache: Map<number, IEntry>;      // index → entry
  #idToIndex: Map<string, number>;       // id → index (built incrementally)
  #pendingBatches: Map<number, Promise>; // batch# → pending fetch

  // Config
  readonly batchSize = 50;
}
```

## Batch Loading Logic

```
Given index N:
  batchNumber = floor(N / batchSize)
  offset = batchNumber * batchSize

  If batch not loaded and not pending:
    Start fetch for entries with { offset, count: batchSize }
    Store promise in pendingBatches
    On resolve: cache all returned entries at their indices

  Return entry from cache (or undefined if not yet loaded)
```

## Event Handling (V1)

| Event | In Cache? | Action |
|-------|-----------|--------|
| Delete | Yes | Remove from cache, shift subsequent indices, decrement count |
| Delete | No | Full reset (refetch count, clear cache) |
| Update | Yes | Update cache entry |
| Update | No | Full reset |

## Future: Event Handling (V2)

| Event | Condition | Action |
|-------|-----------|--------|
| Delete | In cache | Remove, shift indices, decrement count |
| Delete | Not in cache | Full reset (can't query deleted entry's position) |
| Update | In cache | Update cache, increment version |
| Update | Not in cache | Quiet reset (see below) |
| Add | In loaded batch | Insert at position, shift subsequent, increment count |
| Add | After all loaded | Increment count only (entry will load on scroll) |
| Add | Before loaded batch | Quiet reset |

### Quiet Reset

When an event affects entries outside the cache but we want to preserve UX:

1. Clear cache and loaded batch tracking
2. Determine "anchor" batch(es) — the last 1-2 batches that entries were pulled from
3. Immediately reload those batches (no loading indicators)
4. Maintain scroll position (center entry should remain visible)

This avoids jarring skeleton flashes for events the user can't see.

## Answers to Design Questions

### Should we prefetch adjacent batches for smoother scrolling?
No. VList has a `bufferSize` prop that preloads items beyond the visible area. This implicitly triggers `<Delayed>` components to load, which handles prefetching for us.

### How to handle `selectNextEntry()` when next entry isn't loaded yet?
Make `selectNextEntry()` async. It should:
1. Check if next entry is in cache → return immediately
2. Otherwise → call `loadEntryByIndex(nextIndex)` and await before returning

This mirrors the `<Delayed>` component's `getCached` / `load` pattern.

### Cache eviction policy for very long lists?
Not needed for V1. Memory is not a primary concern — performance is. We can revisit if this becomes an issue on low-memory devices.

## Migration Notes

### `EntriesList.svelte` Changes
- Remove: `loadingUndebounced`, `loading`, `entriesResource`, `fetchCurrentEntries`, `useDebounce`
- Add: `EntryLoaderService` instantiation
- Change: VList data from `entries` to index array
- Change: `selectNextEntry()` becomes async

### API Surface of `EntryLoaderService`

```typescript
// Construction
constructor(deps: {
  miniLcmApi: () => IMiniLcmApi | undefined;
  search: () => string;
  sort: () => SortConfig | undefined;
  gridifyFilter: () => string | undefined;
})

// Reactive state (read-only)
readonly totalCount: number | undefined;
readonly loading: boolean;
readonly error: Error | undefined;

// Core methods
getEntryByIndex(index: number): IEntry | undefined;       // sync, from cache
loadEntryByIndex(index: number): Promise<IEntry>;         // async, fetches batch if needed
loadCount(): Promise<void>;                               // fetches total count

// Event handling
getIndexById(id: string): number | undefined;             // from incremental id→index map
removeEntryById(id: string): void;                        // handles delete
updateEntry(entry: IEntry): void;                         // handles update
reset(): void;                                            // full reset

// V2 additions
getVersion(index: number): number;                        // for VList key invalidation
```

## V2 Testing Strategy

Tests live in `frontend/viewer/tests/entries-list-v2.test.ts`.

### Test Utilities

**Creating an entry at a specific index:**
```typescript
// Get entry at targetIndex-1, append '-inserted' to its headword
// This guarantees the new entry sorts immediately after (at targetIndex)
const entryBefore = await demoApi.getEntries({offset: targetIndex - 1, count: 1, order: ...});
const newHeadword = (entryBefore?.lexemeForm?.seh ?? '#') + '-inserted';
const entry = await demoApi.createEntry({id: crypto.randomUUID(), lexemeForm: {seh: newHeadword}, ...});
```

**Measuring item height (once per test):**
```typescript
async function getItemHeight(page: Page): Promise<number> {
  const {entryRows} = getLocators(page);
  await expect(entryRows.first()).toBeVisible();
  
  // Measure two consecutive items to get height + gap
  const firstBox = await entryRows.first().boundingBox();
  const secondBox = await entryRows.nth(1).boundingBox();
  if (!firstBox || !secondBox) throw new Error('Could not measure entry rows');
  
  return secondBox.y - firstBox.y; // Includes margin/gap
}
```

**Scrolling to an index:**
```typescript
async function scrollToIndex(page: Page, targetIndex: number, itemHeight: number): Promise<void> {
  const {vlist} = getLocators(page);
  const targetScroll = targetIndex * itemHeight;
  await vlist.evaluate((el, target) => { el.scrollTop = target; }, targetScroll);
  await page.waitForTimeout(300);
  await expect(page.locator('[data-skeleton]')).toHaveCount(0, {timeout: 5000});
}
```

> **Note**: Measure `itemHeight` once at test start, reuse for all scroll operations in that test.

**Finding center-visible entry:**
```typescript
// Get VList container bounds, find entry row whose box contains centerY
const containerBox = await vlist.boundingBox();
const centerY = containerBox.y + containerBox.height / 2;
// Iterate entry rows, find one where box.y <= centerY < box.y + box.height
```

### V2 Test Cases

| Test | Setup | Action | Verification |
|------|-------|--------|--------------|
| **Entry added in loaded batch** | View batch 0 | Create entry at index 25 | New entry at 25 contains `-inserted`; old 25 now at 26; old 49 pushed to 50; count +1 |
| **Entry added after loaded** | View batch 0 | Create entry at index 5000 | Count +1; visible unchanged; scroll to 5000 → entry visible |
| **Entry added before loaded** | Scroll to batch 2 | Create entry at index 25 | Center entry text unchanged after reset; count +1 |
| **Entry updated not in cache** | View batch 0 | Update entry at index 100 | No visible change; scroll to 100 → updated text visible |

### Batch Boundary Verification (Test 1)

When inserting at index 25 with batch size 50:
- Entry at old index 49 (last in batch 0) gets pushed to index 50 (first in batch 1)
- After insert, scroll to verify:
  - Index 49 shows what was previously at 48
  - Index 50 shows what was previously at 49
- This ensures no entries are lost or duplicated at batch boundaries
