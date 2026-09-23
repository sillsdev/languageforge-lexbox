using System.Text.Json.Serialization;
using LexCore.Entities;

namespace FwLiteShared.AppUpdate;

public interface IPlatformUpdateService
{
    DateTime LastUpdateCheck { get; set; }
    bool IsOnMeteredConnection();
    bool SupportsAutoUpdate { get; }
    Task<UpdateResult> ApplyUpdate(FwLiteRelease latestRelease);
    Task<bool> RequestPermissionToUpdate(FwLiteRelease latestRelease);

    /// <summary>
    /// Finish installing an update the platform has already downloaded (used by Android's Play flexible
    /// flow, where the app must trigger the install/restart itself). No-op where it doesn't apply.
    /// </summary>
    Task CompleteUpdate() => Task.CompletedTask;
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum UpdateResult
{
    Unknown,
    Success,
    Failed,
    Started,
    ManualUpdateRequired,
    Disallowed,

    /// <summary>An update finished downloading and is waiting for the user to restart to install it.</summary>
    Downloaded
}
