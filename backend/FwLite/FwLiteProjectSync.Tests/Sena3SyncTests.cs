using FluentAssertions.Equivalency;
using FluentAssertions.Execution;
using FwDataMiniLcmBridge.Api;
using FwLiteProjectSync.Tests.Fixtures;
using LcmCrdt;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MiniLcm;
using MiniLcm.Models;
using SystemTextJsonPatch;

namespace FwLiteProjectSync.Tests;

public class Sena3SyncTests : IClassFixture<Sena3Fixture>, IAsyncLifetime
{
    private readonly Sena3Fixture _fixture;
    private CrdtFwdataProjectSyncService _syncService = null!;
    private CrdtMiniLcmApi _crdtApi = null!;
    private FwDataMiniLcmApi _fwDataApi = null!;
    private IDisposable? _cleanup;
    private MiniLcmImport _miniLcmImport = null!;


    public Sena3SyncTests(Sena3Fixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        (_crdtApi, _fwDataApi, var services, _cleanup) = await _fixture.SetupProjects();
        _syncService = services.GetRequiredService<CrdtFwdataProjectSyncService>();
        _miniLcmImport = services.GetRequiredService<MiniLcmImport>();
        _fwDataApi.EntryCount.Should().BeGreaterThan(100, "project should be loaded and have entries");
    }

    public Task DisposeAsync()
    {
        _cleanup?.Dispose();
        return Task.CompletedTask;
    }

    private void ShouldAllBeEquivalentTo(Dictionary<Guid, Entry> crdtEntries, Dictionary<Guid, Entry> fwdataEntries)
    {
        crdtEntries.Keys.Should().BeEquivalentTo(fwdataEntries.Keys);
        using (new AssertionScope())
        {
            foreach (var crdtEntry in crdtEntries.Values)
            {
                var fwdataEntry = fwdataEntries[crdtEntry.Id];
                crdtEntry.Should().BeEquivalentTo(fwdataEntry,
                    options => options
                        .For(e => e.Components).Exclude(c => c.Id)
                        .For(e => e.ComplexForms).Exclude(c => c.Id)
                        .For(e => e.Senses).Exclude(s => s.Order),
                    $"CRDT entry {crdtEntry.Id} was synced with FwData");
            }
        }
    }

    //by default the first sync is an import, this will skip that so that the sync will actually sync data
    private async Task BypassImport(bool wsImported = false)
    {
        var snapshot = CrdtFwdataProjectSyncService.ProjectSnapshot.Empty;
        if (wsImported) snapshot = snapshot with { WritingSystems = await _fwDataApi.GetWritingSystems() };
        await _syncService.SaveProjectSnapshot(_fwDataApi.Project, snapshot);
    }

    //this lets us query entries when there is no writing system
    private async Task WorkaroundMissingWritingSystems()
    {
        //must have at least one writing system to query for entries
        await _crdtApi.CreateWritingSystem(WritingSystemType.Vernacular, (await _fwDataApi.GetWritingSystems()).Vernacular.First());

    }

    [Fact]
    public async Task DryRunImport_MakesNoChanges()
    {
        await WorkaroundMissingWritingSystems();
        _crdtApi.GetEntries().ToBlockingEnumerable().Should().BeEmpty();
        await _syncService.SyncDryRun(_crdtApi, _fwDataApi);
        //should still be empty
        _crdtApi.GetEntries().ToBlockingEnumerable().Should().BeEmpty();
    }

    [Fact]
    public async Task DryRunImport_MakesTheSameChangesAsImport()
    {
        var dryRunSyncResult = await _syncService.SyncDryRun(_crdtApi, _fwDataApi);
        var syncResult = await _syncService.Sync(_crdtApi, _fwDataApi);
        dryRunSyncResult.Should().BeEquivalentTo(syncResult);
    }

    [Fact]
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
    public async Task FirstSena3SyncJustDoesAnSync()
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
    }

    [Fact]
    [Trait("Category", "Slow")]
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
    }

    [Fact]
    public async Task SecondSena3SyncDoesNothing()
    {
        await _syncService.Sync(_crdtApi, _fwDataApi);
        var secondSync = await _syncService.Sync(_crdtApi, _fwDataApi);
        secondSync.CrdtChanges.Should().Be(0);
        secondSync.FwdataChanges.Should().Be(0);
    }
}
