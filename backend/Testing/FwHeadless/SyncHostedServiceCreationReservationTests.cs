using FwHeadless.Services;
using LexCore.Sync;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;

namespace Testing.FwHeadless;

/// <summary>
/// Unit tests for the per-project creation reservation that stops a sync from racing a project that
/// is still being created from a template (and stops two concurrent creations of the same project).
/// </summary>
public class SyncHostedServiceCreationReservationTests
{
    /// Bounded so a regression that never completes the wait fails the test instead of hanging it.
    private static CancellationToken TestTimeout => new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token;

    private static SyncHostedService NewService() =>
        new(services: null!, NullLogger<SyncHostedService>.Instance, new MemoryCache(new MemoryCacheOptions()));

    [Fact]
    public void TryStartProjectCreation_blocks_a_second_concurrent_creation_and_is_reusable_after_release()
    {
        var svc = NewService();
        var projectId = Guid.NewGuid();

        svc.TryStartProjectCreation(projectId).Should().BeTrue();
        svc.TryStartProjectCreation(projectId).Should().BeFalse("a creation is already in flight for this project");
        svc.IsJobQueuedOrRunning(projectId).Should().BeTrue();

        svc.EndProjectCreation(projectId, ProjectCreationStatus.Created);
        svc.IsJobQueuedOrRunning(projectId).Should().BeFalse();
        svc.TryStartProjectCreation(projectId).Should().BeTrue("the reservation is released and reusable");
    }

    [Fact]
    public void QueueJob_is_refused_while_a_project_is_being_created()
    {
        var svc = NewService();
        var projectId = Guid.NewGuid();

        svc.TryStartProjectCreation(projectId).Should().BeTrue();
        svc.QueueJob(projectId).Should().BeFalse("a sync must not race a project that's still being created");

        svc.EndProjectCreation(projectId, ProjectCreationStatus.Created);
        svc.QueueJob(projectId).Should().BeTrue("syncing is allowed once creation has finished");
    }

    [Fact]
    public void A_queued_sync_blocks_a_creation()
    {
        var svc = NewService();
        var projectId = Guid.NewGuid();

        svc.QueueJob(projectId).Should().BeTrue();
        svc.TryStartProjectCreation(projectId).Should().BeFalse("a sync is already queued for this project");
    }

    [Fact]
    public async Task AwaitCreationFinished_returns_the_outcome_the_creation_reported()
    {
        var svc = NewService();
        var projectId = Guid.NewGuid();
        svc.TryStartProjectCreation(projectId).Should().BeTrue();

        var awaiting = svc.AwaitCreationFinished(projectId, TestTimeout);
        awaiting.IsCompleted.Should().BeFalse("the creation is still in flight");

        svc.EndProjectCreation(projectId, ProjectCreationStatus.Failed("the push failed"));

        var result = await awaiting;
        result.Should().NotBeNull();
        result!.Status.Should().Be(ProjectCreationStatusEnum.CreationFailed);
        result.Error.Should().Be("the push failed");
    }

    [Fact]
    public async Task AwaitCreationFinished_returns_a_result_that_finished_before_the_caller_asked()
    {
        var svc = NewService();
        var projectId = Guid.NewGuid();

        svc.TryStartProjectCreation(projectId).Should().BeTrue();
        svc.EndProjectCreation(projectId, ProjectCreationStatus.Created);

        // The caller shows up late (e.g. its own request timed out and it came back to ask). It must
        // still learn the outcome rather than being told nothing ever happened.
        var result = await svc.AwaitCreationFinished(projectId, TestTimeout);
        result.Should().NotBeNull();
        result!.Status.Should().Be(ProjectCreationStatusEnum.Created);
    }

    [Fact]
    public async Task AwaitCreationFinished_returns_null_when_no_creation_is_known()
    {
        var svc = NewService();

        var result = await svc.AwaitCreationFinished(Guid.NewGuid(), TestTimeout);
        result.Should().BeNull("the caller should fall back to deriving the status from the project itself");
    }

    [Fact]
    public async Task AwaitCreationFinished_honours_the_caller_token_without_ending_the_creation()
    {
        var svc = NewService();
        var projectId = Guid.NewGuid();
        svc.TryStartProjectCreation(projectId).Should().BeTrue();

        using var cts = new CancellationTokenSource();
        var awaiting = svc.AwaitCreationFinished(projectId, cts.Token);
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => awaiting);
        svc.IsProjectBeingCreated(projectId).Should().BeTrue("giving up on the wait must not cancel the creation");
    }
}
