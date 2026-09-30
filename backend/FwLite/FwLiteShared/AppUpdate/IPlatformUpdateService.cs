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
    /// Restarts the app so a downloaded update can be applied. Only meaningful on platforms that
    /// support auto-update; elsewhere it's a no-op. On success the process is terminated by the platform,
    /// so this typically does not return.
    /// </summary>
    Task RestartToApplyUpdate();
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum UpdateResult
{
    Unknown,
    Success,
    Failed,
    Started,
    ManualUpdateRequired,
    Disallowed
}
