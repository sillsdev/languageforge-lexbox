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
        _fixture.DeleteSyncSnapshot();
        await _fixture.FwDataApi.CreateEntry(new Entry()
        {
            Id = Guid.NewGuid(),
            LexemeForm = { { "en", "Pineapple" } },
        });
        // the fixture is shared between tests, so only create "fr" once
        if (await _fixture.FwDataApi.GetWritingSystem("fr", WritingSystemType.Vernacular) is null)
        {
            await _fixture.FwDataApi.CreateWritingSystem(new WritingSystem()
            {
                Id = Guid.NewGuid(),
                Type = WritingSystemType.Vernacular,
                WsId = new WritingSystemId("fr"),
                Name = "French",
                Abbreviation = "fr",
                Font = "Arial"
            });
        }
        await _syncService.Import(_fixture.CrdtApi, _fixture.FwDataApi);
        await _snapshotService.RegenerateProjectSnapshot(_fixture.CrdtApi, _fixture.FwDataApi.Project, keepBackup: false);
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

        // the fixture is shared, so undo the abbreviation edits made by the shared-WS tests below
        await ResetAbbreviations(_fixture.FwDataApi);
        await ResetAbbreviations(_fixture.CrdtApi);
    }

    private static readonly Dictionary<string, string> OriginalAbbreviations = new() { ["en"] = "Eng", ["fr"] = "fr" };

    private static async Task ResetAbbreviations(IMiniLcmApi api)
    {
        foreach (var (wsId, abbreviation) in OriginalAbbreviations)
        {
            foreach (var type in new[] { WritingSystemType.Vernacular, WritingSystemType.Analysis })
            {
                var ws = await api.GetWritingSystem(wsId, type);
                if (ws is null || ws.Abbreviation == abbreviation) continue;
                var after = ws.Copy();
                after.Abbreviation = abbreviation;
                await api.UpdateWritingSystem(ws, after);
            }
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

    // In FLEx a writing system code has ONE definition shared by the vernacular and analysis lists,
    // while the CRDT stores a separate row per (code, type). The tests below assert that after a single
    // sync both sides agree on every row of a code that is in both lists. They currently fail because
    // FwDataMiniLcmApi writes the shared definition from one row and the other CRDT row is left behind.

    [Fact]
    public async Task SyncWs_EditingAnalysisAbbreviation_KeepsSharedWsConsistent()
    {
        // "en" is both a vernacular and an analysis WS in the fixture project
        var crdtAnalysisEn = await _fixture.CrdtApi.GetWritingSystem("en", WritingSystemType.Analysis);
        crdtAnalysisEn.Should().NotBeNull();

        // act: FwLite user edits only the analysis "en"
        var after = crdtAnalysisEn.Copy();
        after.Abbreviation = "Ana";
        await _fixture.CrdtApi.UpdateWritingSystem(crdtAnalysisEn, after);
        await _syncService.Sync(_fixture.CrdtApi, _fixture.FwDataApi, await GetSnapshot());

        // assert: the edit reached FLEx, and crdt and fwdata agree on both "en" rows
        var fwAnalysisEn = await _fixture.FwDataApi.GetWritingSystem("en", WritingSystemType.Analysis);
        fwAnalysisEn!.Abbreviation.Should().Be("Ana");
        await AssertWritingSystemsInSync("en");
    }

    [Fact]
    public async Task SyncWs_ConflictingEditsOnSharedWs_KeepsSharedWsConsistent()
    {
        var crdtAnalysisEn = await _fixture.CrdtApi.GetWritingSystem("en", WritingSystemType.Analysis);
        var crdtVernacularEn = await _fixture.CrdtApi.GetWritingSystem("en", WritingSystemType.Vernacular);

        // act: FwLite user edits both rows with different values
        var vernacularAfter = crdtVernacularEn!.Copy();
        vernacularAfter.Abbreviation = "Ver";
        await _fixture.CrdtApi.UpdateWritingSystem(crdtVernacularEn, vernacularAfter);
        var analysisAfter = crdtAnalysisEn!.Copy();
        analysisAfter.Abbreviation = "Ana";
        await _fixture.CrdtApi.UpdateWritingSystem(crdtAnalysisEn, analysisAfter);
        await _syncService.Sync(_fixture.CrdtApi, _fixture.FwDataApi, await GetSnapshot());

        // assert: whichever value wins, crdt and fwdata must agree on both "en" rows after one sync
        await AssertWritingSystemsInSync("en");
    }

    [Fact]
    public async Task SyncWs_CreatingAnalysisWsThatExistsAsVernacular_KeepsSharedWsConsistent()
    {
        // "fr" exists only as a vernacular WS (abbreviation "fr")
        var fwVernacularFr = await _fixture.FwDataApi.GetWritingSystem("fr", WritingSystemType.Vernacular);
        fwVernacularFr!.Abbreviation.Should().Be("fr");

        // act: FwLite user adds "fr" as an analysis WS with a different abbreviation
        await _fixture.CrdtApi.CreateWritingSystem(new WritingSystem
        {
            Id = Guid.NewGuid(),
            Type = WritingSystemType.Analysis,
            WsId = "fr",
            Name = "French",
            Abbreviation = "FRA",
            Font = "Arial"
        });
        await _syncService.Sync(_fixture.CrdtApi, _fixture.FwDataApi, await GetSnapshot());

        // assert: the analysis WS exists in FLEx, and crdt and fwdata agree on both "fr" rows
        var fwAnalysisFr = await _fixture.FwDataApi.GetWritingSystem("fr", WritingSystemType.Analysis);
        fwAnalysisFr.Should().NotBeNull();
        await AssertWritingSystemsInSync("fr");
    }

    private async Task AssertWritingSystemsInSync(WritingSystemId wsId)
    {
        foreach (var type in new[] { WritingSystemType.Vernacular, WritingSystemType.Analysis })
        {
            var fwWs = await _fixture.FwDataApi.GetWritingSystem(wsId, type);
            var crdtWs = await _fixture.CrdtApi.GetWritingSystem(wsId, type);
            fwWs.Should().NotBeNull();
            crdtWs.Should().NotBeNull();
            crdtWs.Abbreviation.Should().Be(fwWs.Abbreviation, $"{type} {wsId} abbreviation should match fwdata after sync");
            crdtWs.Font.Should().Be(fwWs.Font, $"{type} {wsId} font should match fwdata after sync");
        }
    }

    private async Task<ProjectSnapshot> GetSnapshot()
    {
        return await _snapshotService.GetProjectSnapshot(_fixture.FwDataApi.Project)
            ?? throw new InvalidOperationException("Expected snapshot to exist");
    }
}
