using System.Collections.Concurrent;

namespace FwHeadless.Services;

public class SyncJobStatusService() : ISyncJobStatusService
{
    private ConcurrentDictionary<Guid, SyncJobStatus> Status { get; init; } = new();

    public void StartSyncing(Guid projectId)
    {
        Status.AddOrUpdate(projectId, SyncJobStatus.Running, (_, _) => SyncJobStatus.Running);
    }

    public void StopSyncing(Guid projectId)
    {
        Status.Remove(projectId, out _);
    }

    public SyncJobStatus SyncStatus(Guid projectId)
    {
        return Status.TryGetValue(projectId, out var status) ? status : SyncJobStatus.NotRunning;
    }
}

public enum SyncJobStatus
{
    NotRunning,
    Running,
}
