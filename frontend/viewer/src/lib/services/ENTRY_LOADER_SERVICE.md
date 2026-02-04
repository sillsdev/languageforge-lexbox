# EntryLoaderService

On-demand entry loading with batch caching for virtual scroll.

## Reset Flow

```mermaid
flowchart TD
    A[Filter change] --> B[scheduleFilterReset]
    B --> |cancels pending event reset| C[debounce]
    C --> D[executeReset quiet=false]
    D --> E[Fetch count]
    E --> F[Clear cache + update totalCount]
    F --> G[If success, bump generation]

    H[Entry update/delete] --> I{filterResetInFlight?}
    I --> |yes| J[Set eventPendingAfterFilterReset]
    I --> |no| K[scheduleEventReset]
    K --> L[debounce 600ms]
    L --> M[executeReset quiet=true]
    M --> N[Fetch count + viewed batches]
    N --> O[Clear cache + preload viewed batches]
    O --> P[If success, bump generation]

    G --> Q{eventPendingAfterFilterReset?}
    Q --> |yes| K
    Q --> |no| R[Done]
    P --> R
```

## Key Behaviors

- **Filter reset** shows loading state and clears cache
- **Event reset** is "quiet" - no loading flash, preloads viewed batches
- Events during filter reset are queued and processed after
- Filter reset cancels any pending event reset (will be superseded)
- Generation counter invalidates stale async operations
