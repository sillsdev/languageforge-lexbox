---
title: Harmony sync
sidebar_position: 2
---

FieldWorks Lite stores a project in [Harmony](https://github.com/sillsdev/harmony), SIL's CRDT library, backed by SQLite, not as a
document. Every edit becomes a *change* inside a *commit*; syncing is just exchanging commits with the server.

## How Harmony merges

- **Commits carry a hybrid logical clock**: a wall-clock timestamp plus a counter, so every client can put the
  same set of commits in the same total order without a central clock.
- **State is derived, not stored.** Applying commits in that order rebuilds per-entity snapshots, so two clients
  that hold the same commits end up with the same data regardless of the order they received them in.
- **Merging is per change, not per record.** Two clients editing different fields of one entry both keep their
  edit. Two clients editing the same field settle on the later commit in clock order (last writer wins, per
  field).
- **Changes must tolerate missing targets.** A change can reference an entity another client already deleted, so
  each [change type](https://github.com/sillsdev/languageforge-lexbox/tree/develop/backend/FwLite/LcmCrdt/Changes)
  checks for that.

Nothing is ever overwritten in the log: a sync only adds commits the other side is missing, in both directions.

## What triggers a client sync

| Trigger | Notes |
|---|---|
| A local edit | Every write queues a sync, which runs about 100 ms later. |
| Opening a project | Queues a sync; the push listener starts when that sync completes. |
| A push from the server | After a client uploads commits, the server tells everyone else subscribed to the project. The uploading client ignores its own push. |
| Push listener (re)connects | A reconnect moves no data by itself (the server only pushes changes made after subscribing), so it triggers a catch-up sync for every open project. |
| A successful sync | Restarts a push listener that failed to start earlier, e.g. because the user was offline when they opened the project. |
| Every 5 minutes | A backstop that revives a push listener the other paths missed. Does nothing if the connection is healthy. |

Syncs run [one at a time](https://github.com/sillsdev/languageforge-lexbox/blob/develop/backend/FwLite/FwLiteShared/Sync/BackgroundSyncService.cs).
There is no deduplication: a burst of edits queues a sync each, run back to back. The later ones just find
nothing new to exchange.

## When a sync does nothing

A sync that can't run never throws away local work; the commits stay queued for the next one. It reports one of:

- **No server:** the project was never uploaded, or no server is configured for it.
- **Not logged in:** no account for that server. The user can fix this, so it's reported ahead of offline.
- **Offline:** the server is unreachable, the connection dropped, or the login can't be refreshed.
- **Unknown error:** the exchange ran but didn't complete.

None of this touches FieldWorks Classic (FLEx). Getting Harmony data into FwData is a separate, user-triggered job, see
[The FwHeadless merge](./fwheadless-merge.md).
