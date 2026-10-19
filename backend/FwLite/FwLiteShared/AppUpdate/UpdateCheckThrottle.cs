using System.Globalization;
using FwLiteShared.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FwLiteShared.AppUpdate;

/// <summary>
/// Tracks when we last checked for an update and decides, based on
/// <see cref="FwLiteConfig.UpdateCheckCondition"/> and <see cref="FwLiteConfig.UpdateCheckInterval"/>,
/// whether enough time has elapsed to check again. Shared by <see cref="UpdateChecker"/> (the release-feed
/// check) and the Android Play in-app update service so the interval gate and last-check persistence live
/// in one place instead of being reimplemented per platform.
/// </summary>
public class UpdateCheckThrottle(
    IPreferencesService preferences,
    IOptions<FwLiteConfig> config,
    ILogger<UpdateCheckThrottle> logger)
{
    internal const string LastUpdateCheckKey = "lastUpdateChecked";

    /// <summary>When we last checked for an update, or <see cref="DateTime.MinValue"/> if never.</summary>
    public DateTime LastUpdateCheck =>
        DateTime.TryParse(preferences.Get(LastUpdateCheckKey), CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind, out var value)
            ? value
            : DateTime.MinValue;

    /// <summary>Record that a check just happened, so the interval starts again.</summary>
    public void RecordCheck() =>
        preferences.Set(LastUpdateCheckKey, DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture));

    /// <summary>
    /// Whether enough time has elapsed to check for an update, honoring the configured condition and
    /// interval. This is platform-agnostic: the release-feed store skip (iOS/Mac/Android) lives in
    /// <see cref="UpdateChecker"/>, since it's specific to the GitHub-feed path.
    /// </summary>
    public bool ShouldCheckForUpdate()
    {
        var cfg = config.Value;
        if (cfg.UpdateCheckCondition == UpdateCheckCondition.Never)
        {
            logger.LogInformation("Update check prevented by configuration");
            return false;
        }
        if (cfg.UpdateCheckCondition == UpdateCheckCondition.Always)
        {
            logger.LogInformation("Update check forced by configuration");
            return true;
        }

        var lastChecked = LastUpdateCheck;
        var timeSinceLastCheck = DateTime.UtcNow - lastChecked;
        if (timeSinceLastCheck.TotalHours < -1)
        {
            logger.LogInformation("Should check for update, because last check was in the future: {LastCheck}",
                lastChecked);
            return true;
        }

        if (timeSinceLastCheck < cfg.UpdateCheckInterval)
        {
            logger.LogInformation("Should not check for update, because last check was too recent: {LastCheck}",
                lastChecked);
            return false;
        }

        logger.LogInformation("Should check for update based on last check time: {LastCheck}", lastChecked);
        return true;
    }
}
