using FwHeadless.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;

namespace Testing.FwHeadless;

/// <summary>
/// Unit tests for how SyncHostedService answers a sync request, depending on whether a sync for the
/// project is already queued or already running.
/// </summary>
public class SyncHostedServiceQueueTests
{
    private static SyncHostedService NewService() =>
        new(services: null!, NullLogger<SyncHostedService>.Instance, new MemoryCache(new MemoryCacheOptions()));

    [Fact]
    public void QueueJob_reports_a_second_request_as_already_queued_while_the_sync_has_not_started()
    {
        var svc = NewService();
        var projectId = Guid.NewGuid();

        svc.QueueJob(projectId).Should().Be(QueueJobResult.Queued);
        svc.QueueJob(projectId).Should().Be(QueueJobResult.AlreadyQueued,
            "the queued sync hasn't read the repo yet, so it will pick up the caller's changes");
        svc.IsJobQueuedOrRunning(projectId).Should().BeTrue();
    }

    [Fact]
    public void QueueJob_is_refused_while_a_sync_is_running()
    {
        var svc = NewService();
        var projectId = Guid.NewGuid();

        svc.QueueJob(projectId).Should().Be(QueueJobResult.Queued);
        svc.MarkSyncStarted(projectId);

        svc.QueueJob(projectId).Should().Be(QueueJobResult.SyncAlreadyRunning,
            "the running sync may have missed the caller's changes, so it must not be reported as covering them");
        svc.IsJobQueuedOrRunning(projectId).Should().BeTrue();
    }

    [Fact]
    public void MarkSyncStarted_does_nothing_for_a_project_with_no_sync()
    {
        var svc = NewService();
        var projectId = Guid.NewGuid();

        svc.MarkSyncStarted(projectId);

        svc.IsJobQueuedOrRunning(projectId).Should().BeFalse();
        svc.QueueJob(projectId).Should().Be(QueueJobResult.Queued);
    }
}
