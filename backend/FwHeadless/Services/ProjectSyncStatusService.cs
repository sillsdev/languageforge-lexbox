using System.Collections.Concurrent;

namespace FwHeadless.Services;

public class SyncJobStatusService()
{
    private ConcurrentDictionary<Guid, SyncJobStatus> Status { get; init; } = new();

    public void StartSyncing(Guid projectId)
    {
        Status.AddOrUpdate(projectId, (_) => SyncJobStatus.Running, (_, _) => SyncJobStatus.Running);
    }

    public void StopSyncing(Guid projectId)
    {
        Status.Remove(projectId, out var _);
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

public enum ProjectSyncStatusEnum
{
    NeverSynced,
    ReadyToSync,
    Syncing,
    QueuedToSync
}

// TODO: Bikeshed this name
public record ProjectSyncStatus(
    ProjectSyncStatusEnum status,
    int ChangesAvailable)
{
    public static ProjectSyncStatus NeverSynced => new(ProjectSyncStatusEnum.NeverSynced, 0);
    public static ProjectSyncStatus Syncing => new(ProjectSyncStatusEnum.Syncing, 0);
    public static ProjectSyncStatus QueuedToSync => new(ProjectSyncStatusEnum.QueuedToSync, 0);
    public static ProjectSyncStatus ReadyToSync(int changes)
    {
        return new(ProjectSyncStatusEnum.ReadyToSync, changes);
    }
}
