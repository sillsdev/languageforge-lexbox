using FwLiteShared.Events;
using Microsoft.Extensions.Logging;

namespace FwLiteShared.Services;

public enum LongRunningWorkCategory
{
    General,
    DataSync,
}

public enum LongRunningWorkCancellationBehavior
{
    RunToCompletion,
    CancelWhenCallerCancels,
}

public sealed record LongRunningWorkRequest(
    string Title,
    string NotificationText,
    LongRunningWorkCategory Category = LongRunningWorkCategory.General,
    LongRunningWorkCancellationBehavior CancellationBehavior = LongRunningWorkCancellationBehavior.RunToCompletion);

public interface ILongRunningWorkQueue
{
    Task EnqueueAsync(
        LongRunningWorkRequest request,
        Func<CancellationToken, Task> work,
        CancellationToken cancellationToken = default);

    Task<TResult> EnqueueAsync<TResult>(
        LongRunningWorkRequest request,
        Func<CancellationToken, Task<TResult>> work,
        CancellationToken cancellationToken = default);
}

public interface ILongRunningWorkHost
{
    Task WorkStartedAsync(LongRunningWorkRequest request, CancellationToken cancellationToken);
    Task WorkQueueDrainedAsync(CancellationToken cancellationToken);
}

public sealed class NoOpLongRunningWorkHost : ILongRunningWorkHost
{
    public Task WorkStartedAsync(LongRunningWorkRequest request, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task WorkQueueDrainedAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

public sealed class InProcessLongRunningWorkQueue(
    ILongRunningWorkHost workHost,
    GlobalEventBus globalEventBus,
    ILogger<InProcessLongRunningWorkQueue> logger) : ILongRunningWorkQueue
{
    private readonly SemaphoreSlim queueLock = new(1, 1);
    private int queuedOrRunningCount;
    private int hostActive;

    public async Task EnqueueAsync(
        LongRunningWorkRequest request,
        Func<CancellationToken, Task> work,
        CancellationToken cancellationToken = default)
    {
        await EnqueueAsync<object?>(request,
            async token =>
            {
                await work(token);
                return null;
            },
            cancellationToken);
    }

    public async Task<TResult> EnqueueAsync<TResult>(
        LongRunningWorkRequest request,
        Func<CancellationToken, Task<TResult>> work,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(work);

        Interlocked.Increment(ref queuedOrRunningCount);
        var lockTaken = false;
        try
        {
            var waitToken = request.CancellationBehavior == LongRunningWorkCancellationBehavior.CancelWhenCallerCancels
                ? cancellationToken
                : CancellationToken.None;
            await queueLock.WaitAsync(waitToken);
            lockTaken = true;

            var workToken = request.CancellationBehavior == LongRunningWorkCancellationBehavior.CancelWhenCallerCancels
                ? cancellationToken
                : CancellationToken.None;

            await NotifyWorkStarted(request, workToken);
            return await Task.Run(() => work(workToken), workToken);
        }
        finally
        {
            // Drain while still holding the lock so StopService / wake-lock release
            // cannot race a following StartForegroundService / acquire.
            if (lockTaken)
            {
                await CompleteQueuedSlotUnderLock();
                queueLock.Release();
            }
            else
            {
                Interlocked.Decrement(ref queuedOrRunningCount);
            }
        }
    }

    private async Task NotifyWorkStarted(LongRunningWorkRequest request, CancellationToken cancellationToken)
    {
        Interlocked.Exchange(ref hostActive, 1);
        try
        {
            await workHost.WorkStartedAsync(request, cancellationToken);
        }
        catch (Exception e)
        {
            // Fail open: work continues without foreground/wake-lock protection so downloads still
            // succeed when the host fails. Surface the failure so users can report it.
            logger.LogError(e, "Error starting long-running work host for {WorkTitle}", request.Title);
            try
            {
                globalEventBus.PublishEvent(new UserNotificationEvent(
                    message: "Background work protection failed",
                    notificationType: UserNotificationType.Error,
                    duration: UserNotificationDuration.Infinite,
                    description:
                    $"\"{request.Title}\" will continue, but may stop if the screen turns off. Please report this error.",
                    clipboardText: e.ToString()));
            }
            catch (Exception publishError)
            {
                logger.LogError(publishError, "Failed to publish long-running work host failure event");
            }
        }
    }

    private async Task CompleteQueuedSlotUnderLock()
    {
        if (Interlocked.Decrement(ref queuedOrRunningCount) != 0) return;
        if (Interlocked.Exchange(ref hostActive, 0) == 0) return;

        try
        {
            await workHost.WorkQueueDrainedAsync(CancellationToken.None);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error stopping long-running work host");
        }
    }
}
