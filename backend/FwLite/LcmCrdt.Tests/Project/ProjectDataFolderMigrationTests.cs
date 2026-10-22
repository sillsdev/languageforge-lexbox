using System.Security.Cryptography;
using System.Text;
using LcmCrdt.Project;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using MiniLcm.Media;

namespace LcmCrdt.Tests.Project;

public class ProjectDataFolderMigrationTests : IAsyncLifetime
{
    private readonly string _testRoot = Path.GetFullPath(Path.Join("project-data-migration-tests", Guid.NewGuid().ToString("N")));
    private string LegacyRoot => Path.Join(_testRoot, "LocalState");
    private string TargetRoot => Path.Join(_testRoot, "ProgramData", "user");
    private readonly List<ServiceProvider> _providers = [];
    private readonly List<IDisposable> _leases = [];

    public Task InitializeAsync()
    {
        Directory.CreateDirectory(LegacyRoot);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        foreach (var lease in _leases) lease.Dispose();
        foreach (var provider in _providers) await provider.DisposeAsync();
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(_testRoot)) Directory.Delete(_testRoot, true);
    }

    private bool Migrate() => MigrateKeepingLease().Migrated;

    /// <summary>like an app instance, keeps any legacy lease until the test ends</summary>
    private ProjectDataFolderMigration.MigrationResult MigrateKeepingLease()
    {
        var result = ProjectDataFolderMigration.TryMigrate(LegacyRoot, TargetRoot, NullLogger.Instance);
        if (result.LegacyLease is not null)
        {
            lock (_leases) _leases.Add(result.LegacyLease);
        }
        return result;
    }

    private string LeaseFile => Path.Join(Path.GetDirectoryName(TargetRoot)!, "user.legacy-in-use.lock");

    private CrdtProjectsService ProjectsService(string projectPath, bool useProjectFolders)
    {
        var provider = new ServiceCollection()
            .AddTestLcmCrdtClient()
            .Configure<LcmCrdtConfig>(config =>
            {
                config.ProjectPath = projectPath;
                config.UseProjectFolders = useProjectFolders;
            })
            .BuildServiceProvider();
        _providers.Add(provider);
        return provider.GetRequiredService<CrdtProjectsService>();
    }

    private async Task<IMiniLcmApi> Open(CrdtProject project)
    {
        var scope = _providers.Last().CreateAsyncScope();
        return await scope.ServiceProvider.OpenCrdtProject(project);
    }

    private async Task DisposeProviders()
    {
        foreach (var provider in _providers) await provider.DisposeAsync();
        _providers.Clear();
        SqliteConnection.ClearAllPools();
    }

    private Dictionary<string, string> HashFiles(string root)
    {
        return Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
            .ToDictionary(f => Path.GetRelativePath(root, f), f => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(f))));
    }

    [Fact]
    public async Task MigratesProjectWithEntriesAndMedia()
    {
        var legacyService = ProjectsService(LegacyRoot, useProjectFolders: false);
        var legacyProject = await legacyService.CreateExampleProject("Example");
        var legacyApi = await Open(legacyProject);
        var expectedEntries = await legacyApi.GetEntries().Select(e => e.Id).ToArrayAsync();
        var upload = await legacyApi.SaveFile(new MemoryStream(Encoding.UTF8.GetBytes("audio bytes")), new LcmFileMetadata("recording.wav", "audio/wav"));
        upload.Result.Should().Be(UploadFileResult.SavedLocally);
        await DisposeProviders();
        var legacyHashes = HashFiles(LegacyRoot);

        Migrate().Should().BeTrue();

        // contents unchanged, only the database is renamed so it can't be opened and synced again
        var expectedLegacy = legacyHashes.ToDictionary(
            kv => kv.Key == "example.sqlite" ? "example.sqlite" + ProjectDataFolderMigration.RetiredDbExtension : kv.Key,
            kv => kv.Value);
        HashFiles(LegacyRoot).Should().BeEquivalentTo(expectedLegacy);
        var service = ProjectsService(TargetRoot, useProjectFolders: true);
        var project = service.GetProject("example");
        project.Should().NotBeNull();
        project.DbPath.Should().Be(LcmCrdtConfig.ProjectFolderDbPath(TargetRoot, "example"));
        var api = await Open(project);
        (await api.GetEntries().Select(e => e.Id).ToArrayAsync()).Should().BeEquivalentTo(expectedEntries);
        await using var file = await api.GetFileStream(upload.MediaUri!.Value, downloadIfMissing: false);
        file.Result.Should().Be(ReadFileResult.Success);
        file.Stream.Should().BeAssignableTo<FileStream>()
            .Which.Name.Should().StartWith(Path.Join(TargetRoot, ProjectDataFolderMigration.LocalResourcesCacheDirName));
        (await new StreamReader(file.Stream!).ReadToEndAsync()).Should().Be("audio bytes");
    }

    [Fact]
    public async Task IncludesChangesStillInTheWal()
    {
        var dbPath = Path.Join(LegacyRoot, "wal-project.sqlite");
        await using (var connection = new SqliteConnection($"Data Source={dbPath};Pooling=false"))
        {
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = """
                                  PRAGMA journal_mode = WAL;
                                  PRAGMA wal_autocheckpoint = 0;
                                  CREATE TABLE Things (Id INTEGER PRIMARY KEY);
                                  INSERT INTO Things VALUES (1), (2), (3);
                                  """;
            await command.ExecuteNonQueryAsync();
            File.Exists(dbPath + "-wal").Should().BeTrue();

            // still open, so nothing has been checkpointed into the main file
            Migrate().Should().BeTrue();
        }

        await using var migrated = new SqliteConnection($"Data Source={LcmCrdtConfig.ProjectFolderDbPath(TargetRoot, "wal-project")};Pooling=false");
        await migrated.OpenAsync();
        await using var count = migrated.CreateCommand();
        count.CommandText = "SELECT count(*) FROM Things";
        (await count.ExecuteScalarAsync()).Should().Be(3L);
    }

    [Fact]
    public async Task RewritesOnlyPathsInTheLegacyMediaCache()
    {
        var mediaFile = Path.Join(LegacyRoot, ProjectDataFolderMigration.LocalResourcesCacheDirName, "proj", "a.png");
        Directory.CreateDirectory(Path.GetDirectoryName(mediaFile)!);
        await File.WriteAllTextAsync(mediaFile, "png");
        var elsewhere = Path.Join(_testRoot, "elsewhere", "b.png");
        await CreateDbWithLocalResources(Path.Join(LegacyRoot, "proj.sqlite"), mediaFile, elsewhere, "relative/c.png");

        Migrate().Should().BeTrue();

        var paths = await ReadLocalPaths(LcmCrdtConfig.ProjectFolderDbPath(TargetRoot, "proj"));
        var migratedMedia = Path.Join(TargetRoot, ProjectDataFolderMigration.LocalResourcesCacheDirName, "proj", "a.png");
        paths.Should().BeEquivalentTo([migratedMedia, elsewhere, "relative/c.png"]);
        (await File.ReadAllTextAsync(migratedMedia)).Should().Be("png");
    }

    [Fact]
    public void EmptyLegacyFolder_CreatesTargetWithMarker()
    {
        Migrate().Should().BeTrue();

        File.Exists(Path.Join(TargetRoot, ProjectDataFolderMigration.MarkerFileName)).Should().BeTrue();
    }

    [Fact]
    public async Task SecondRun_DoesNothing()
    {
        CreateEmptyDb(Path.Join(LegacyRoot, "first.sqlite"));
        Migrate().Should().BeTrue();
        // created after the migration, e.g. by an older version; must not be copied over the migrated data
        CreateEmptyDb(Path.Join(LegacyRoot, "later.sqlite"));

        Migrate().Should().BeTrue();

        File.Exists(LcmCrdtConfig.ProjectFolderDbPath(TargetRoot, "first")).Should().BeTrue();
        File.Exists(Path.Join(LegacyRoot, "first.sqlite")).Should().BeFalse();
        File.Exists(Path.Join(LegacyRoot, "first.sqlite" + ProjectDataFolderMigration.RetiredDbExtension)).Should().BeTrue();
        File.Exists(LcmCrdtConfig.ProjectFolderDbPath(TargetRoot, "later")).Should().BeFalse();
    }

    [Fact]
    public async Task TargetThatWasNotMigrated_IsLeftAlone()
    {
        CreateEmptyDb(Path.Join(LegacyRoot, "proj.sqlite"));
        Directory.CreateDirectory(TargetRoot);
        await File.WriteAllTextAsync(Path.Join(TargetRoot, "someone-elses.txt"), "keep");

        Migrate().Should().BeFalse();

        Directory.EnumerateFileSystemEntries(TargetRoot).Select(Path.GetFileName).Should().BeEquivalentTo(["someone-elses.txt"]);
    }

    [Fact]
    public async Task CorruptDatabase_FailsWithoutCreatingTarget()
    {
        CreateEmptyDb(Path.Join(LegacyRoot, "good.sqlite"));
        await File.WriteAllTextAsync(Path.Join(LegacyRoot, "corrupt.sqlite"), "this is not a sqlite database");
        var legacyHashes = HashFiles(LegacyRoot);

        Migrate().Should().BeFalse();

        Directory.Exists(TargetRoot).Should().BeFalse();
        Directory.EnumerateFileSystemEntries(Path.GetDirectoryName(TargetRoot)!).Should()
            .BeEquivalentTo([LeaseFile], "staging and the migration lock are cleaned up, the lease is held while on legacy");
        HashFiles(LegacyRoot).Should().BeEquivalentTo(legacyHashes);
    }

    [Fact]
    public async Task InstanceStillOnTheLegacyFolder_BlocksMigrationUntilItExits()
    {
        // instance A fails to migrate (corrupt db) and keeps running on the legacy folder
        CreateEmptyDb(Path.Join(LegacyRoot, "proj.sqlite"));
        var corrupt = Path.Join(LegacyRoot, "corrupt.sqlite");
        await File.WriteAllTextAsync(corrupt, "this is not a sqlite database");
        var instanceA = ProjectDataFolderMigration.TryMigrate(LegacyRoot, TargetRoot, NullLogger.Instance);
        instanceA.Migrated.Should().BeFalse();
        instanceA.LegacyLease.Should().NotBeNull();
        File.Delete(corrupt);

        // instance B could migrate now, but must not while A is still writing to the legacy folder
        Migrate().Should().BeFalse();
        Directory.Exists(TargetRoot).Should().BeFalse();
        File.Exists(Path.Join(LegacyRoot, "proj.sqlite")).Should().BeTrue();

        instanceA.LegacyLease!.Dispose();
        foreach (var lease in _leases) lease.Dispose();
        _leases.Clear();

        Migrate().Should().BeTrue();
        File.Exists(LcmCrdtConfig.ProjectFolderDbPath(TargetRoot, "proj")).Should().BeTrue();
    }

    [Fact]
    public async Task ConcurrentStarts_AllUseTheMigratedFolder()
    {
        for (var i = 0; i < 3; i++) CreateEmptyDb(Path.Join(LegacyRoot, $"proj{i}.sqlite"));

        var results = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => Task.Run(MigrateKeepingLease)));

        results.Should().AllSatisfy(r => r.Migrated.Should().BeTrue());
        for (var i = 0; i < 3; i++) File.Exists(LcmCrdtConfig.ProjectFolderDbPath(TargetRoot, $"proj{i}")).Should().BeTrue();
    }

    [Fact]
    public void LeftoverStagingFolderFromACrash_IsRemoved()
    {
        CreateEmptyDb(Path.Join(LegacyRoot, "proj.sqlite"));
        var leftover = Path.Join(Path.GetDirectoryName(TargetRoot)!, "user.migrating-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Join(leftover, "projects", "proj"));

        Migrate().Should().BeTrue();

        Directory.Exists(leftover).Should().BeFalse();
        File.Exists(LcmCrdtConfig.ProjectFolderDbPath(TargetRoot, "proj")).Should().BeTrue();
    }

    private static void CreateEmptyDb(string dbPath)
    {
        using var connection = new SqliteConnection($"Data Source={dbPath};Pooling=false");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "CREATE TABLE Things (Id INTEGER PRIMARY KEY); INSERT INTO Things VALUES (1);";
        command.ExecuteNonQuery();
    }

    private static async Task CreateDbWithLocalResources(string dbPath, params string[] localPaths)
    {
        await using var connection = new SqliteConnection($"Data Source={dbPath};Pooling=false");
        await connection.OpenAsync();
        await using var create = connection.CreateCommand();
        // same shape as Harmony's table (see the AddResources migration)
        create.CommandText = "CREATE TABLE LocalResource (Id TEXT NOT NULL PRIMARY KEY, LocalPath TEXT NOT NULL)";
        await create.ExecuteNonQueryAsync();
        foreach (var localPath in localPaths)
        {
            await using var insert = connection.CreateCommand();
            insert.CommandText = "INSERT INTO LocalResource (Id, LocalPath) VALUES (@id, @path)";
            insert.Parameters.AddWithValue("@id", Guid.NewGuid().ToString());
            insert.Parameters.AddWithValue("@path", localPath);
            await insert.ExecuteNonQueryAsync();
        }
    }

    private static async Task<List<string>> ReadLocalPaths(string dbPath)
    {
        await using var connection = new SqliteConnection($"Data Source={dbPath};Pooling=false");
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT LocalPath FROM LocalResource";
        await using var reader = await command.ExecuteReaderAsync();
        var paths = new List<string>();
        while (await reader.ReadAsync()) paths.Add(reader.GetString(0));
        return paths;
    }
}
