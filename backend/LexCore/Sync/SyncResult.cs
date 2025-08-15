using System.Text.Json.Serialization;

namespace LexCore.Sync;

public record SyncResult(int CrdtChanges, int FwdataChanges);

[JsonConverter(typeof(JsonStringEnumConverter<SyncStatus>))]
public enum SyncStatus
{
    Success,
    NotLoggedIn,
    NoServer,
    Offline,
    UnknownError
}
