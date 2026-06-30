using System.Collections.Concurrent;
using FwLiteShared.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace FwLiteShared.Tests.Services;

public class InProcessLongRunningWorkQueueTests
{
    [Fact]
    public async Task RunsQueuedWorkSerially()
    {
        var host = new FakeLongRunningWorkHost();
        var queue = CreateQueue(host);
        var firstStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseFirst = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var observed = new ConcurrentQueue<string>();

        var first = queue.EnqueueAsync(Request("First"), async _ =>
        {
            observed.Enqueue("first-start");
            firstStarted.SetResult();
            await releaseFirst.Task.WaitAsync(TimeSpan.FromSeconds(5));
            observed.Enqueue("first-end");
        });
        await firstStarted.Task;

        var second = queue.EnqueueAsync(Request("Second"), _ =>
        {
            observed.Enqueue("second-start");
            secondStarted.SetResult();
            return Task.CompletedTask;
        });

        var earlySecondStart = await Task.WhenAny(secondStarted.Task, Task.Delay(100));
        earlySecondStart.Should().NotBe(secondStarted.Task);

        releaseFirst.SetResult();
        await Task.WhenAll(first, second);

        observed.Should().Equal("first-start", "first-end", "second-start");
    }

    [Fact]
    public async Task PropagatesWorkExceptionAndContinuesWithNextItem()
    {
        var queue = CreateQueue();

        var failedWork = async () => await queue.EnqueueAsync(
            Request("Fails"),
            _ => throw new InvalidOperationException("boom"));

        await failedWork.Should().ThrowAsync<InvalidOperationException>().WithMessage("boom");

        var result = await queue.EnqueueAsync(Request("Succeeds"), _ => Task.FromResult(42));
        result.Should().Be(42);
    }

    [Fact]
    public async Task KeepsHostActiveUntilQueuedWorkDrains()
    {
        var host = new FakeLongRunningWorkHost();
        var queue = CreateQueue(host);
        var firstStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseFirst = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var first = queue.EnqueueAsync(Request("First"), async _ =>
        {
            firstStarted.SetResult();
            await releaseFirst.Task.WaitAsync(TimeSpan.FromSeconds(5));
        });
        await firstStarted.Task;

        var second = queue.EnqueueAsync(Request("Second"), _ => Task.CompletedTask);
        releaseFirst.SetResult();
        await Task.WhenAll(first, second);

        host.StartedTitles.Should().Equal("First", "Second");
        host.DrainedCount.Should().Be(1);
    }

    private static InProcessLongRunningWorkQueue CreateQueue(ILongRunningWorkHost? host = null)
    {
        return new InProcessLongRunningWorkQueue(
            host ?? new FakeLongRunningWorkHost(),
            NullLogger<InProcessLongRunningWorkQueue>.Instance);
    }

    private static LongRunningWorkRequest Request(string title) => new(title, $"{title} notification");

    private sealed class FakeLongRunningWorkHost : ILongRunningWorkHost
    {
        public List<string> StartedTitles { get; } = [];
        public int DrainedCount { get; private set; }

        public Task WorkStartedAsync(LongRunningWorkRequest request, CancellationToken cancellationToken)
        {
            StartedTitles.Add(request.Title);
            return Task.CompletedTask;
        }

        public Task WorkQueueDrainedAsync(CancellationToken cancellationToken)
        {
            DrainedCount++;
            return Task.CompletedTask;
        }
    }
}
