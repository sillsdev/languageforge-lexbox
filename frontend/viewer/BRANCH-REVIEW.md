# Virtual Scrolling Branch Review (feature/virtual-scrolling-entries)

## Code Quality ✅
- **svelte-check**: 0 errors, 0 warnings
- **ESLint**: 0 errors
- **Build**: Successful

## Findings

### 1. Unused API Method: `GetEntryIndex`
**Status**: Not currently used by frontend  
**Location**: 
- Backend: `backend/FwLite/LcmCrdt/Data/MiniLcmRepository.cs:169`
- Frontend demo: `frontend/viewer/src/project/demo/in-memory-demo-api.ts`
- Generated types: `frontend/viewer/src/lib/dotnet-types/generated-types/FwLiteShared/Services/IMiniLcmJsInvokable.ts`

**Implementation**:
- Filters and sorts all entries, then scans sequentially to find index
- O(n) time complexity
- Never called by EntriesList component

**Recommendation**: Safe to remove. The functionality is integrated into `GetEntriesWindow` via `GetEntryIndexInternal`.

**Issue**: lb-74v

### 2. Query Efficiency: Index Lookup Scanning
**Current flow for `GetEntriesWindow(targetEntryId)`**:
1. Query 1: Count all entries matching filter
2. Query 2: Scan all entry IDs to find target's global index (O(n))
3. Query 3: Fetch centered window of entries

**Problem**: Step 2 scans entire result set to find one index

**Optimization opportunity**: Use SQL `ROW_NUMBER()` OVER clause to calculate index in single query:
```sql
WITH ranked AS (
  SELECT id, ROW_NUMBER() OVER (ORDER BY headword) AS row_num
  FROM entries WHERE filter_applied
)
SELECT row_num FROM ranked WHERE id = @targetId
```

**Impact**: Reduces query count from 3 to 2, eliminates O(n) client-side scan  
**Complexity**: Medium (requires Linq2Db/EF Core integration)  
**Issue**: lb-aii

### 3. Frontend Code Quality
**Strengths**:
- Clean separation: `userSelectedEntryId` vs `selectedEntryId` prop prevents race conditions
- Proper reactive state management with Svelte 5 `$state` and `$effect`
- QueryParamState correctly integrates with EntryView resource reactivity
- Virtual scrolling implementation is stable

**Potential simplifications**:
- None identified - code is clear and minimal

### 4. Architecture Review
**Component Flow**: 
```
EntriesList.svelte
  ├─ onSelectEntry() → updates BrowseView
  ├─ userSelectedEntryId (local scroll control)
  ├─ scroll effect → checks if entry visible before scrolling
  └─ infinite scroll → loadEntriesBefore/loadEntriesAfter

BrowseView.svelte
  ├─ selectedEntryId (QueryParamState - URL synced)
  └─ EntryView receives entryId prop
        └─ resource watches entryId → refetches entry
```

**Sound design** - proper separation of concerns between scroll state and selection state.

## Summary
- ✅ All quality checks pass
- ✅ No major simplification opportunities
- 📋 Two optimization issues created for future work (low priority)
- ✅ Branch ready for production
