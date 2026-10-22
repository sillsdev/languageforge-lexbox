---
title: Harmony sync
sidebar_position: 2
---

FieldWorks Lite stores a project in [Harmony](https://github.com/sillsdev/harmony), SIL's CRDT library, backed by SQLite, not as a
document. Every edit becomes a *change* inside a *commit*; syncing is just exchanging commits with the server.

## How Harmony merges

- **Commits carry a hybrid logical clock** (`Commit.HybridDateTime`): a wall-clock timestamp plus a counter, so
  every client can put the same set of commits in the same total order without a central clock.
- **State is derived, not stored.** Applying commits in that order rebuilds per-entity snapshots, so two clients
  that hold the same commits end up with the same data regardless of the order they received them in.
- **Merging is per change, not per record.** Two clients editing different fields of one entry both keep their
  edit. Two clients editing the same field settle on the later commit in clock order (last writer wins, per
  field).
- **Changes must tolerate missing targets.** A change can reference an entity another client already deleted, so
  change classes check for that (`LcmCrdt/Changes/`).

Nothing is ever overwritten in the log: a sync only adds commits the other side is missing, in both directions
(`DataModel.SyncWith`).

## What triggers a client sync

| Trigger | Where | Notes |
|---|---|---|
| A local edit | `MiniLcmJsInvokable.OnDataChanged` | Every write queues a sync; the background worker waits ~100 ms before running it. |
| Opening a project | `ProjectServicesProvider` | Queues a sync; the push listener starts when that sync completes (see the Successful-sync row). |
| `OnProjectUpdated` push | `LexboxHubConnection` | The server pushes to everyone subscribed to the project after another client uploads commits. The pushing client's own id is passed along and that client ignores it. |
| Push listener (re)connect | `LexboxHubConnection.OnConnected` | A reconnect moves no data by itself (the server only pushes changes made after subscribing), so a connect transition triggers a catch-up sync for every tracked project. |
| Successful sync | `SyncService.ExecuteSync` | Best-effort restart of a listener that failed to start earlier (e.g. the user was offline at project open). |
| 5-minute recovery check | `PushListenerRecoveryService` | Cross-platform backstop that revives a listener the event-driven paths missed. Idempotent: a healthy connection short-circuits. |

Sync requests are queued on a channel and processed one at a time, ~100 ms apart
(`FwLiteShared/Sync/BackgroundSyncService.cs`). There is no deduplication: a burst of edits queues a sync each,
run back to back. The later ones just find nothing new to exchange.

## When a sync does nothing

`SafeExecuteSync` catches any exception and returns empty results. `ExecuteSync` itself only turns `HttpRequestException` and `OperationCanceledException` into `Offline`. Either way the local commits stay queued:

| Status | Cause |
|---|---|
| `NoServer` | The project has no origin domain (never uploaded to a server), or no server is configured for its domain. |
| `NotLoggedIn` | No account for that server. Actionable by the user, so it's reported ahead of `Offline`. |
| `Offline` | Token unrefreshable, connection dropped, or the server is unreachable. |
| `UnknownError` | The exchange ran but came back not-synced. |

None of this touches FieldWorks Classic (FLEx). Getting Harmony data into FwData is a separate, user-triggered job, see
[The FwHeadless merge](./fwheadless-merge.md).
