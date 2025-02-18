namespace LexCore.Sync;

public enum ProjectSyncStatusEnum
{
    NeverSynced,
    ReadyToSync,
    Syncing,
    QueuedToSync
}

public record ProjectSyncStatus(
    ProjectSyncStatusEnum status,
    int PendingCrdtChanges = 0,
    int PendingMercurialChanges = 0, // Will be -1 if there is no clone yet; this means "all the commits, but we don't know how many there will be"
    DateTimeOffset? LastCrdtCommitDate = null,
    DateTimeOffset? LastMercurialCommitDate = null)
{
    public static ProjectSyncStatus NeverSynced => new(ProjectSyncStatusEnum.NeverSynced);
    public static ProjectSyncStatus Syncing => new(ProjectSyncStatusEnum.Syncing);
    public static ProjectSyncStatus QueuedToSync => new(ProjectSyncStatusEnum.QueuedToSync);
    public static ProjectSyncStatus ReadyToSync(
        int pendingCrdtChanges,
        int pendingMercurialChanges,
        DateTimeOffset? lastCrdtCommitDate,
        DateTimeOffset? lastMercurialCommitDate)

    {
        return new(ProjectSyncStatusEnum.ReadyToSync, pendingCrdtChanges, pendingMercurialChanges, lastCrdtCommitDate, lastMercurialCommitDate);
    }
}
