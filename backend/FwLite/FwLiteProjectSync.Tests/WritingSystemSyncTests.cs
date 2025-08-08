using FwLiteProjectSync.Tests.Fixtures;
using MiniLcm.Models;

namespace FwLiteProjectSync.Tests;

public class WritingSystemSyncTests : IClassFixture<SyncFixture>, IAsyncLifetime
{

    private readonly SyncFixture _fixture;
    private readonly CrdtFwdataProjectSyncService _syncService;

    public WritingSystemSyncTests(SyncFixture fixture)
    {
        _fixture = fixture;
        _syncService = _fixture.SyncService;
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
        await _syncService.Sync(_fixture.CrdtApi, _fixture.FwDataApi);
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
        var en = await _fixture.FwDataApi.GetWritingSystem("en", WritingSystemType.Vernacular);
        en.Should().NotBeNull();
        en.Order.Should().Be(0);
        var fr = await _fixture.FwDataApi.GetWritingSystem("fr", WritingSystemType.Vernacular);
        fr.Should().NotBeNull();
        fr.Order.Should().Be(1);
        await _fixture.FwDataApi.MoveWritingSystem("fr", WritingSystemType.Vernacular, new(null, "en"));
        fr = await _fixture.FwDataApi.GetWritingSystem("fr", WritingSystemType.Vernacular);
        fr.Should().NotBeNull();
        fr.Order.Should().Be(0);
        var crdtFr = await _fixture.CrdtApi.GetWritingSystem("fr", WritingSystemType.Vernacular);
        crdtFr.Should().NotBeNull();
        crdtFr.Order.Should().Be(1);

        await _syncService.Sync(_fixture.CrdtApi, _fixture.FwDataApi);

        fr = await _fixture.FwDataApi.GetWritingSystem("fr", WritingSystemType.Vernacular);
        fr.Should().NotBeNull();
        fr.Order.Should().Be(0);

        var crdtEn = await _fixture.CrdtApi.GetWritingSystem("en", WritingSystemType.Vernacular);
        crdtEn.Should().NotBeNull();
        var actualFr = await _fixture.CrdtApi.GetWritingSystem("fr", WritingSystemType.Vernacular);
        actualFr.Should().NotBeNull();
        actualFr.Order.Should().BeLessThan(crdtEn.Order);
    }
}
