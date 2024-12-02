using System.Collections.Concurrent;

namespace FwHeadless.Services;

public class ProjectSyncStatusService()
{
    private ConcurrentDictionary<Guid, ProjectSyncStatus> Status { get; init; } = new();

    public void StartSyncing(Guid projectId)
    {
        Status.AddOrUpdate(projectId, (_) => ProjectSyncStatus.Syncing, (_, _) => ProjectSyncStatus.Syncing);
    }

    public void StopSyncing(Guid projectId)
    {
        Status.AddOrUpdate(projectId, (_) => ProjectSyncStatus.ReadyToSync, (_, _) => ProjectSyncStatus.ReadyToSync);
    }

    public ProjectSyncStatus? SyncStatus(Guid projectId)
    {
        return Status.TryGetValue(projectId, out var status) ? status : null;
    }
}

public enum ProjectSyncStatus
{
    ReadyToSync,
    Syncing,
}
