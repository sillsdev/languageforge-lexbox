---
title: The sync chain
sidebar_position: 1
---

Data gets from FieldWorks Lite to FieldWorks Classic (FLEx) and back through four separate hops, each with its own
trigger, transport and failure modes. Nothing is one big pipeline: hop 1 runs on its own, hops 2 and 4 each need a person to start them, hop 3 runs
only inside hop 2, and only hop 2 moves data between the Harmony world and the Mercurial world.

For the non-technical version, see the [user guide explainer](/fw-lite/how-sync-works).

```mermaid
flowchart LR
    subgraph USER[User machines]
        LITE[FieldWorks Lite<br/>Harmony SQLite]
        FLEX[FieldWorks Classic<br/>FwData XML]
    end

    subgraph LEXBOX[Lexbox server]
        HARMONY[(Harmony commits<br/>Postgres)]
        HG[(Mercurial repo<br/>hgweb)]
        API[Lexbox API<br/>+ SignalR hub]
    end

    subgraph HEADLESS[FwHeadless]
        WORKER[Sync worker<br/>Harmony copy + FwData copy]
    end

    LITE <-- "1. Harmony sync over HTTP<br/>+ SignalR push" --> API
    API --- HARMONY
    API -- "2. user presses Sync<br/>POST /api/merge/execute" --> WORKER
    WORKER <-- "2a. Harmony sync over HTTP<br/>(/api/crdt)" --> API
    WORKER <-- "3. Chorus Send/Receive" --> HG
    FLEX <-- "4. Send/Receive" --> HG

    style WORKER fill:#ff9,stroke:#333
```

## Hop 1: FieldWorks Lite and Lexbox (Harmony)

- **Trigger:** Every local edit, opening a project, a push from the server saying another client uploaded something, and a periodic recovery check. See [Harmony sync](./harmony.md).
- **Travels:** Harmony commits (each a list of changes) in both directions, plus pending media uploads/downloads.
- **Transport:** HTTP to `/api/crdt/{projectId}/…`; push notifications over the SignalR hub `/api/hub/crdt/project-changes`.
- **Code:** [client sync](https://github.com/sillsdev/languageforge-lexbox/blob/develop/backend/FwLite/FwLiteShared/Sync/SyncService.cs), [push listener](https://github.com/sillsdev/languageforge-lexbox/blob/develop/backend/FwLite/FwLiteShared/Projects/LexboxHubConnection.cs), [server endpoints](https://github.com/sillsdev/languageforge-lexbox/blob/develop/backend/LexBoxApi/Controllers/CrdtController.cs)

Failure modes: not logged in, or offline (server unreachable, or the login can't be refreshed). Both leave the
local edits safely queued for the next sync. A push listener that dies and never recovers is worse: edits still
sync out on the next local edit, but incoming changes stop arriving until something else triggers a sync.

## Hop 2: Lexbox to FwHeadless merge job

- **Trigger:** **A user, always.** FieldWorks Lite's Sync dialog, or "Sync FieldWorks Lite" on the Lexbox project page. No scheduler, no hg hook.
- **Travels:** Only the project id: `POST /api/fw-lite/sync/trigger/{projectId}` → FwHeadless `POST /api/merge/execute`, which queues the job.
- **Runs:** One global sequential worker; a second project waits its turn.
- **Code:** [trigger endpoint](https://github.com/sillsdev/languageforge-lexbox/blob/develop/backend/LexBoxApi/Controllers/SyncController.cs), [FwHeadless routes](https://github.com/sillsdev/languageforge-lexbox/blob/develop/backend/FwHeadless/Routes/MergeRoutes.cs), [job queue and worker](https://github.com/sillsdev/languageforge-lexbox/blob/develop/backend/FwHeadless/Services/SyncHostedService.cs)

Failure modes: 403 (FwHeadless cannot authenticate to Lexbox), 404 (project unknown), 423 (project blocked from
syncing). The client then polls for the result. Details in [The FwHeadless merge](./fwheadless-merge.md).

## Hop 3: FwHeadless and the Mercurial repo

- **Trigger:** Inside the merge job only: a Send/Receive before the merge if there are pending hg commits (a clone on the very first sync), and one after the merge if the merge wrote FwData changes.
- **Travels:** Mercurial changesets of the project's `.fwdata` file and friends. Chorus does the hg-side merging.
- **Code:** [Send/Receive](https://github.com/sillsdev/languageforge-lexbox/blob/develop/backend/FwHeadless/Services/SendReceiveService.cs)

Failure modes: Send/Receive fails (an HTTP 500 is retried once); Chorus detects a rollback, which blocks the
project from further syncing; the repo isn't a FieldWorks project (e.g. a WeSay project), so the clone has no
`.fwdata`; a stale hg lock hangs operations; and a FieldWorks data-model version that doesn't match FwHeadless's
risks corrupt data.

## Hop 4: Mercurial repo and FieldWorks Classic

- **Trigger:** The user's own Send/Receive in FieldWorks.
- **Travels:** Mercurial changesets; Chorus merges the FwData XML on the user's machine.

Failure mode that matters most: none of the FieldWorks Lite edits reach the FieldWorks user until they do a
Send/Receive, and their own edits only reach FieldWorks Lite once someone presses Sync (hop 2). The end-to-end
path is only as live as its slowest manual step.

## Which copy wins

FwHeadless keeps its own Harmony database *and* its own FwData working copy per project. The merge diffs both
against the snapshot (a file recording the state as of the last successful merge) and applies
**FwData → Harmony first**, so a field edited on both sides since the
last merge ends up with the FieldWorks Classic value. Within hop 1 (Lite clients only) there is no such
asymmetry: Harmony merges per change. See [The FwHeadless merge](./fwheadless-merge.md).
