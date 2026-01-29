using FwLiteProjectSync.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using MiniLcm.Models;
using MiniLcm;

namespace FwLiteProjectSync.Tests;

public class WritingSystemSyncTests : IClassFixture<SyncFixture>, IAsyncLifetime
{

    private readonly SyncFixture _fixture;
    private readonly CrdtFwdataProjectSyncService _syncService;
    private readonly ProjectSnapshotService _snapshotService;

    public WritingSystemSyncTests(SyncFixture fixture)
    {
        _fixture = fixture;
        _syncService = _fixture.SyncService;
        _snapshotService = _fixture.Services.GetRequiredService<ProjectSnapshotService>();
    }

    public async Task InitializeAsync()
    {
        await _fixture.FwDataApi.CreateEntry(new Entry()
        {
            Id = Guid.NewGuid(),
            LexemeForm = { { "en", "Pineapple" } },
        });
        await _fixture.FwDataApi.CreateWritingSystem(new WritingSystem()
        {
            Id = Guid.NewGuid(),
            Type = WritingSystemType.Vernacular,
            WsId = new WritingSystemId("fr"),
            Name = "French",
            Abbreviation = "fr",
            Font = "Arial"
        });
        await _syncService.Import(_fixture.CrdtApi, _fixture.FwDataApi);
    }

    public async Task DisposeAsync()
    {
        await foreach (var entry in _fixture.FwDataApi.GetAllEntries())
        {
            await _fixture.FwDataApi.DeleteEntry(entry.Id);
        }

        foreach (var entry in await _fixture.CrdtApi.GetAllEntries().ToArrayAsync())
        {
            await _fixture.CrdtApi.DeleteEntry(entry.Id);
        }
    }

    [Fact]
    public async Task SyncWs_UpdatesOrder()
    {
        var fwVernacularWSs = (await _fixture.FwDataApi.GetWritingSystems())!.Vernacular;
        var crdtVernacularWSs = (await _fixture.CrdtApi.GetWritingSystems())!.Vernacular;
        fwVernacularWSs.Should().HaveCount(2);
        crdtVernacularWSs.Should().HaveCount(2);
        fwVernacularWSs.Should().BeEquivalentTo(crdtVernacularWSs, options => options.WithStrictOrdering().Excluding(ws => ws.Order));
        var en = await _fixture.CrdtApi.GetWritingSystem("en", WritingSystemType.Vernacular);
        var fr = await _fixture.CrdtApi.GetWritingSystem("fr", WritingSystemType.Vernacular);
        en.Should().NotBeNull();
        fr.Should().NotBeNull();
        fwVernacularWSs.Should().BeEquivalentTo([en, fr], options => options.WithStrictOrdering().Excluding(ws => ws.Order));

        // act - move fr before en
        await _fixture.FwDataApi.MoveWritingSystem("fr", WritingSystemType.Vernacular, new(null, "en"));
        var updatedFwVernacularWSs = (await _fixture.FwDataApi.GetWritingSystems())!.Vernacular;
        updatedFwVernacularWSs.Should().BeEquivalentTo([fr, en], options => options.WithStrictOrdering().Excluding(ws => ws.Order));
        var projectSnapshot = await GetSnapshot();
        await _syncService.Sync(_fixture.CrdtApi, _fixture.FwDataApi, projectSnapshot);

        // assert
        var updatedCrdtVernacularWSs = (await _fixture.CrdtApi.GetWritingSystems())!.Vernacular;
        updatedCrdtVernacularWSs.Should().BeEquivalentTo(updatedFwVernacularWSs, options => options.WithStrictOrdering().Excluding(ws => ws.Order));
    }

    private async Task<ProjectSnapshot> GetSnapshot()
    {
        return await _snapshotService.GetProjectSnapshot(_fixture.FwDataApi.Project)
            ?? throw new InvalidOperationException("Expected snapshot to exist");
    }
}
