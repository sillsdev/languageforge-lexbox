using System.Collections.Concurrent;

namespace FwHeadless.Services;

public class ProjectSyncStatusService()
{
    private ConcurrentDictionary<Guid, ProjectSyncStatusEnum> Status { get; init; } = new();

    public void StartSyncing(Guid projectId)
    {
        Status.AddOrUpdate(projectId, (_) => ProjectSyncStatusEnum.Syncing, (_, _) => ProjectSyncStatusEnum.Syncing);
    }

    public void StopSyncing(Guid projectId)
    {
        Status.Remove(projectId, out var _);
    }

    public ProjectSyncStatusEnum? SyncStatus(Guid projectId)
    {
        return Status.TryGetValue(projectId, out var status) ? status : null;
    }
}

public enum ProjectSyncStatusEnum
{
    NeverSynced,
    ReadyToSync,
    Syncing,
}

// TODO: Bikeshed this name
public record ProjectSyncStatus(
    ProjectSyncStatusEnum status,
    int ChangesAvailable)
{
    public static ProjectSyncStatus NeverSynced => new(ProjectSyncStatusEnum.NeverSynced, 0);
    public static ProjectSyncStatus Syncing => new(ProjectSyncStatusEnum.Syncing, 0);
    public static ProjectSyncStatus ReadyToSync(int changes)
    {
        return new(ProjectSyncStatusEnum.ReadyToSync, changes);
    }
}
