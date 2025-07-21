using System.Text.Json.Serialization;

namespace LexCore.Sync;

public record SyncJobResult([property: JsonConverter(typeof(JsonStringEnumConverter))] SyncJobStatusEnum Status, string? Error, SyncResult? SyncResult = null);

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SyncJobStatusEnum
{
    Success,
    ProjectNotFound,
    SyncJobNotFound,
    SyncJobTimedOut,
    TimedOutAwaitingSyncStatus,
    UnableToAuthenticate,
    UnableToSync,
    CrdtSyncFailed,
    SendReceiveFailed,
    UnknownError,
}
