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

Add `getEntryIndex(entryId, queryOptions)` API endpoint to enable:
- Jump-to-entry by ID
- Smart insert for new entries (know exact position)
- Avoid full reset on events for entries not in cache

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

| Event | Action |
|-------|--------|
| Delete in cache | Remove, shift indices, decrement count |
| Delete not in cache | Full reset (can't query deleted entry's position) |
| Add/Update | Call `getEntryIndex()` → insert at position or ignore if outside loaded batches |

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
```
