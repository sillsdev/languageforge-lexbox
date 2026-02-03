# EntryLoaderService

On-demand entry loading with batch caching for virtual scroll.

## Reset Flow

```mermaid
flowchart TD
    subgraph FilterReset["Filter Reset (search/sort/filter change)"]
        A[Filter change] --> B[scheduleFilterReset]
        B --> |cancels pending event reset| C[debounce 300ms]
        C --> D[executeReset quiet=false]
        D --> E[Fetch count]
        E --> F[Clear cache, update totalCount]
        F --> G[Set loading=false, bump generation]
    end

    subgraph EventQueue["Event Queuing"]
        H[Entry event during filter reset] --> I{filterResetInFlight?}
        I --> |yes| J[Set eventPendingAfterFilterReset flag]
        J --> K[Wait for filter reset]
        I --> |no| L[scheduleEventReset]
    end

    subgraph EventReset["Event Reset (entry update/delete)"]
        L --> M[debounce 600ms]
        M --> N[executeReset quiet=true]
        N --> O[Fetch count + viewed batches]
        O --> P[Swap cache atomically]
        P --> Q[Bump generation, no loading change]
    end

    G --> |check flag| R{eventPendingAfterFilterReset?}
    R --> |yes| L
    R --> |no| S[Done]
    K --> G
    Q --> S
```

## Key Behaviors

- **Filter reset** shows loading state and clears cache
- **Event reset** is "quiet" - no loading flash, preloads viewed batches
- Events during filter reset are queued and processed after
- Filter reset cancels any pending event reset (will be superseded)
- Generation counter invalidates stale async operations
