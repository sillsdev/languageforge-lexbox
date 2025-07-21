using System.Text.Json.Serialization;

namespace LexCore.Sync;

public record SyncJobResult([property: JsonConverter(typeof(JsonStringEnumConverter))] SyncJobResultEnum Result, string? Error, SyncResult? SyncResult = null);

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SyncJobResultEnum
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
