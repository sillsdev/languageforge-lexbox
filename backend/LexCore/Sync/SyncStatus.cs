namespace LexCore.Sync;

public enum ProjectSyncStatusEnum
{
    NeverSynced,
    ReadyToSync,
    Syncing,
}

public record ProjectSyncStatus(
    ProjectSyncStatusEnum status,
    int PendingCrdtChanges,
    int PendingMercurialChanges,
    DateTimeOffset? LastCrdtCommitDate,
    DateTimeOffset? LastMercurialCommitDate)
{
    public static ProjectSyncStatus NeverSynced => new(ProjectSyncStatusEnum.NeverSynced, 0, 0, null, null);
    public static ProjectSyncStatus Syncing => new(ProjectSyncStatusEnum.Syncing, 0, 0, null, null);
    public static ProjectSyncStatus ReadyToSync(
        int pendingCrdtChanges,
        int pendingMercurialChanges,
        DateTimeOffset? lastCrdtCommitDate,
        DateTimeOffset? lastMercurialCommitDate)

    {
        return new(ProjectSyncStatusEnum.ReadyToSync, pendingCrdtChanges, pendingMercurialChanges, lastCrdtCommitDate, lastMercurialCommitDate);
    }
}
