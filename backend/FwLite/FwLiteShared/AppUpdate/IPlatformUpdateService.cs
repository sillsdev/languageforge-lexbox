using System.Text.Json.Serialization;
using LexCore.Entities;

namespace FwLiteShared.AppUpdate;

public record UpdateRequest(string AppVersion, FwLiteEdition Edition);
public interface IPlatformUpdateService
{
    UpdateRequest UpdateRequest { get; }
    DateTime LastUpdateCheck { get; set; }
    bool IsOnMeteredConnection();
    bool SupportsAutoUpdate { get; }
    Task<UpdateResult> ApplyUpdate(FwLiteRelease latestRelease);
    Task<bool> RequestPermissionToUpdate(FwLiteRelease latestRelease);
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum UpdateResult
{
    Unknown,
    Success,
    Failed,
    Started,
    ManualUpdateRequired
}
