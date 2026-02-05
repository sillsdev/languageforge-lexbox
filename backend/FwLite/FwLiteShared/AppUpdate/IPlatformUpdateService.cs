using System.Text.Json.Serialization;
using LexCore.Entities;

namespace FwLiteShared.AppUpdate;

public interface IPlatformUpdateService
{
    DateTime LastUpdateCheck { get; set; }
    bool IsOnMeteredConnection();
    bool SupportsAutoUpdate { get; }

    /// <summary>
    /// True for platforms that handle their own update checking (e.g., Android via Play Store).
    /// When true, UpdateChecker will skip the HTTP call to LexBox/GitHub and delegate to
    /// <see cref="CheckForUpdateAsync"/> instead.
    /// </summary>
    bool HandlesOwnUpdateCheck => false;

    /// <summary>
    /// Platform-specific update check. Only called when <see cref="HandlesOwnUpdateCheck"/> is true.
    /// For Android, this queries the Play Store for available updates.
    /// </summary>
    Task<AvailableUpdate?> CheckForUpdateAsync() => Task.FromResult<AvailableUpdate?>(null);

    Task<UpdateResult> ApplyUpdate(FwLiteRelease latestRelease);
    Task<bool> RequestPermissionToUpdate(FwLiteRelease latestRelease);
}

public record AvailableUpdate(FwLiteRelease Release, bool SupportsAutoUpdate);

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
