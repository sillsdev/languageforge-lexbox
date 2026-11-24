using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace LcmCrdt.Project;

/// <summary>
/// One-time copy of a legacy data folder (flat <c>{code}.sqlite</c> files plus <c>localResourcesCache</c>)
/// into a new root that uses the project-folder layout (<see cref="LcmCrdtConfig.UseProjectFolders"/>), where
/// each project's database and media share one folder.
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
    public const string FailedSyncsDirName = "failedSyncs";
    public const string RetiredDbExtension = ".migrated";
    private const string StagingSuffix = ".migrating-";

    public record Marker(string MigratedFrom, DateTimeOffset MigratedAt, string[] Projects);

    public enum MigrationOutcome
    {
        /// <summary>By an earlier start, or by another instance while this one waited for the lock.</summary>
        AlreadyMigrated,
        Migrated,
        /// <summary>Another instance is still using the legacy root; retried on a later start.</summary>
        Deferred,
        /// <summary>The target root exists but wasn't created by a migration.</summary>
        TargetExists,
        Failed,
    }

    /// <param name="LegacyLease">when using the legacy root: hold (don't dispose) for the process lifetime, so no
    /// other instance migrates the data out from under this one. Null if it couldn't be taken.</param>
    public sealed record MigrationResult(MigrationOutcome Outcome, IDisposable? LegacyLease = null)
    {
        /// <summary>true when the target root is ready to use; false when the caller must keep using the legacy
        /// root with the flat layout.</summary>
        public bool Migrated => Outcome is MigrationOutcome.Migrated or MigrationOutcome.AlreadyMigrated;
        public int ProjectCount { get; init; }
        /// <summary>Databases plus media.</summary>
        public long Bytes { get; init; }
        public TimeSpan Duration { get; init; }
        /// <summary>Media referenced from outside the project's legacy media folder, so still lost on uninstall.</summary>
        public int MediaLeftBehind { get; init; }
        /// <summary>Legacy databases that couldn't be renamed, so an older version could still open and sync them.</summary>
        public int RetireFailures { get; init; }
        public Exception? Error { get; init; }
    }

    public static MigrationResult TryMigrate(string legacyRoot, string targetRoot, ILogger logger)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = TryMigrateCore(legacyRoot, targetRoot, logger);
        return result with { Duration = stopwatch.Elapsed };
    }

    private static MigrationResult TryMigrateCore(string legacyRoot, string targetRoot, ILogger logger)
    {
        legacyRoot = Path.GetFullPath(legacyRoot);
        targetRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(targetRoot));
        var targetName = Path.GetFileName(targetRoot);
        FileStream migrationLock;
        string parent;
        try
        {
            if (IsMigrated(targetRoot, legacyRoot, logger)) return new(MigrationOutcome.AlreadyMigrated);
            parent = Path.GetDirectoryName(targetRoot) ?? throw new ArgumentException($"'{targetRoot}' has no parent folder", nameof(targetRoot));
            Directory.CreateDirectory(parent);
            migrationLock = AcquireMigrationLock(parent, targetName, logger);
        }
        catch (Exception e)
        {
            // e.g. no write access to the parent folder, which blocks every other instance's migration too
            logger.LogError(e, "Failed to prepare data migration to {TargetRoot}, keeping data in {LegacyRoot}", targetRoot, legacyRoot);
            return new(MigrationOutcome.Failed) { Error = e };
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
                return UseLegacy(MigrationOutcome.Failed, parent, targetName, logger) with { Error = e };
            }
        }
    }

    private static MigrationResult MigrateWhileLocked(string legacyRoot, string targetRoot, string parent, string targetName, ILogger logger)
    {
        // another process may have finished while we waited for the lock
        if (IsMigrated(targetRoot, legacyRoot, logger)) return new(MigrationOutcome.AlreadyMigrated);
        if (Directory.Exists(targetRoot))
        {
            logger.LogError("{TargetRoot} already exists but was not created by a data migration, keeping data in {LegacyRoot}", targetRoot, legacyRoot);
            return UseLegacy(MigrationOutcome.TargetExists, parent, targetName, logger);
        }

        CopyResult copied;
        int retireFailures;
        using (var exclusiveLease = OpenLegacyLease(parent, targetName, FileShare.None))
        {
            if (exclusiveLease is null)
            {
                logger.LogWarning("Another FieldWorks Lite instance is using {LegacyRoot}, data will be migrated on a later start", legacyRoot);
                return UseLegacy(MigrationOutcome.Deferred, parent, targetName, logger);
            }

            DeleteStagingFolders(parent, targetName, logger);
            var staging = Path.Combine(parent, targetName + StagingSuffix + Guid.NewGuid().ToString("N"));
            try
            {
                Directory.CreateDirectory(staging);
                copied = CopyAndVerify(legacyRoot, staging, targetRoot, logger);
                WriteMarker(staging, new Marker(legacyRoot, DateTimeOffset.UtcNow, copied.Projects));
                MoveIntoPlace(staging, targetRoot);
            }
            finally
            {
                if (Directory.Exists(staging)) TryDeleteDirectory(staging, logger);
            }

            logger.LogInformation("Migrated {ProjectCount} projects from {LegacyRoot} to {TargetRoot}", copied.Projects.Length, legacyRoot, targetRoot);
            retireFailures = RetireLegacyDatabases(legacyRoot, copied.Projects, logger);
        }

        return new(MigrationOutcome.Migrated)
        {
            ProjectCount = copied.Projects.Length,
            Bytes = copied.Bytes,
            MediaLeftBehind = copied.MediaLeftBehind,
            RetireFailures = retireFailures,
        };
    }

    private static void MoveIntoPlace(string staging, string targetRoot)
    {
        // an antivirus or indexer still reading a just-written file blocks the rename for a moment
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                Directory.Move(staging, targetRoot);
                return;
            }
            catch (Exception e) when (attempt < 20 && e is IOException or UnauthorizedAccessException)
            {
                Thread.Sleep(500);
            }
        }
    }

    private static MigrationResult UseLegacy(MigrationOutcome outcome, string parent, string targetName, ILogger logger)
    {
        var lease = OpenLegacyLease(parent, targetName, FileShare.ReadWrite);
        if (lease is null) logger.LogWarning("Failed to take the legacy data folder lease");
        return new(outcome, lease);
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
            logger.LogError("{LegacyRoot} contains projects created after data was migrated to {TargetRoot}; they are not shown", legacyRoot, targetRoot);
        }
        return true;
    }

    private static FileStream AcquireMigrationLock(string parent, string targetName, ILogger logger)
    {
        // no timeout: giving up while another instance migrates would put the two instances on different roots
        var lockPath = Path.Combine(parent, targetName + ".migration.lock");
        var logged = false;
        DateTime? deniedSince = null;
        while (true)
        {
            try
            {
                return new FileStream(lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 1, FileOptions.DeleteOnClose);
            }
            // other IO errors (disk full, bad path) won't clear up by waiting; they fall back to the legacy root
            catch (Exception e) when (e is UnauthorizedAccessException || IsSharingViolation(e))
            {
                // access is also denied while the previous holder's lock file is pending deletion (e.g. an antivirus
                // handle is still open); only a lasting denial means we can't lock at all
                if (e is UnauthorizedAccessException)
                {
                    deniedSince ??= DateTime.UtcNow;
                    if (DateTime.UtcNow - deniedSince > TimeSpan.FromSeconds(30)) throw;
                }
                else deniedSince = null;
                if (!logged) logger.LogInformation("Waiting for another FieldWorks Lite instance to finish migrating data");
                logged = true;
                Thread.Sleep(500);
            }
        }
    }

    // ERROR_SHARING_VIOLATION or ERROR_LOCK_VIOLATION; elsewhere .NET reports a held lock as a bare IOException
    private static bool IsSharingViolation(Exception e) =>
        e is IOException && (!OperatingSystem.IsWindows() || (e.HResult & 0xFFFF) is 32 or 33);

    private static int RetireLegacyDatabases(string legacyRoot, string[] projects, ILogger logger)
    {
        var failures = 0;
        foreach (var code in projects)
        {
            var legacyDb = Path.Combine(legacyRoot, $"{code}.sqlite");
            try
            {
                File.Move(legacyDb, legacyDb + RetiredDbExtension);
            }
            catch (Exception e)
            {
                // an older version could still open and sync it, forking sync with the migrated copy
                logger.LogError(e, "Failed to retire migrated database {LegacyDb}", legacyDb);
                failures++;
            }
        }
        return failures;
    }

    private sealed record CopyResult(string[] Projects, long Bytes, int MediaLeftBehind);

    private static CopyResult CopyAndVerify(string legacyRoot, string staging, string targetRoot, ILogger logger)
    {
        if (!Directory.Exists(legacyRoot)) return new([], 0, 0);
        var legacyCache = Path.Combine(legacyRoot, LcmCrdtConfig.LocalResourcesCacheDirName);
        var legacyDbs = Directory.GetFiles(legacyRoot, "*.sqlite");
        string[] projects = [.. legacyDbs.Select(db => Path.GetFileNameWithoutExtension(db))];
        // a failed migration is retried on every start, so fail the cheap checks before the (large) media copy
        var bytes = EnsureFreeSpace(staging, legacyDbs, legacyCache);
        foreach (var legacyDb in legacyDbs)
        {
            var stagedDb = LcmCrdtConfig.ProjectFolderDbPath(staging, Path.GetFileNameWithoutExtension(legacyDb));
            Directory.CreateDirectory(Path.GetDirectoryName(stagedDb)!);
            CopyDatabase(legacyDb, stagedDb);
            VerifyDatabase(legacyDb, stagedDb);
        }

        var mediaLeftBehind = projects.Sum(code => CopyProjectMedia(code, legacyRoot, legacyCache, staging, logger));
        CopyProjectDataCache(legacyRoot, staging, targetRoot, legacyDbs, logger);

        var legacyFailedSyncs = Path.Combine(legacyRoot, FailedSyncsDirName);
        if (Directory.Exists(legacyFailedSyncs))
        {
            try
            {
                CopyDirectory(legacyFailedSyncs, Path.Combine(staging, FailedSyncsDirName));
            }
            catch (Exception e)
            {
                // diagnostics only, not worth failing the migration over
                logger.LogWarning(e, "Failed to copy {FailedSyncs}", legacyFailedSyncs);
            }
        }

        return new(projects, bytes, mediaLeftBehind);
    }

    private static long EnsureFreeSpace(string staging, string[] legacyDbs, string legacyCache)
    {
        var needed = legacyDbs.Sum(db => FileLength(db) + FileLength(db + "-wal"));
        if (Directory.Exists(legacyCache))
            needed += new DirectoryInfo(legacyCache).EnumerateFiles("*", SearchOption.AllDirectories).Sum(f => f.Length);
        var available = new DriveInfo(Path.GetPathRoot(staging)!).AvailableFreeSpace;
        // headroom so a migration doesn't leave the disk full
        const long headroom = 200L * 1024 * 1024;
        if (available < needed + headroom)
            throw new IOException($"Not enough free space to migrate: {needed} bytes needed plus {headroom} spare, {available} available");
        return needed;
    }

    private static long FileLength(string path) => File.Exists(path) ? new FileInfo(path).Length : 0;

    /// <summary>
    /// The project list reads names, ids and servers from this cache without opening each project. Its keys are
    /// db paths, so they are re-keyed; without it every project shows as local-only until it is opened.
    /// </summary>
    private static void CopyProjectDataCache(string legacyRoot, string staging, string targetRoot, string[] legacyDbs, ILogger logger)
    {
        var legacyCacheFile = Path.Combine(legacyRoot, LcmCrdtConfig.DefaultProjectCacheFileName);
        if (!File.Exists(legacyCacheFile)) return;
        try
        {
            var newDbPaths = legacyDbs.ToDictionary(db => db,
                db => LcmCrdtConfig.ProjectFolderDbPath(targetRoot, Path.GetFileNameWithoutExtension(db)),
                StringComparer.OrdinalIgnoreCase);
            var legacyEntries = JsonNode.Parse(File.ReadAllText(legacyCacheFile))?.AsObject() ?? new JsonObject();
            var entries = new JsonObject();
            foreach (var (dbPath, entry) in legacyEntries)
            {
                if (entry is not null && newDbPaths.TryGetValue(dbPath, out var newDbPath))
                    entries[newDbPath] = entry.DeepClone();
            }
            File.WriteAllText(Path.Combine(staging, LcmCrdtConfig.DefaultProjectCacheFileName), entries.ToJsonString());
        }
        catch (Exception e)
        {
            // only a cache: a project's entry is rebuilt when it is opened
            logger.LogWarning(e, "Failed to migrate the project data cache {CacheFile}", legacyCacheFile);
        }
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

    /// <summary>
    /// Copies a project's media out of the shared legacy cache into its project folder and re-points its
    /// <c>LocalResource.LocalPath</c> rows there. Legacy rows are absolute, or relative to the cache when written
    /// with <c>StoreLocalResourcePathsRelativeToCache</c>; migrated rows are always relative.
    /// </summary>
    private static int CopyProjectMedia(string code, string legacyRoot, string legacyCache, string staging, ILogger logger)
    {
        var legacyMedia = AsDirectoryPrefix(Path.Combine(legacyCache, code));
        var stagedMedia = LcmCrdtConfig.ProjectFolderResourcesPath(staging, code);
        if (Directory.Exists(legacyMedia)) CopyDirectory(legacyMedia, stagedMedia);

        var stagedDb = LcmCrdtConfig.ProjectFolderDbPath(staging, code);
        using var connection = Open(stagedDb, SqliteOpenMode.ReadWrite);
        if (!TableExists(connection, "LocalResource")) return 0;
        var leftInLegacy = 0;
        foreach (var (id, localPath) in ReadLocalResources(connection))
        {
            var legacyPath = Path.GetFullPath(localPath, legacyCache);
            if (!legacyPath.StartsWith(legacyMedia, StringComparison.OrdinalIgnoreCase))
            {
                // the app only stores media in the project's cache folder, but anything else in the legacy folder is lost on uninstall
                if (legacyPath.StartsWith(AsDirectoryPrefix(legacyRoot), StringComparison.OrdinalIgnoreCase)) leftInLegacy++;
                // relative rows would otherwise resolve against the new cache root
                SetLocalPath(connection, id, legacyPath);
                continue;
            }

            var file = legacyPath[legacyMedia.Length..];
            // a file that was already missing before the migration can't be expected to be copied
            if (File.Exists(legacyPath) && !File.Exists(Path.Combine(stagedMedia, file)))
                throw new InvalidDataException($"Media file '{file}' referenced by '{stagedDb}' was not copied");
            SetLocalPath(connection, id, string.Join('/', code, LcmCrdtConfig.ProjectResourcesDirName, file.Replace(Path.DirectorySeparatorChar, '/')));
        }

        if (leftInLegacy > 0)
            logger.LogWarning("{Count} media files referenced by {StagedDb} are outside the project's media folder and were not migrated", leftInLegacy, stagedDb);
        return leftInLegacy;
    }

    private static void SetLocalPath(SqliteConnection connection, string id, string path)
    {
        using var update = connection.CreateCommand();
        update.CommandText = "UPDATE LocalResource SET LocalPath = @path WHERE Id = @id";
        update.Parameters.AddWithValue("@id", id);
        update.Parameters.AddWithValue("@path", path);
        update.ExecuteNonQuery();
    }

    private static List<(string Id, string LocalPath)> ReadLocalResources(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, LocalPath FROM LocalResource";
        using var reader = command.ExecuteReader();
        var rows = new List<(string, string)>();
        while (reader.Read()) rows.Add((reader.GetString(0), reader.GetString(1)));
        return rows;
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
