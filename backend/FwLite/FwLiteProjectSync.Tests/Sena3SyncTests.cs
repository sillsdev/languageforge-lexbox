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


    public Sena3SyncTests(Sena3Fixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        (_crdtApi, _fwDataApi, var services, _cleanup) = await _fixture.SetupProjects();
        _syncService = services.GetRequiredService<CrdtFwdataProjectSyncService>();
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
                        .For(e => e.ComplexForms).Exclude(c => c.Id),
                    $"CRDT entry {crdtEntry.Id} was synced with FwData");
            }
        }
    }

    //by default the first sync is an import, this will skip that so that the sync will actually sync data
    private async Task BypassImport()
    {
        await _syncService.SaveProjectSnapshot(_fwDataApi.Project, CrdtFwdataProjectSyncService.ProjectSnapshot.Empty);
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

    [Fact(Skip = "this test is waiting for syncing ComplexFormTypes and WritingSystems")]
    public async Task DryRunSync_MakesTheSameChangesAsImport()
    {
        await BypassImport();
        var dryRunSyncResult = await _syncService.SyncDryRun(_crdtApi, _fwDataApi);
        var syncResult = await _syncService.Sync(_crdtApi, _fwDataApi);
        dryRunSyncResult.Should().BeEquivalentTo(syncResult);
    }

    [Fact]
    public async Task FirstSena3SyncJustDoesAnSync()
    {
        _fwDataApi.EntryCount.Should().BeGreaterThan(1000,
            "projects with less than 1000 entries don't trip over the default query limit");

        var results = await _syncService.Sync(_crdtApi, _fwDataApi);
        results.FwdataChanges.Should().Be(0);
        results.CrdtChanges.Should().BeGreaterThanOrEqualTo(_fwDataApi.EntryCount);

        var crdtEntries = await _crdtApi.GetEntries().ToDictionaryAsync(e => e.Id);
        var fwdataEntries = await _fwDataApi.GetEntries().ToDictionaryAsync(e => e.Id);
        fwdataEntries.Count.Should().Be(_fwDataApi.EntryCount);
        ShouldAllBeEquivalentTo(crdtEntries, fwdataEntries);
    }

    [Fact]
    public async Task SyncWithoutImport_CrdtShouldMatchFwdata()
    {
        await BypassImport();

        var results = await _syncService.Sync(_crdtApi, _fwDataApi);
        results.FwdataChanges.Should().Be(0);
        results.CrdtChanges.Should().BeGreaterThan(_fwDataApi.EntryCount);

        var crdtEntries = await _crdtApi.GetEntries().ToDictionaryAsync(e => e.Id);
        var fwdataEntries = await _fwDataApi.GetEntries().ToDictionaryAsync(e => e.Id);
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
