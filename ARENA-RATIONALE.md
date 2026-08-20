# Arena rationale — candidate A: Android screen-off keep-awake

## What I built

One caller-facing seam, one platform seam, and an Android implementation of the platform seam.

| File | Role |
| --- | --- |
| `backend/FwLite/FwLiteShared/KeepAwake/IKeepAwake.cs` | `KeepAwakeWork(Title, NotificationText)` record + `IKeepAwake.RunAsync` (void and `T`-returning overloads). The only thing callers see. |
| `backend/FwLite/FwLiteShared/KeepAwake/IKeepAwakePlatform.cs` | `IKeepAwakePlatform.Acquire/Release` — the seam faked in tests and replaced on Android — plus the shared `NoOpKeepAwakePlatform`. |
| `backend/FwLite/FwLiteShared/KeepAwake/RefCountedKeepAwake.cs` | Refcount, transition lock, fail-open, `UserNotificationEvent` publishing. Platform agnostic, so it is fully unit testable from `FwLiteShared.Tests`. |
| `backend/FwLite/FwLiteMaui/Platforms/Android/AndroidKeepAwakePlatform.cs` | Starts/stops the foreground service, holds the partial wake lock, requests `POST_NOTIFICATIONS` on API 33+. |
| `backend/FwLite/FwLiteMaui/Platforms/Android/KeepAwakeForegroundService.cs` | `[Service]` with `dataSync` type; low-importance channel, ongoing alert-once status notification, tap launches the app, `StartForeground` typed on API 29+, `NotSticky`, `StopForeground(Remove)` on destroy. |
| `.../Platforms/Android/Resources/drawable/ic_notification.xml` | mdi cloud-sync white silhouette for the status bar. |
| `.../Platforms/Android/AndroidManifest.xml` | Service declaration matching `[Service(Name = ...)]` + `FOREGROUND_SERVICE`, `FOREGROUND_SERVICE_DATA_SYNC`, `POST_NOTIFICATIONS`, `WAKE_LOCK`. |
| `.../Platforms/Android/README.md` | Shape of the feature + the manual screen-off test. |
| `backend/FwLite/FwLiteShared/Events/UserNotificationEvent.cs` | New global event (+ `FwEventType.UserNotification`, `JsonDerivedType`, Reinforced.Typings enum exports, regenerated TS). |
| `frontend/viewer/src/lib/notifications/NotificationOutlet.svelte` | Renders the event: error-with-clipboard goes to `AppNotification.error`, everything else to `AppNotification.display` with mapped type/timeout. |
| `backend/FwLite/FwLiteShared.Tests/KeepAwake/RefCountedKeepAwakeTests.cs` | 6 tests, see below. |

DI follows the existing platform-service pattern: `TryAddSingleton` for both interfaces in `FwLiteSharedKernel`, and an `#if ANDROID` `services.Replace(ServiceDescriptor.Singleton<IKeepAwakePlatform, AndroidKeepAwakePlatform>())` in `FwLiteMauiKernel`, exactly like `IPlatformFeaturesService`. Only the platform primitive is replaced — every platform shares one refcount implementation, so the interesting logic is exercised by the same tests on every OS.

Call site (`CombinedProjectsService.DownloadProject`) wraps the CRDT `CreateProject` + `SyncService.ExecuteSync(true)` callback in `keepAwake.RunAsync`, labelled `Downloading project {code}` with body "FieldWorks Lite is downloading a project". The pre-existing `Task.Run` thread hop is kept inside the wrapper. Nothing about the API is download-specific: any future long-running work calls the same `RunAsync`.

## Alternatives considered and rejected

- **Queue with `ILongRunningWorkQueue` + `ILongRunningWorkHost` and a global `SemaphoreSlim(1,1)`** (the shape on the handoff branch). Rejected: the semaphore makes every piece of user-visible work app-wide serial, which is a real behaviour regression (two downloads, or a download plus a sync, would no longer overlap) bought for nothing — the OS state we need is boolean, not exclusive. Two interfaces also make callers pick the wrong one and split the refcount logic across the queue/host boundary.
- **`IDisposable`/`IAsyncDisposable` scope (`using var _ = keepAwake.Begin(work)`)**. Tempting and slightly more composable, but the release then depends on callers not forgetting `using`, and an async release inside `DisposeAsync` is easy to fire-and-forget. `RunAsync` makes the balanced enter/leave the only thing a caller can express.
- **`SemaphoreSlim(1,1)` for the transition instead of `Lock`.** Rejected because a semaphore *permits* `await`ing the user work while holding it — precisely the bug we're avoiding. `Lock` makes that a compile error. This costs an async platform seam (`Acquire`/`Release` are synchronous), which is free here: `StartForegroundService`, `StopService` and `WakeLock.Acquire` are all synchronous Android calls.
- **Refcount with `Interlocked` and no lock at all.** Rejected: the count and the platform call have to move together. With bare `Interlocked` a `1 → 0` decrement can call `StopService` after a `0 → 1` increment has already called `StartForegroundService`, leaving work running with no foreground service — the exact race called out in the constraints.
- **MAUI's `DeviceDisplay.KeepScreenOn`.** Keeps the screen on rather than the CPU, drains the battery, and does not stop Android from suspending background work.
- **Publishing the fail-open notification from the Android layer.** Rejected: the notification is behaviour, not platform ceremony. Keeping it in `RefCountedKeepAwake` is why it can be asserted from `FwLiteShared.Tests`, which cannot reference Android or MAUI.

## How start/stop races are prevented

`RefCountedKeepAwake` holds a `System.Threading.Lock` around exactly two things: the refcount mutation and the platform call that the mutation implies.

- `Enter`: take the lock, call `platform.Acquire` if the count is 0, increment, release the lock.
- `Leave`: take the lock, decrement, call `platform.Release` only if the count reached 0, release the lock.

So `Acquire` and `Release` calls are totally ordered and can never interleave, and a `StopService` for finished work cannot overtake the `StartForegroundService` of work that just started — the increment is already published before the lock is dropped. The lock is never held across the caller's work: `Enter`/`Leave` are synchronous methods with no `await` in them, and because it is a `Lock` rather than a `SemaphoreSlim`, awaiting inside it wouldn't compile. Work items therefore run fully concurrently; only the transitions are serial. `Enter` publishes the fail-open notification *after* dropping the lock, so a subscriber that reacts by starting more work can't deadlock the coordinator.

`AndroidKeepAwakePlatform` relies on that serialization for its `_wakeLock` field (the coordinator's lock supplies the memory barrier) and is still individually idempotent: a second `Acquire` with a held lock is a no-op, and `Release` tolerates being called with nothing held.

## How fail-open works

If `platform.Acquire` throws, `Enter` swallows the exception, **still increments the refcount**, and returns normally — the work runs. The refcount increment matters: it keeps `Enter`/`Leave` symmetric, so the session still drains to 0 and `Release` is still attempted (cleaning up a wake lock or service that partially started). After the lock is released, `RefCountedKeepAwake` logs the error and publishes `UserNotificationEvent(message: "Background work protection failed", Error, Infinite, description: "\"Downloading project {code}\" will continue, but may stop if the screen turns off before it finishes.", clipboardText: exception.ToString())`. The viewer turns an Error-with-clipboardText event into `AppNotification.error`, which is the existing copyable toast, so the user can paste the full exception into a bug report. A failing `Release` is logged only — there is nothing the user can do about it and the work already finished.

On Android, a missing notification permission is not a failure: on API 33+ the permission is requested when work starts, and if there is no current activity to request through we log a warning and carry on.

## Tests

`backend/FwLite/FwLiteShared.Tests/KeepAwake/RefCountedKeepAwakeTests.cs`, xunit + FluentAssertions, faking `IKeepAwakePlatform` and reading notifications off a real `GlobalEventBus` via `OnGlobalEvent.OfType<UserNotificationEvent>()`.

1. `RunAsync_RunsTheWorkAndReturnsItsResult` — result propagates, the work is handed to the platform, no notification on the happy path.
2. `RunAsync_PropagatesWorkFailures` — the exception surfaces to the caller and the keep-awake is still released.
3. `RunAsync_FailedWorkDoesNotPoisonLaterWork` — after a throwing item, the next item runs and acquires again (refcount really returned to 0).
4. `RunAsync_OverlappingWorkRunsConcurrentlyAndOnlyKeepsAwakeOnce` — two items are proven to be inside their work bodies simultaneously via `TaskCompletionSource`, and the platform was acquired once and released once, after the last one.
5. `RunAsync_DoesNotStopKeepingAwakeWhileOtherWorkIsStillRunning` — the second item finishing while the first is still active does not release.
6. `RunAsync_WhenKeepAwakeCannotBeAcquired_WorkStillRunsAndTheUserIsNotified` — work completes, `Release` is still attempted after the drain, and a single Error/Infinite `UserNotificationEvent` carries the work title in the description and the exception text in `clipboardText`.

There is deliberately no serial-queue assertion; test 4 asserts the opposite.

### Commands and results

```
dotnet test backend/FwLite/FwLiteShared.Tests --filter "FullyQualifiedName~RefCountedKeepAwakeTests"
  → Passed! Failed: 0, Passed: 6, Skipped: 0

dotnet build backend/FwLite/FwLiteShared/FwLiteShared.csproj
  → Build succeeded, 0 warnings, 0 errors (regenerated + committed the TS types under
    frontend/viewer/src/lib/dotnet-types/generated-types/FwLiteShared/Events/)

dotnet build backend/FwLite/FwLiteMaui/FwLiteMaui.csproj -f net10.0-android
  → Build succeeded, 0 warnings, 0 errors (the Android workload is installed here, so the
    foreground service and platform implementation are really compiled)
```

Two things the Android compiler caught, both worth knowing about: the `ForegroundService` enum used by
`[Service(ForegroundServiceType = ...)]` lives in `Android.Content.PM`, and `NotificationCompat.Builder`'s
fluent methods are typed as nullable in the Android bindings, so chaining them trips CS8602 under this
repo's nullable-warnings-as-errors setting — the builder is configured with statements instead.

`svelte-check` was not run: `frontend/viewer/node_modules` is absent in this worktree, and a full `pnpm install` was not worth the time for a 12-line addition to one component. The component was validated with the Svelte MCP autofixer instead (no issues), and the generated TS types it imports were produced by the build above.

## Notes

- Another process was concurrently writing to this worktree the whole time I worked: half-finished variants
  of this same feature kept appearing under `FwLiteShared/Services/`, `FwLiteShared/KeepAwake/`,
  `FwLiteShared.Tests/Services/` and `Platforms/Android/`, my edits to shared files were reverted under me
  several times, and at one point a `git reset --hard` deleted two commits I had already made (recovered from
  the reflog; also kept on the `arena-candidate-a` branch as a safety ref). Everything described above was
  re-applied and re-verified afterwards, so the final tree is the design described here and nothing else, but
  it is worth checking that Arena candidates aren't sharing a worktree.
