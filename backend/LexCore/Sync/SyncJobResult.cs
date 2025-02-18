namespace LexCore.Sync;

public record SyncJobResult(SyncJobResultEnum Result, string? Error, SyncResult? SyncResult = null);

public enum SyncJobResultEnum
{
    Success,
    ProjectNotFound,
    UnableToAuthenticate,
    UnableToSync,
    CrdtSyncFailed,
    SendReceiveFailed,
    UnknownError,
}
