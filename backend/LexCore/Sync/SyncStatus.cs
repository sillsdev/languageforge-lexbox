using System.Text.Json.Serialization;

namespace LexCore.Sync;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ProjectSyncStatusEnum
{
    NeverSynced,
    ReadyToSync,
    Syncing,
    QueuedToSync,
    Unknown
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ProjectSyncStatusErrorCode
{
    NotLoggedIn,
    Unknown
}

public record ProjectSyncStatus(
    [property:JsonConverter(typeof(JsonStringEnumConverter))]
    ProjectSyncStatusEnum status,
    int PendingCrdtChanges = 0,
    int PendingMercurialChanges = 0, // Will be -1 if there is no clone yet; this means "all the commits, but we don't know how many there will be"
    DateTimeOffset? LastCrdtCommitDate = null,
    DateTimeOffset? LastMercurialCommitDate = null,
    ProjectSyncStatusErrorCode? ErrorCode = null,
    string? ErrorMessage = null) : IEquatable<ProjectSyncStatus>
{
    public static ProjectSyncStatus NeverSynced => new(ProjectSyncStatusEnum.NeverSynced);
    public static ProjectSyncStatus Syncing => new(ProjectSyncStatusEnum.Syncing);
    public static ProjectSyncStatus QueuedToSync => new(ProjectSyncStatusEnum.QueuedToSync);
    public static ProjectSyncStatus Unknown(ProjectSyncStatusErrorCode errorCode, string? errorMessage = null)
    {
        return new(ProjectSyncStatusEnum.Unknown, ErrorCode: errorCode, ErrorMessage: errorMessage);
    }

    public static ProjectSyncStatus ReadyToSync(
        int pendingCrdtChanges,
        int pendingMercurialChanges,
        DateTimeOffset? lastCrdtCommitDate,
        DateTimeOffset? lastMercurialCommitDate)

    {
        return new(ProjectSyncStatusEnum.ReadyToSync, pendingCrdtChanges, pendingMercurialChanges, lastCrdtCommitDate, lastMercurialCommitDate);
    }
}
