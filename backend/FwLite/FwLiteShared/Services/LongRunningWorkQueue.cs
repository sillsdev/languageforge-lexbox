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
            if (lockTaken) queueLock.Release();
            await CompleteQueuedSlot();
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
            logger.LogError(e, "Error starting long-running work host for {WorkTitle}", request.Title);
        }
    }

    private async Task CompleteQueuedSlot()
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
