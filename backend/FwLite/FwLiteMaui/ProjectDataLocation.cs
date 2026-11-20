using FwLiteShared.Analytics;
using LcmCrdt.Project;
using Microsoft.Extensions.Logging;
using static LcmCrdt.Project.ProjectDataFolderMigration;

namespace FwLiteMaui;

/// <summary>
/// The folder projects are read from and created in. When <see cref="FwLiteMauiConfig.ResolveProjectDataDir"/> moved
/// away from <see cref="FwLiteMauiConfig.BaseDataDir"/>, existing data is migrated there first; if that fails
/// the app keeps using the old folder and layout, and the migration is retried on the next start.
/// </summary>
/// <param name="LegacyLease">held for the process lifetime (this is a singleton) while using the old folder, so
/// another instance can't migrate the data away from under this one</param>
public record ProjectDataLocation(string Root, bool UseProjectFolders, IDisposable? LegacyLease = null)
{
    /// <summary>Sent on every start that attempts a migration, not once it's done. Failure details are also in
    /// the <see cref="ErrorReporting.ErrorEvent"/> logged with it.</summary>
    public const string MigrationEvent = "data_folder_migration";

    public static ProjectDataLocation Resolve(FwLiteMauiConfig config, IAnalyticsService analytics, ILogger logger)
    {
        var projectDataDir = config.ResolveProjectDataDir();
        if (projectDataDir == config.BaseDataDir) return new(config.BaseDataDir, UseProjectFolders: false);
        var result = TryMigrate(config.BaseDataDir, projectDataDir, logger);
        if (result.Outcome != MigrationOutcome.AlreadyMigrated) analytics.Track(MigrationEvent, MigrationProperties(result));
        return result.Migrated
            ? new(projectDataDir, UseProjectFolders: true)
            : new(config.BaseDataDir, UseProjectFolders: false, result.LegacyLease);
    }

    internal static Dictionary<string, object?> MigrationProperties(MigrationResult result)
    {
        var properties = new Dictionary<string, object?>
        {
            ["outcome"] = result.Outcome.ToString(),
            ["duration_ms"] = (long)result.Duration.TotalMilliseconds,
        };
        if (result.Outcome == MigrationOutcome.Migrated)
        {
            properties["project_count"] = result.ProjectCount;
            properties["bytes"] = result.Bytes;
            properties["media_left_behind"] = result.MediaLeftBehind;
            properties["retire_failures"] = result.RetireFailures;
        }
        if (result.Error is not null)
            properties["exception_type"] = result.Error.GetType().FullName;
        return properties;
    }
}
