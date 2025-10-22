using System.Runtime.CompilerServices;
using System.Text.Json;
using FluentAssertions.Execution;
using FwDataMiniLcmBridge.Api;
using FwLiteProjectSync.Tests.Fixtures;
using LcmCrdt;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MiniLcm;
using MiniLcm.Models;

namespace FwLiteProjectSync.Tests;

[Trait("Category", "Integration")]
public class Sena3SyncTests : IClassFixture<Sena3Fixture>, IAsyncLifetime
{
    private readonly Sena3Fixture _fixture;
    private CrdtFwdataProjectSyncService _syncService = null!;
    private CrdtMiniLcmApi _crdtApi = null!;
    private FwDataMiniLcmApi _fwDataApi = null!;
    private TestProject _project = null!;
    private MiniLcmImport _miniLcmImport = null!;
    private static readonly JsonSerializerOptions IndentedDefaultJsonOptions = new()
    {
        WriteIndented = true,
    };


    public Sena3SyncTests(Sena3Fixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        _project = await _fixture.SetupProjects();
        _crdtApi = _project.CrdtApi;
        _fwDataApi = _project.FwDataApi;
        var services = _project.Services;
        _syncService = services.GetRequiredService<CrdtFwdataProjectSyncService>();
        _miniLcmImport = services.GetRequiredService<MiniLcmImport>();
        _fwDataApi.EntryCount.Should().BeGreaterThan(100, "project should be loaded and have entries");
    }

    public Task DisposeAsync()
    {
        _project.Dispose();
        return Task.CompletedTask;
    }

    private void ShouldAllBeEquivalentTo(Dictionary<Guid, Entry> crdtEntries, Dictionary<Guid, Entry> fwdataEntries)
    {
        crdtEntries.Keys.Except(fwdataEntries.Keys).Should().BeEmpty("there should be no entries in CRDT that are not in FwData");
        fwdataEntries.Keys.Except(crdtEntries.Keys).Should().BeEmpty("there should be no entries in FwData that are not in CRDT");
        crdtEntries.Keys.Should().BeEquivalentTo(fwdataEntries.Keys);
        using (new AssertionScope())
        {
            foreach (var crdtEntry in crdtEntries.Values)
            {
                var fwdataEntry = fwdataEntries[crdtEntry.Id];
                crdtEntry.Should().BeEquivalentTo(fwdataEntry,
                    SyncTests.SyncExclusions,
                    $"CRDT entry {crdtEntry.Id} was synced with FwData");
            }
        }
    }

    //by default the first sync is an import, this will skip that so that the sync will actually sync data
    private async Task BypassImport(bool wsImported = false)
    {
        var snapshot = ProjectSnapshot.Empty;
        if (wsImported) snapshot = snapshot with { WritingSystems = await _fwDataApi.GetWritingSystems() };

        //saving the snapshot will try to read the Id but it will be empty when coming from FwData
        foreach (var ws in snapshot.WritingSystems.Analysis)
        {
            if (ws.MaybeId is null) ws.Id = Guid.NewGuid();
        }
        foreach (var ws in snapshot.WritingSystems.Vernacular)
        {
            if (ws.MaybeId is null) ws.Id = Guid.NewGuid();
        }
        await CrdtFwdataProjectSyncService.SaveProjectSnapshot(_fwDataApi.Project, snapshot);
    }

    //this lets us query entries when there is no writing system
    private async Task WorkaroundMissingWritingSystems()
    {
        //must have at least one writing system to query for entries
        await _crdtApi.CreateWritingSystem((await _fwDataApi.GetWritingSystems()).Vernacular.First());

    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task DryRunImport_MakesNoChanges()
    {
        await WorkaroundMissingWritingSystems();
        _crdtApi.GetEntries().ToBlockingEnumerable().Should().BeEmpty();
        await _syncService.SyncDryRun(_crdtApi, _fwDataApi);
        //should still be empty
        _crdtApi.GetEntries().ToBlockingEnumerable().Should().BeEmpty();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task DryRunImport_MakesTheSameChangesAsImport()
    {
        var dryRunSyncResult = await _syncService.SyncDryRun(_crdtApi, _fwDataApi);
        var syncResult = await _syncService.Sync(_crdtApi, _fwDataApi);
        dryRunSyncResult.Should().BeEquivalentTo(syncResult);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task DryRunSync_MakesNoChanges()
    {
        await BypassImport();
        await WorkaroundMissingWritingSystems();
        _crdtApi.GetEntries().ToBlockingEnumerable().Should().BeEmpty();
        await _syncService.SyncDryRun(_crdtApi, _fwDataApi);
        //should still be empty
        _crdtApi.GetEntries().ToBlockingEnumerable().Should().BeEmpty();
    }

    [Fact]
    [Trait("Category", "Slow")]
    [Trait("Category", "Integration")]
    public async Task DryRunSync_MakesTheSameChangesAsSync()
    {
        //syncing requires querying entries, which fails if there are no writing systems, so we import those first
        await _miniLcmImport.ImportWritingSystems(_crdtApi, _fwDataApi);
        await BypassImport(true);

        var dryRunSyncResult = await _syncService.SyncDryRun(_crdtApi, _fwDataApi);
        var syncResult = await _syncService.Sync(_crdtApi, _fwDataApi);
        dryRunSyncResult.CrdtChanges.Should().Be(syncResult.CrdtChanges);
        //can't test fwdata changes as they will not work correctly since the sync code expects Crdts to contain data from FWData
        //this throws off the algorithm and it will try to delete everything in fwdata since there's no data in the crdt since it was a dry run
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task FirstSena3SyncJustDoesAnImport()
    {
        _fwDataApi.EntryCount.Should().BeGreaterThan(1000,
            "projects with less than 1000 entries don't trip over the default query limit");

        var results = await _syncService.Sync(_crdtApi, _fwDataApi);
        results.FwdataChanges.Should().Be(0);
        results.CrdtChanges.Should().BeGreaterThanOrEqualTo(_fwDataApi.EntryCount);

        var crdtEntries = await _crdtApi.GetAllEntries().ToDictionaryAsync(e => e.Id);
        var fwdataEntries = await _fwDataApi.GetAllEntries().ToDictionaryAsync(e => e.Id);
        fwdataEntries.Count.Should().Be(_fwDataApi.EntryCount);
        ShouldAllBeEquivalentTo(crdtEntries, fwdataEntries);
        SyncTests.AssertSnapshotsAreEquivalent(
            await _fwDataApi.TakeProjectSnapshot(),
            await _crdtApi.TakeProjectSnapshot()
        );
    }

    [Fact]
    [Trait("Category", "Slow")]
    [Trait("Category", "Integration")]
    public async Task SyncWithoutImport_CrdtShouldMatchFwdata()
    {
        await BypassImport();

        var results = await _syncService.Sync(_crdtApi, _fwDataApi);
        results.FwdataChanges.Should().Be(0);
        results.CrdtChanges.Should().BeGreaterThan(_fwDataApi.EntryCount);

        var crdtEntries = await _crdtApi.GetAllEntries().ToDictionaryAsync(e => e.Id);
        var fwdataEntries = await _fwDataApi.GetAllEntries().ToDictionaryAsync(e => e.Id);
        fwdataEntries.Count.Should().Be(_fwDataApi.EntryCount);
        ShouldAllBeEquivalentTo(crdtEntries, fwdataEntries);
        SyncTests.AssertSnapshotsAreEquivalent(
            await _fwDataApi.TakeProjectSnapshot(),
            await _crdtApi.TakeProjectSnapshot()
        );
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task SecondSena3SyncDoesNothing()
    {
        await _syncService.Sync(_crdtApi, _fwDataApi);
        var secondSync = await _syncService.Sync(_crdtApi, _fwDataApi);
        secondSync.CrdtChanges.Should().Be(0);
        secondSync.FwdataChanges.Should().Be(0);
    }

    /// <summary>
    /// This test maintains a "live" sena-3 crdt db and fw-headless snapshot
    /// that walks through model changes and their sync result as they are made to the project.
    /// By keeping both the db and snapshot in the repo we can observe and verify
    /// the effects of any data changes over time (whether from serialization, new fields etc.)
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    public async Task LiveSena3Sync()
    {
        // arrange - put "live" crdt db and fw-headless snapshot in place
        // we just ignore the crdt db that was created by the fixture and
        // put in a copy of the "live" db in the same directory
        var liveCrdtProject = new CrdtProject("sena-3-live",
        Path.Join(Path.GetDirectoryName(_project.CrdtProject.DbPath), "sena-3-live.sqlite"));
        File.Copy(
            RelativePath("sena-3-live.verified.sqlite"),
            liveCrdtProject.DbPath,
            overwrite: true);
        File.Copy(
            RelativePath("sena-3-live_snapshot.verified.txt"),
            CrdtFwdataProjectSyncService.SnapshotPath(_project.FwDataProject),
            overwrite: true);
        await using var liveScope = _project.Services.CreateAsyncScope();
        var liveCrdtApi = await liveScope.ServiceProvider.OpenCrdtProject(liveCrdtProject);

        // The default fonts used for writing systems in our Sena 3 project differ when opened on
        // Windows versus Linux. So, we standardize them to Charis SIL (which is the default on Linux).
        // Otherwise, the snapshot verification isn't consistent.
        await PatchAllWsFontsWithCharisSIL(_fwDataApi);

        // act
        var result = await _syncService.Sync(liveCrdtApi, _fwDataApi);
        await _syncService.RegenerateProjectSnapshot(liveCrdtApi, _fwDataApi.Project);

        // assert
        var fwHeadlessSnapshot = await _syncService.GetProjectSnapshot(_project.FwDataProject);
        Exception? verifyException = null;
        var throwAnyVerifyException = () => { if (verifyException is not null) throw verifyException; };
        try
        {
            await Verify(JsonSerializer.Serialize(fwHeadlessSnapshot, IndentedDefaultJsonOptions))
            .UseStrictJson()
            .UseFileName("sena-3-live_snapshot");
        }
        catch (Exception ex)
        {
            verifyException = ex;
        }

        if (result.CrdtChanges > 0)
        {
            // copy the updated "live" crdt db to a file for inspection,
            // so the developer doesn't need to go find it and can decide if the changes are acceptable.
            var dbContext = await liveScope.ServiceProvider.GetRequiredService<IDbContextFactory<LcmCrdtDbContext>>().CreateDbContextAsync();
            BackupDatabase(dbContext, RelativePath("sena-3-live.received.sqlite"));
        }

        // updating the db and snapshot should always be done atomically
        using (new AssertionScope())
        {
            result.CrdtChanges.Should().Be(0, "otherwise the live crdt db has changed and needs developer approval");
            throwAnyVerifyException.Should().NotThrow("otherwise the fw-headless snapshot has changed and needs developer approval");
        }

        result.FwdataChanges.Should().Be(0);
    }

    private async Task PatchAllWsFontsWithCharisSIL(FwDataMiniLcmApi fwDataApi)
    {
        var writingSystems = await fwDataApi.GetWritingSystems();

        // Patch all Analysis writing systems
        foreach (var ws in writingSystems.Analysis)
        {
            await fwDataApi.UpdateWritingSystem(ws.WsId, WritingSystemType.Analysis,
                new UpdateObjectInput<WritingSystem>().Set(w => w.Font, "Charis SIL"));
        }

        // Patch all Vernacular writing systems
        foreach (var ws in writingSystems.Vernacular)
        {
            await fwDataApi.UpdateWritingSystem(ws.WsId, WritingSystemType.Vernacular,
                new UpdateObjectInput<WritingSystem>().Set(w => w.Font, "Charis SIL"));
        }
    }

    private static string RelativePath(string name, [CallerFilePath] string sourceFile = "")
    {
        return Path.Combine(
            Path.GetDirectoryName(sourceFile) ??
            throw new InvalidOperationException("Could not get directory of source file"),
            name);
    }

    private static void BackupDatabase(DbContext sourceContext, string destinationPath)
    {
        var source = (SqliteConnection)sourceContext.Database.GetDbConnection();
        if (source.State != System.Data.ConnectionState.Open)
            source.Open();

        using var destination = new SqliteConnection($"Data Source={destinationPath}");
        destination.Open();

        source.BackupDatabase(destination);
        source.Close();
    }
}
