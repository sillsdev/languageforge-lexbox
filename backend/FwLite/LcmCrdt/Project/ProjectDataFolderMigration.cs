using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace LcmCrdt.Project;

/// <summary>
/// One-time copy of a legacy data folder (flat <c>{code}.sqlite</c> files plus <c>localResourcesCache</c>)
/// into a new root that uses the project-folder layout (<see cref="LcmCrdtConfig.UseProjectFolders"/>).
/// Used to move Windows MSIX installs out of the package folder, which is deleted on uninstall (#1391).
/// <para>
/// Everything is copied into a staging folder and verified there, and the new root then appears in one
/// directory rename together with a marker file. A crash or any failure leaves the legacy folder untouched and
/// in use, and the migration is retried on the next start. Only after the new root exists are the legacy
/// databases renamed to <c>.sqlite.migrated</c>: their contents stay, but no app version can open and sync them
/// any more, which would fork sync since both copies carry the same ClientId.
/// </para>
/// <para>
/// Two app instances must never end up on different roots. A process that keeps using the legacy folder holds
/// a shared lease (<see cref="MigrationResult.LegacyLease"/>) for its lifetime, and a migration only runs while
/// it can hold that lease exclusively. Migrations are serialized by a lock that is waited on without timeout.
/// </para>
/// </summary>
public static class ProjectDataFolderMigration
{
    public const string MarkerFileName = "fwlite-data-migration.json";
    public const string LocalResourcesCacheDirName = "localResourcesCache";
    public const string FailedSyncsDirName = "failedSyncs";
    public const string RetiredDbExtension = ".migrated";
    private const string StagingSuffix = ".migrating-";

    public record Marker(string MigratedFrom, DateTimeOffset MigratedAt, string[] Projects);

    /// <param name="Migrated">true when the target root is ready to use; false when the caller must keep using
    /// the legacy root with the flat layout.</param>
    /// <param name="LegacyLease">when using the legacy root: hold (don't dispose) for the process lifetime, so no
    /// other instance migrates the data out from under this one. Null if it couldn't be taken.</param>
    public sealed record MigrationResult(bool Migrated, IDisposable? LegacyLease = null);

    public static MigrationResult TryMigrate(string legacyRoot, string targetRoot, ILogger logger)
    {
        legacyRoot = Path.GetFullPath(legacyRoot);
        targetRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(targetRoot));
        var targetName = Path.GetFileName(targetRoot);
        FileStream migrationLock;
        string parent;
        try
        {
            if (IsMigrated(targetRoot, legacyRoot, logger)) return new(Migrated: true);
            parent = Path.GetDirectoryName(targetRoot) ?? throw new ArgumentException($"'{targetRoot}' has no parent folder", nameof(targetRoot));
            Directory.CreateDirectory(parent);
            migrationLock = AcquireMigrationLock(parent, targetName, logger);
        }
        catch (Exception e)
        {
            // the target can't even be prepared, so no other instance can migrate into it either
            logger.LogError(e, "Failed to prepare data migration to {TargetRoot}, keeping data in {LegacyRoot}", targetRoot, legacyRoot);
            return new(Migrated: false);
        }

        using (migrationLock)
        {
            try
            {
                return MigrateWhileLocked(legacyRoot, targetRoot, parent, targetName, logger);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed to migrate data from {LegacyRoot} to {TargetRoot}, keeping data in the legacy folder", legacyRoot, targetRoot);
                return UseLegacy(parent, targetName, logger);
            }
        }
    }

    private static MigrationResult MigrateWhileLocked(string legacyRoot, string targetRoot, string parent, string targetName, ILogger logger)
    {
        // another process may have finished while we waited for the lock
        if (IsMigrated(targetRoot, legacyRoot, logger)) return new(Migrated: true);
        if (Directory.Exists(targetRoot))
        {
            logger.LogError("{TargetRoot} already exists but was not created by a data migration, keeping data in {LegacyRoot}", targetRoot, legacyRoot);
            return UseLegacy(parent, targetName, logger);
        }

        string[] projects;
        using (var exclusiveLease = OpenLegacyLease(parent, targetName, FileShare.None))
        {
            if (exclusiveLease is null)
            {
                logger.LogWarning("Another FieldWorks Lite instance is using {LegacyRoot}, data will be migrated on a later start", legacyRoot);
                return UseLegacy(parent, targetName, logger);
            }

            DeleteStagingFolders(parent, targetName, logger);
            var staging = Path.Combine(parent, targetName + StagingSuffix + Guid.NewGuid().ToString("N"));
            try
            {
                Directory.CreateDirectory(staging);
                projects = CopyAndVerify(legacyRoot, staging, targetRoot, logger);
                WriteMarker(staging, new Marker(legacyRoot, DateTimeOffset.UtcNow, projects));
                Directory.Move(staging, targetRoot);
            }
            finally
            {
                if (Directory.Exists(staging)) TryDeleteDirectory(staging, logger);
            }

            logger.LogInformation("Migrated {ProjectCount} projects from {LegacyRoot} to {TargetRoot}", projects.Length, legacyRoot, targetRoot);
            RetireLegacyDatabases(legacyRoot, projects, logger);
        }

        return new(Migrated: true);
    }

    private static MigrationResult UseLegacy(string parent, string targetName, ILogger logger)
    {
        var lease = OpenLegacyLease(parent, targetName, FileShare.ReadWrite);
        if (lease is null) logger.LogWarning("Failed to take the legacy data folder lease");
        return new(Migrated: false, lease);
    }

    /// <summary>
    /// Shared (<see cref="FileShare.ReadWrite"/>) by every process using the legacy folder; exclusive
    /// (<see cref="FileShare.None"/>) while migrating. Returns null when the requested mode conflicts with a
    /// handle another process holds.
    /// </summary>
    private static FileStream? OpenLegacyLease(string parent, string targetName, FileShare share)
    {
        try
        {
            return new FileStream(Path.Combine(parent, targetName + ".legacy-in-use.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, share);
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static bool IsMigrated(string targetRoot, string legacyRoot, ILogger logger)
    {
        if (!File.Exists(Path.Combine(targetRoot, MarkerFileName))) return false;
        if (Directory.Exists(legacyRoot) && Directory.EnumerateFiles(legacyRoot, "*.sqlite").Any())
        {
            // e.g. a downgraded version created projects in the legacy folder; they are not migrated twice
            logger.LogWarning("{LegacyRoot} contains projects created after data was migrated to {TargetRoot}; they are not shown", legacyRoot, targetRoot);
        }
        return true;
    }

    private static FileStream AcquireMigrationLock(string parent, string targetName, ILogger logger)
    {
        // no timeout: giving up while another instance migrates would put the two instances on different roots
        var lockPath = Path.Combine(parent, targetName + ".migration.lock");
        var logged = false;
        while (true)
        {
            try
            {
                return new FileStream(lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 1, FileOptions.DeleteOnClose);
            }
            catch (IOException)
            {
                if (!logged) logger.LogInformation("Waiting for another FieldWorks Lite instance to finish migrating data");
                logged = true;
                Thread.Sleep(500);
            }
        }
    }

    private static void RetireLegacyDatabases(string legacyRoot, string[] projects, ILogger logger)
    {
        foreach (var code in projects)
        {
            var legacyDb = Path.Combine(legacyRoot, $"{code}.sqlite");
            try
            {
                File.Move(legacyDb, legacyDb + RetiredDbExtension);
            }
            catch (Exception e)
            {
                logger.LogWarning(e, "Failed to retire migrated database {LegacyDb}", legacyDb);
            }
        }
    }

    private static string[] CopyAndVerify(string legacyRoot, string staging, string targetRoot, ILogger logger)
    {
        if (!Directory.Exists(legacyRoot)) return [];
        var legacyCache = Path.Combine(legacyRoot, LocalResourcesCacheDirName);
        var stagingCache = Path.Combine(staging, LocalResourcesCacheDirName);
        // media first: verifying the databases checks that every media file they reference was copied
        if (Directory.Exists(legacyCache)) CopyDirectory(legacyCache, stagingCache);

        var oldCachePrefix = AsDirectoryPrefix(legacyCache);
        var newCachePrefix = AsDirectoryPrefix(Path.Combine(targetRoot, LocalResourcesCacheDirName));
        var codes = new List<string>();
        foreach (var legacyDb in Directory.EnumerateFiles(legacyRoot, "*.sqlite"))
        {
            var code = Path.GetFileNameWithoutExtension(legacyDb);
            var stagedDb = LcmCrdtConfig.ProjectFolderDbPath(staging, code);
            Directory.CreateDirectory(Path.GetDirectoryName(stagedDb)!);
            CopyDatabase(legacyDb, stagedDb);
            RewriteLocalResourcePaths(stagedDb, oldCachePrefix, newCachePrefix);
            VerifyDatabase(legacyDb, stagedDb);
            VerifyLocalResources(stagedDb, oldCachePrefix, newCachePrefix, AsDirectoryPrefix(stagingCache), AsDirectoryPrefix(legacyRoot), logger);
            codes.Add(code);
        }

        var legacyFailedSyncs = Path.Combine(legacyRoot, FailedSyncsDirName);
        if (Directory.Exists(legacyFailedSyncs))
        {
            try
            {
                CopyDirectory(legacyFailedSyncs, Path.Combine(staging, FailedSyncsDirName));
            }
            catch (IOException e)
            {
                // diagnostics only, not worth failing the migration over
                logger.LogWarning(e, "Failed to copy {FailedSyncs}", legacyFailedSyncs);
            }
        }

        return [.. codes];
    }

    private static string AsDirectoryPrefix(string dir) => Path.TrimEndingDirectorySeparator(dir) + Path.DirectorySeparatorChar;

    private static SqliteConnection Open(string dbPath, SqliteOpenMode mode)
    {
        // no pooling: a pooled handle would keep the staged file open and block the final directory rename.
        // Never ReadOnly: a read-only last connection can't clean up a WAL db's -wal/-shm files on close.
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = dbPath, Mode = mode, Pooling = false
        }.ConnectionString);
        connection.Open();
        return connection;
    }

    /// <summary>
    /// Uses SQLite's backup API rather than a file copy, so pending WAL content is included and the copy is a
    /// consistent snapshot.
    /// </summary>
    private static void CopyDatabase(string sourceDb, string destDb)
    {
        using var source = Open(sourceDb, SqliteOpenMode.ReadWrite);
        using var dest = Open(destDb, SqliteOpenMode.ReadWriteCreate);
        source.BackupDatabase(dest);
    }

    /// <summary>
    /// Harmony stores absolute media paths (<c>LocalResource.LocalPath</c>), so rows pointing into the legacy
    /// media cache are re-pointed at the new one. Rows elsewhere (or relative ones) are left alone.
    /// </summary>
    private static void RewriteLocalResourcePaths(string dbPath, string oldPrefix, string newPrefix)
    {
        using var connection = Open(dbPath, SqliteOpenMode.ReadWrite);
        if (!TableExists(connection, "LocalResource")) return;
        using var command = connection.CreateCommand();
        command.CommandText = """
                              UPDATE LocalResource SET LocalPath = @newPrefix || substr(LocalPath, length(@oldPrefix) + 1)
                              WHERE substr(LocalPath, 1, length(@oldPrefix)) = @oldPrefix COLLATE NOCASE
                              """;
        command.Parameters.AddWithValue("@oldPrefix", oldPrefix);
        command.Parameters.AddWithValue("@newPrefix", newPrefix);
        command.ExecuteNonQuery();
    }

    private static void VerifyDatabase(string sourceDb, string stagedDb)
    {
        using var source = Open(sourceDb, SqliteOpenMode.ReadWrite);
        using var staged = Open(stagedDb, SqliteOpenMode.ReadWrite);
        // quick_check rather than integrity_check: the full check compares index contents, which needs the
        // custom writing-system collations that only the app registers
        using (var check = staged.CreateCommand())
        {
            check.CommandText = "PRAGMA quick_check";
            var result = check.ExecuteScalar() as string;
            if (result != "ok") throw new InvalidDataException($"Copied database '{stagedDb}' failed quick_check: {result}");
        }

        foreach (var table in ListTables(source))
        {
            var expected = CountRows(source, table);
            var actual = CountRows(staged, table);
            if (expected != actual)
                throw new InvalidDataException($"Copied database '{stagedDb}' has {actual} rows in '{table}', expected {expected}");
        }
    }

    private static void VerifyLocalResources(string stagedDb, string oldPrefix, string newPrefix, string stagingPrefix, string legacyRootPrefix, ILogger logger)
    {
        var leftInLegacy = 0;
        using var connection = Open(stagedDb, SqliteOpenMode.ReadWrite);
        if (!TableExists(connection, "LocalResource")) return;
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT LocalPath FROM LocalResource";
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var localPath = reader.GetString(0);
            if (!localPath.StartsWith(newPrefix, StringComparison.Ordinal))
            {
                // the app only stores media in the cache, but anything else in the legacy folder is lost on uninstall
                if (localPath.StartsWith(legacyRootPrefix, StringComparison.OrdinalIgnoreCase)) leftInLegacy++;
                continue;
            }
            var relative = localPath[newPrefix.Length..];
            // a file that was already missing before the migration can't be expected to be copied
            if (File.Exists(oldPrefix + relative) && !File.Exists(stagingPrefix + relative))
                throw new InvalidDataException($"Media file '{relative}' referenced by '{stagedDb}' was not copied");
        }

        if (leftInLegacy > 0)
            logger.LogWarning("{Count} media files referenced by {StagedDb} are outside the media cache and were not migrated", leftInLegacy, stagedDb);
    }

    private static bool TableExists(SqliteConnection connection, string table)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT count(*) FROM sqlite_master WHERE type = 'table' AND name = @name";
        command.Parameters.AddWithValue("@name", table);
        return (long)command.ExecuteScalar()! > 0;
    }

    private static List<string> ListTables(SqliteConnection connection)
    {
        // virtual tables (FTS) are skipped: counting them goes through their module; their shadow tables are counted
        using var command = connection.CreateCommand();
        command.CommandText = """
                              SELECT name FROM sqlite_master
                              WHERE type = 'table' AND name NOT LIKE 'sqlite_%' AND sql NOT LIKE 'CREATE VIRTUAL TABLE%'
                              """;
        using var reader = command.ExecuteReader();
        var tables = new List<string>();
        while (reader.Read()) tables.Add(reader.GetString(0));
        return tables;
    }

    private static long CountRows(SqliteConnection connection, string table)
    {
        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT count(*) FROM \"{table.Replace("\"", "\"\"")}\"";
        return (long)command.ExecuteScalar()!;
    }

    private static void CopyDirectory(string source, string dest)
    {
        Directory.CreateDirectory(dest);
        foreach (var dir in Directory.EnumerateDirectories(source, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(Path.Combine(dest, Path.GetRelativePath(source, dir)));
        }

        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            var destFile = Path.Combine(dest, Path.GetRelativePath(source, file));
            File.Copy(file, destFile);
            var expected = new FileInfo(file).Length;
            var actual = new FileInfo(destFile).Length;
            if (expected != actual)
                throw new InvalidDataException($"Copied file '{destFile}' is {actual} bytes, expected {expected}");
        }
    }

    private static void WriteMarker(string root, Marker marker)
    {
        File.WriteAllText(Path.Combine(root, MarkerFileName), JsonSerializer.Serialize(marker));
    }

    private static void DeleteStagingFolders(string parent, string targetName, ILogger logger)
    {
        // left behind by a crash; safe to remove because we hold the migration lock
        foreach (var dir in Directory.EnumerateDirectories(parent, targetName + StagingSuffix + "*"))
        {
            TryDeleteDirectory(dir, logger);
        }
    }

    private static void TryDeleteDirectory(string dir, ILogger logger)
    {
        try
        {
            Directory.Delete(dir, true);
        }
        catch (Exception e)
        {
            logger.LogWarning(e, "Failed to delete migration staging folder {StagingFolder}", dir);
        }
    }
}
