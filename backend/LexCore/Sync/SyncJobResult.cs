using System.Text.Json.Serialization;

namespace LexCore.Sync;

public record SyncJobResult
{
    [JsonConstructor]
    protected SyncJobResult(SyncJobStatusEnum status, string? error, SyncResult? syncResult = null)
    {
        Status = status;
        Error = error;
        SyncResult = syncResult;
    }

    public SyncJobResult(SyncResult syncResult) : this(SyncJobStatusEnum.Success, error: null, syncResult: syncResult) { }
    public SyncJobResult(SyncJobStatusEnum status, string error) : this(status, error: error, syncResult: null)
    {
        if (status == SyncJobStatusEnum.Success)
        {
            throw new InvalidOperationException("Cannot create success SyncJobResult with error message");
        }
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public SyncJobStatusEnum Status { get; init; }
    public string? Error { get; init; }
    public SyncResult? SyncResult { get; init; } = null;
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SyncJobStatusEnum
{
    Success,
    SuccessHarmonyOnly,
    ProjectNotFound,
    SyncJobNotFound,
    SyncJobTimedOut,
    TimedOutAwaitingSyncStatus,
    UnableToAuthenticate,
    UnableToSync,
    CrdtSyncFailed,
    SendReceiveFailed,
    SyncBlocked,
    UnknownError,
}
