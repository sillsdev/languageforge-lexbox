using FwHeadless.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;

namespace Testing.FwHeadless;

/// <summary>
/// Unit tests for the per-project creation reservation that stops a sync from racing a project that
/// is still being created from a template (and stops two concurrent creations of the same project).
/// </summary>
public class SyncHostedServiceCreationReservationTests
{
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

        svc.EndProjectCreation(projectId);
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

        svc.EndProjectCreation(projectId);
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
}
