# Android Long-Running Work

FwLite uses `AndroidForegroundWorkHost` and `ForegroundWorkService` as a generic foreground-service host for user-visible long-running work. The service only maintains Android foreground-service state and notification lifecycle; queued work continues to run through the shared .NET `ILongRunningWorkQueue`.

## Manual Sleep Test

1. Start FwLite on an Android device or emulator.
2. Start downloading a large project from the normal project download UI.
3. Turn the screen off and wait long enough to exceed the normal screen-sleep window.
4. Turn the screen back on.
5. Verify the project completed or surfaced a real error, rather than silently stopping while the screen was off.
