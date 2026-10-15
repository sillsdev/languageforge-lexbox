using System.Reactive.Linq;
using FwLiteShared.Events;
using FwLiteShared.KeepAwake;
using Microsoft.Extensions.Logging.Abstractions;

namespace FwLiteShared.Tests.KeepAwake;

public class RefCountedKeepAwakeTests
{
    private static readonly KeepAwakeWork Download = new("Downloading project test-proj");
    private static readonly KeepAwakeWork Sync = new("Syncing project test-proj");
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    private readonly GlobalEventBus _globalEventBus = new(NullLogger<GlobalEventBus>.Instance);
    private readonly List<UserNotificationEvent> _notifications = [];

    public RefCountedKeepAwakeTests()
    {
        _globalEventBus.OnGlobalEvent.OfType<UserNotificationEvent>().Subscribe(_notifications.Add);
    }

    private RefCountedKeepAwake CreateKeepAwake(FakeKeepAwakePlatform platform) =>
        new(platform, _globalEventBus, NullLogger<RefCountedKeepAwake>.Instance);

    [Fact]
    public async Task RunAsync_RunsTheWorkAndReturnsItsResult()
    {
        var platform = new FakeKeepAwakePlatform();

        var result = await CreateKeepAwake(platform).RunAsync(Download, () => Task.FromResult(42));

        result.Should().Be(42);
        platform.AcquiredWork.Should().ContainSingle().Which.Should().Be(Download);
        platform.ReleaseCount.Should().Be(1);
        _notifications.Should().BeEmpty();
    }

    [Fact]
    public async Task RunAsync_PropagatesWorkFailures()
    {
        var platform = new FakeKeepAwakePlatform();
        var keepAwake = CreateKeepAwake(platform);

        var run = () => keepAwake.RunAsync(Download, () => Task.FromException<int>(new InvalidOperationException("work failed")));

        (await run.Should().ThrowAsync<InvalidOperationException>()).WithMessage("work failed");
        platform.ReleaseCount.Should().Be(1);
    }

    [Fact]
    public async Task RunAsync_FailedWorkDoesNotPoisonLaterWork()
    {
        var platform = new FakeKeepAwakePlatform();
        var keepAwake = CreateKeepAwake(platform);
        var failingRun = () => keepAwake.RunAsync(Download, () => Task.FromException(new InvalidOperationException("work failed")));
        await failingRun.Should().ThrowAsync<InvalidOperationException>();

        var result = await keepAwake.RunAsync(Sync, () => Task.FromResult("finished"));

        result.Should().Be("finished");
        platform.AcquireCount.Should().Be(2, "each run starts from a drained refcount");
        platform.ReleaseCount.Should().Be(2);
    }

#pragma warning disable VSTHRD003 // Avoid awaiting foreign Tasks: the TaskCompletionSources are how these tests hold work open
    [Fact]
    public async Task RunAsync_OverlappingWorkRunsConcurrentlyAndOnlyKeepsAwakeOnce()
    {
        var platform = new FakeKeepAwakePlatform();
        var keepAwake = CreateKeepAwake(platform);
        var bothStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var finishBoth = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var active = 0;

        var download = keepAwake.RunAsync(Download, async () =>
        {
            if (Interlocked.Increment(ref active) == 2) bothStarted.SetResult();
            await finishBoth.Task;
            Interlocked.Decrement(ref active);
        });
        var sync = keepAwake.RunAsync(Sync, async () =>
        {
            if (Interlocked.Increment(ref active) == 2) bothStarted.SetResult();
            await finishBoth.Task;
            Interlocked.Decrement(ref active);
        });

        await bothStarted.Task.WaitAsync(Timeout);
        Volatile.Read(ref active).Should().Be(2);
        platform.AcquireCount.Should().Be(1, "the second item joins the keep awake the first one started");
        platform.ReleaseCount.Should().Be(0);

        finishBoth.SetResult();
        await Task.WhenAll(download, sync).WaitAsync(Timeout);
        platform.AcquireCount.Should().Be(1);
        platform.ReleaseCount.Should().Be(1, "keep awake is released once, after the last item finishes");
    }

    [Fact]
    public async Task RunAsync_DoesNotStopKeepingAwakeWhileOtherWorkIsStillRunning()
    {
        var platform = new FakeKeepAwakePlatform();
        var keepAwake = CreateKeepAwake(platform);
        var finishDownload = new TaskCompletionSource();
        var finishSync = new TaskCompletionSource();
        var download = keepAwake.RunAsync(Download, async () => await finishDownload.Task);
        var sync = keepAwake.RunAsync(Sync, async () => await finishSync.Task);

        finishSync.SetResult();
        await sync.WaitAsync(Timeout);

        platform.ReleaseCount.Should().Be(0, "the download is still running");

        finishDownload.SetResult();
        await download.WaitAsync(Timeout);

        platform.AcquireCount.Should().Be(1);
        platform.ReleaseCount.Should().Be(1);
    }
#pragma warning restore VSTHRD003

    [Fact]
    public async Task RunAsync_WhenKeepAwakeCannotBeAcquired_WorkStillRunsAndTheUserIsNotified()
    {
        var platform = new FakeKeepAwakePlatform(new InvalidOperationException("foreground service was rejected"));
        var workRan = false;

        await CreateKeepAwake(platform).RunAsync(Download, () =>
        {
            workRan = true;
            return Task.CompletedTask;
        });

        workRan.Should().BeTrue("keep awake failures must fail open");
        platform.ReleaseCount.Should().Be(1, "the refcount still drains, so cleanup is still attempted");
        var notification = _notifications.Should().ContainSingle().Subject;
        notification.NotificationType.Should().Be(UserNotificationType.Error);
        notification.Duration.Should().Be(UserNotificationDuration.Infinite);
        notification.Message.Should().Be("Background work protection failed");
        notification.Description.Should().Contain(Download.Title);
        notification.ClipboardText.Should().Contain("foreground service was rejected");
    }

    [Fact]
    public async Task RunAsync_AcquireFailureDoesNotPoisonLaterWork()
    {
        var platform = new FakeKeepAwakePlatform { AcquireFailuresRemaining = 1 };
        var keepAwake = CreateKeepAwake(platform);

        await keepAwake.RunAsync(Download, () => Task.CompletedTask);
        await keepAwake.RunAsync(Sync, () => Task.CompletedTask);

        platform.AcquireCount.Should().Be(2);
        platform.ReleaseCount.Should().Be(2, "each session still drains, so cleanup is attempted after a failed acquire");
        _notifications.Should().ContainSingle();
    }

    private sealed class FakeKeepAwakePlatform(Exception? acquireFailure = null) : IKeepAwakePlatform
    {
        public List<KeepAwakeWork> AcquiredWork { get; } = [];
        public int AcquireCount => AcquiredWork.Count;
        public int ReleaseCount { get; private set; }
        public int AcquireFailuresRemaining { get; set; }

        public void Acquire(KeepAwakeWork work)
        {
            AcquiredWork.Add(work);
            if (acquireFailure is not null) throw acquireFailure;
            if (AcquireFailuresRemaining-- > 0) throw new InvalidOperationException("acquire failed");
        }

        public void Release() => ReleaseCount++;
    }
}
