#if IOS
using FwLiteShared.KeepAwake;
using Microsoft.Extensions.Logging;
using UIKit;

namespace FwLiteMaui;

/// <summary>
/// iOS has no foreground-service concept like Android. While work runs we disable the idle timer so
/// the screen doesn't auto-lock (and suspend the app) while it is foregrounded, and we hold a
/// UIApplication background task so that if the user backgrounds the app mid-work iOS grants a short
/// grace period (historically ~30s) to finish or checkpoint rather than suspending immediately.
/// <see cref="RefCountedKeepAwake"/> serializes Acquire/Release; all UIApplication access is
/// marshalled to the main thread, where the background-task id is the only shared state.
/// </summary>
public sealed class IosKeepAwakePlatform(ILogger<IosKeepAwakePlatform> logger) : IKeepAwakePlatform
{
    private nint _backgroundTaskId = UIApplication.BackgroundTaskInvalid;

    public void Acquire(KeepAwakeWork work)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var app = UIApplication.SharedApplication;
            app.IdleTimerDisabled = true;
            if (_backgroundTaskId == UIApplication.BackgroundTaskInvalid)
            {
                _backgroundTaskId = app.BeginBackgroundTask(work.Title, OnBackgroundTimeExpired);
            }
        });
    }

    public void Release()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            UIApplication.SharedApplication.IdleTimerDisabled = false;
            EndBackgroundTask();
        });
    }

    private void OnBackgroundTimeExpired()
    {
        // iOS reclaimed our background time before the work finished. Ending the task here is
        // mandatory, or the OS terminates the app. The in-flight sync/download will be suspended and
        // should resume or retry when the app next returns to the foreground.
        logger.LogWarning(
            "iOS background time expired before keep-awake work finished; work may be suspended until the app returns to the foreground");
        EndBackgroundTask();
    }

    private void EndBackgroundTask()
    {
        if (_backgroundTaskId == UIApplication.BackgroundTaskInvalid) return;
        UIApplication.SharedApplication.EndBackgroundTask(_backgroundTaskId);
        _backgroundTaskId = UIApplication.BackgroundTaskInvalid;
    }
}
#endif
