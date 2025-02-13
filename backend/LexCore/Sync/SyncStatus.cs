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
    int PendingMercurialChanges, // Will be -1 if there is no clone yet; this means "all the commits, but we don't know how many there will be"
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
