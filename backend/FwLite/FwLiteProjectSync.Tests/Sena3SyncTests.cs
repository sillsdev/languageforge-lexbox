using FluentAssertions.Equivalency;
using FluentAssertions.Execution;
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
    private readonly CrdtFwdataProjectSyncService _syncService;

    public async Task InitializeAsync()
    {
    }

    public async Task DisposeAsync()
    {
    }

    public Sena3SyncTests(Sena3Fixture fixture)
    {
        _fixture = fixture;
        _syncService = _fixture.SyncService;
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
                        .For(e => e.ComplexForms).Exclude(c => c.Id),
                    $"CRDT entry {crdtEntry.Id} was synced with FwData");
            }
        }
    }

    //by default the first sync is an import, this will skip that so that the sync will actually sync data
    private async Task BypassImport()
    {
        await _syncService.SaveProjectSnapshot(_fixture.FwDataApi.Project, new ([], [], []));
    }

    //this lets us query entries when there is no writing system
    private async Task WorkaroundMissingWritingSystems()
    {
        //must have at least one writing system to query for entries
        await _fixture.CrdtApi.CreateWritingSystem(WritingSystemType.Vernacular, (await _fixture.FwDataApi.GetWritingSystems()).Vernacular.First());

    }

    [Fact]
    public async Task DryRunImport_MakesNoChanges()
    {
        var crdtApi = _fixture.CrdtApi;
        await WorkaroundMissingWritingSystems();
        crdtApi.GetEntries().ToBlockingEnumerable().Should().BeEmpty();
        var fwdataApi = _fixture.FwDataApi;
        await _syncService.SyncDryRun(crdtApi, fwdataApi);
        //should still be empty
        crdtApi.GetEntries().ToBlockingEnumerable().Should().BeEmpty();
    }

    [Fact]
    public async Task DryRunImport_MakesTheSameChangesAsImport()
    {
        var dryRunSyncResult = await _syncService.SyncDryRun(_fixture.CrdtApi, _fixture.FwDataApi);
        var syncResult = await _syncService.Sync(_fixture.CrdtApi, _fixture.FwDataApi);
        dryRunSyncResult.Should().BeEquivalentTo(syncResult);
    }

    [Fact]
    public async Task DryRunSync_MakesNoChanges()
    {
        await BypassImport();
        var crdtApi = _fixture.CrdtApi;
        await WorkaroundMissingWritingSystems();
        crdtApi.GetEntries().ToBlockingEnumerable().Should().BeEmpty();
        var fwdataApi = _fixture.FwDataApi;
        await _syncService.SyncDryRun(crdtApi, fwdataApi);
        //should still be empty
        crdtApi.GetEntries().ToBlockingEnumerable().Should().BeEmpty();
    }

    [Fact(Skip = "this test is waiting for syncing ComplexFormTypes and WritingSystems")]
    public async Task DryRunSync_MakesTheSameChangesAsImport()
    {
        await BypassImport();
        var dryRunSyncResult = await _syncService.SyncDryRun(_fixture.CrdtApi, _fixture.FwDataApi);
        var syncResult = await _syncService.Sync(_fixture.CrdtApi, _fixture.FwDataApi);
        dryRunSyncResult.Should().BeEquivalentTo(syncResult);
    }

    [Fact]
    public async Task FirstSena3SyncJustDoesAnSync()
    {
        var crdtApi = _fixture.CrdtApi;
        var fwdataApi = _fixture.FwDataApi;
        var results = await _syncService.Sync(crdtApi, fwdataApi);
        results.FwdataChanges.Should().Be(0);
        results.CrdtChanges.Should().BeGreaterThan(fwdataApi.EntryCount);

        var crdtEntries = await crdtApi.GetEntries().ToDictionaryAsync(e => e.Id);
        var fwdataEntries = await fwdataApi.GetEntries().ToDictionaryAsync(e => e.Id);
        ShouldAllBeEquivalentTo(crdtEntries, fwdataEntries);
    }

    [Fact(Skip = "this test is waiting for syncing ComplexFormTypes and WritingSystems")]
    public async Task SyncWithoutImport_CrdtShouldMatchFwdata()
    {
        await BypassImport();

        var results = await _syncService.Sync(_fixture.CrdtApi, _fixture.FwDataApi);
        results.FwdataChanges.Should().Be(0);
        results.CrdtChanges.Should().BeGreaterThan(_fixture.FwDataApi.EntryCount);

        var crdtEntries = await _fixture.CrdtApi.GetEntries().ToDictionaryAsync(e => e.Id);
        var fwdataEntries = await _fixture.FwDataApi.GetEntries().ToDictionaryAsync(e => e.Id);
        ShouldAllBeEquivalentTo(crdtEntries, fwdataEntries);
    }

    [Fact]
    public async Task SecondSena3SyncDoesNothing()
    {
        var crdtApi = _fixture.CrdtApi;
        var fwdataApi = _fixture.FwDataApi;
        await _syncService.Sync(crdtApi, fwdataApi);
        var secondSync = await _syncService.Sync(crdtApi, fwdataApi);
        secondSync.CrdtChanges.Should().Be(0);
        secondSync.FwdataChanges.Should().Be(0);
    }
}
