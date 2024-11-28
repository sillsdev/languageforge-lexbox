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

    [Fact]
    public async Task FirstSena3SyncJustDoesAnImport()
    {
        var crdtApi = _fixture.CrdtApi;
        var fwdataApi = _fixture.FwDataApi;
        await _syncService.Sync(crdtApi, fwdataApi);

        var crdtEntries = await crdtApi.GetEntries().ToDictionaryAsync(e => e.Id);
        var fwdataEntries = await fwdataApi.GetEntries().ToDictionaryAsync(e => e.Id);
        crdtEntries.Keys.Should().BeEquivalentTo(fwdataEntries.Keys);
        using (new AssertionScope())
        {
            foreach (var crdtEntry in crdtEntries.Values)
            {
                var fwdataEntry = fwdataEntries[crdtEntry.Id];
                crdtEntry.Should().BeEquivalentTo(fwdataEntry,
                    options => options
                        .For(e => e.Components).Exclude(c => c.Id)
                        .For(e => e.ComplexForms).Exclude(c => c.Id), $"CRDT entry {crdtEntry.Id} was synced with FwData");
            }
        }
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
