using LcmCrdt.Project;
using Microsoft.Extensions.Logging;

namespace FwLiteMaui;

/// <summary>
/// The folder projects are read from and created in. When <see cref="FwLiteMauiConfig.ProjectDataDir"/> moved
/// away from <see cref="FwLiteMauiConfig.BaseDataDir"/>, existing data is migrated there first; if that fails
/// the app keeps using the old folder and layout, and the migration is retried on the next start.
/// </summary>
/// <param name="LegacyLease">held for the process lifetime (this is a singleton) while using the old folder, so
/// another instance can't migrate the data away from under this one</param>
public record ProjectDataLocation(string Root, bool UseProjectFolders, IDisposable? LegacyLease = null)
{
    public static ProjectDataLocation Resolve(FwLiteMauiConfig config, ILogger logger)
    {
        if (config.ProjectDataDir == config.BaseDataDir) return new(config.BaseDataDir, UseProjectFolders: false);
        var result = ProjectDataFolderMigration.TryMigrate(config.BaseDataDir, config.ProjectDataDir, logger);
        return result.Migrated
            ? new(config.ProjectDataDir, UseProjectFolders: true)
            : new(config.BaseDataDir, UseProjectFolders: false, result.LegacyLease);
    }
}
