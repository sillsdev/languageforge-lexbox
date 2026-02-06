using System.Text.Json.Serialization;
using LexCore.Entities;

namespace FwLiteShared.AppUpdate;

public interface IPlatformUpdateService
{
    DateTime LastUpdateCheck { get; set; }
    bool IsOnMeteredConnection();
    bool SupportsAutoUpdate { get; }

    /// <summary>
    /// Check if an update is available. The base implementation queries LexBox/GitHub via HTTP.
    /// Platforms like Android override this to query their app store instead.
    /// </summary>
    Task<ShouldUpdateResponse> ShouldUpdateAsync();

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
