# Android keep awake

Android suspends the process when the screen turns off, which killed downloads part way through. To stop
that, user-visible long-running work is wrapped in `IKeepAwake.RunAsync` (`FwLiteShared/KeepAwake/`), and on
Android that turns into a `dataSync` foreground service plus a partial wake lock:

- `KeepAwakeForegroundService` is the foreground service. It owns the low-importance notification channel,
  the ongoing notification (tapping it launches the app) and calls `StartForeground` with the `dataSync`
  type on API 29+.
- `AndroidKeepAwakePlatform` is the `IKeepAwakePlatform` implementation registered over the shared no-op by
  `FwLiteMauiKernel`. It starts/stops that service and acquires/releases the wake lock. It also asks for
  `POST_NOTIFICATIONS` on API 33+ if we don't have it yet.

`RefCountedKeepAwake` in FwLiteShared decides *when* those calls happen: a single lock covers only the
active-work count and the platform start/stop. 0→1 starts Android protection, N→0 stops it. Work always
runs outside that lock, so overlapping downloads are concurrent. Nothing here queues or serializes work.

If the foreground service or wake lock can't be established the work still runs (fail open), and the user
gets an error notification with the exception attached, since the download may then die on screen-off.

## Manual sleep test

1. Run FwLite on an Android device or emulator.
2. Start downloading a large project.
3. Turn the screen off and wait past the normal sleep window.
4. Turn the screen back on.
5. The download either completed or shows a real error — it must not have silently stopped.

While the download runs you should see the ongoing "FieldWorks Lite is downloading a project" notification,
and it should disappear when the download finishes.
