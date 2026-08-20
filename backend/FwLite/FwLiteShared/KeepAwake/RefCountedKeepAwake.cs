using FwLiteShared.Events;
using Microsoft.Extensions.Logging;

namespace FwLiteShared.KeepAwake;

public class RefCountedKeepAwake(
    IKeepAwakePlatform platform,
    GlobalEventBus globalEventBus,
    ILogger<RefCountedKeepAwake> logger) : IKeepAwake
{
    private readonly Lock _keepAwakeLock = new();
    private int _activeWorkCount;

    public async Task RunAsync(KeepAwakeWork work, Func<Task> action)
    {
        Enter(work);
        try
        {
            await action();
        }
        finally
        {
            Leave(work);
        }
    }

    public async Task<T> RunAsync<T>(KeepAwakeWork work, Func<Task<T>> action)
    {
        Enter(work);
        try
        {
            return await action();
        }
        finally
        {
            Leave(work);
        }
    }

    private void Enter(KeepAwakeWork work)
    {
        Exception? acquireFailure = null;
        lock (_keepAwakeLock)
        {
            if (_activeWorkCount == 0)
            {
                try
                {
                    platform.Acquire(work);
                }
                catch (Exception e)
                {
                    acquireFailure = e;
                }
            }

            _activeWorkCount++;
        }

        if (acquireFailure is not null) ReportAcquireFailure(work, acquireFailure);
    }

    private void Leave(KeepAwakeWork work)
    {
        Exception? releaseFailure = null;
        lock (_keepAwakeLock)
        {
            if (--_activeWorkCount > 0) return;
            try
            {
                platform.Release();
            }
            catch (Exception e)
            {
                releaseFailure = e;
            }
        }

        if (releaseFailure is not null)
            logger.LogError(releaseFailure, "Failed to stop keeping the device awake after {WorkTitle}", work.Title);
    }

    private void ReportAcquireFailure(KeepAwakeWork work, Exception exception)
    {
        logger.LogError(exception, "Failed to keep the device awake for {WorkTitle}", work.Title);
        try
        {
            globalEventBus.PublishEvent(new UserNotificationEvent("Background work protection failed",
                UserNotificationType.Error,
                UserNotificationDuration.Infinite,
                $"\"{work.Title}\" will continue, but may stop if the screen turns off before it finishes.",
                exception.ToString()));
        }
        catch (Exception publishException)
        {
            logger.LogError(publishException, "Failed to publish keep-awake failure notification");
        }
    }
}
