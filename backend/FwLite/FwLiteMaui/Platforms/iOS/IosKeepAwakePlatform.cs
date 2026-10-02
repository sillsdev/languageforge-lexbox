using Foundation;
using FwLiteShared.KeepAwake;
using Microsoft.Extensions.Logging;
using UIKit;

namespace FwLiteMaui;

/// <summary>
/// iOS has no foreground-service concept like Android. While work runs we disable the idle timer so
/// the screen doesn't auto-lock (and suspend the app) while it is foregrounded, and we hold a
/// UIApplication background task so that if the user backgrounds the app mid-work iOS grants a short
/// grace period (historically ~30s) to finish or checkpoint rather than suspending immediately.
/// If that grace period expires, we begin a new background task when the app returns to the
/// foreground, so the next backgrounding of the same work gets a grace period too.
/// <see cref="RefCountedKeepAwake"/> serializes Acquire/Release; all UIApplication access is
/// marshalled to the main thread, where all of this class's state lives.
/// </summary>
public sealed class IosKeepAwakePlatform(ILogger<IosKeepAwakePlatform> logger) : IKeepAwakePlatform
{
    private nint _backgroundTaskId = UIApplication.BackgroundTaskInvalid;
    // Non-null while work is active.
    private string? _activeWorkTitle;
    private NSObject? _foregroundObserver;

    public void Acquire(KeepAwakeWork work)
    {
        OnMainThread("acquire", () =>
        {
            _activeWorkTitle = work.Title;
            UIApplication.SharedApplication.IdleTimerDisabled = true;
            _foregroundObserver ??= UIApplication.Notifications.ObserveWillEnterForeground((_, _) =>
                RunLogged("renew", BeginBackgroundTaskIfNeeded));
            BeginBackgroundTaskIfNeeded();
        });
    }

    public void Release()
    {
        OnMainThread("release", () =>
        {
            _activeWorkTitle = null;
            UIApplication.SharedApplication.IdleTimerDisabled = false;
            _foregroundObserver?.Dispose();
            _foregroundObserver = null;
            EndBackgroundTask(_backgroundTaskId);
        });
    }

    // The action runs after Acquire/Release return, so RefCountedKeepAwake can't catch its exceptions,
    // and an unhandled exception on the main thread would crash the app.
    private void OnMainThread(string operation, Action action)
    {
        MainThread.BeginInvokeOnMainThread(() => RunLogged(operation, action));
    }

    private void RunLogged(string operation, Action action)
    {
        try
        {
            action();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to {Operation} iOS keep-awake", operation);
        }
    }

    private void BeginBackgroundTaskIfNeeded()
    {
        if (_activeWorkTitle is null || _backgroundTaskId != UIApplication.BackgroundTaskInvalid) return;
        var taskId = UIApplication.BackgroundTaskInvalid;
        // iOS calls the expiration handler on the main thread, so taskId is assigned before it can run.
        taskId = UIApplication.SharedApplication.BeginBackgroundTask(_activeWorkTitle,
            () => RunLogged("expire", () => OnBackgroundTimeExpired(taskId)));
        _backgroundTaskId = taskId;
    }

    private void OnBackgroundTimeExpired(nint taskId)
    {
        // iOS reclaimed our background time before the work finished. Ending the task here is
        // mandatory, or the OS terminates the app. The in-flight download will be suspended and
        // should resume or retry when the app next returns to the foreground, where we renew the task.
        logger.LogWarning(
            "iOS background time expired before keep-awake work finished; work may be suspended until the app returns to the foreground");
        EndBackgroundTask(taskId);
    }

    private void EndBackgroundTask(nint taskId)
    {
        if (taskId == UIApplication.BackgroundTaskInvalid) return;
        UIApplication.SharedApplication.EndBackgroundTask(taskId);
        if (_backgroundTaskId == taskId) _backgroundTaskId = UIApplication.BackgroundTaskInvalid;
    }
}
