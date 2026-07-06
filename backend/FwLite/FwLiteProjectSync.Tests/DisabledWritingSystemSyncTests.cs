using FwLiteProjectSync.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using MiniLcm;
using MiniLcm.Models;

namespace FwLiteProjectSync.Tests;

public class DisabledWritingSystemSyncTests : IClassFixture<SyncFixture>, IAsyncLifetime
{
    private readonly SyncFixture _fixture;
    private readonly CrdtFwdataProjectSyncService _syncService;
    private readonly ProjectSnapshotService _snapshotService;

    public DisabledWritingSystemSyncTests(SyncFixture fixture)
    {
        _fixture = fixture;
        _syncService = _fixture.SyncService;
        _snapshotService = _fixture.Services.GetRequiredService<ProjectSnapshotService>();
    }

    public async Task InitializeAsync()
    {
        if (!ProjectSnapshotService.HasSyncedSuccessfully(_fixture.FwDataApi.Project))
        {
            await _syncService.Import(_fixture.CrdtApi, _fixture.FwDataApi);
        }
        // the fixture is shared between tests, so each test starts from a snapshot matching the current state
        await _snapshotService.RegenerateProjectSnapshot(_fixture.CrdtApi, _fixture.FwDataApi.Project, keepBackup: false);
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    private async Task<ProjectSnapshot> GetSnapshot()
    {
        return await _snapshotService.GetProjectSnapshot(_fixture.FwDataApi.Project)
            ?? throw new InvalidOperationException("Expected snapshot to exist");
    }

    private static WritingSystem NewVernacularWs(string code, bool isDisabled = false)
    {
        return new WritingSystem
        {
            Id = Guid.NewGuid(),
            Type = WritingSystemType.Vernacular,
            WsId = code,
            Name = code,
            Abbreviation = code,
            Font = "Arial",
            IsDisabled = isDisabled,
        };
    }

    [Fact]
    public async Task SwappingWhichWritingSystemIsEnabled_Syncs()
    {
        // "en" is the only enabled analysis writing system; add a disabled "sw" next to it
        await _fixture.FwDataApi.CreateWritingSystem(NewVernacularWs("sw", isDisabled: true) with { Type = WritingSystemType.Analysis });
        await _syncService.Sync(_fixture.CrdtApi, _fixture.FwDataApi, await GetSnapshot());
        await _snapshotService.RegenerateProjectSnapshot(_fixture.CrdtApi, _fixture.FwDataApi.Project, keepBackup: false);

        // swap in FwData: enable sw, disable en
        // (the sync must not reject the swap just because the disable is diffed before the enable)
        var en = await _fixture.FwDataApi.GetWritingSystem("en", WritingSystemType.Analysis);
        var sw = await _fixture.FwDataApi.GetWritingSystem("sw", WritingSystemType.Analysis);
        var swEnabled = sw!.Copy();
        swEnabled.IsDisabled = false;
        await _fixture.FwDataApi.UpdateWritingSystem(sw, swEnabled);
        var enDisabled = en!.Copy();
        enDisabled.IsDisabled = true;
        await _fixture.FwDataApi.UpdateWritingSystem(en, enDisabled);

        await _syncService.Sync(_fixture.CrdtApi, _fixture.FwDataApi, await GetSnapshot());

        (await _fixture.CrdtApi.GetWritingSystem("en", WritingSystemType.Analysis))!.IsDisabled.Should().BeTrue();
        (await _fixture.CrdtApi.GetWritingSystem("sw", WritingSystemType.Analysis))!.IsDisabled.Should().BeFalse();
    }

    [Fact]
    public async Task DisabledFwDataWritingSystem_SyncsToCrdtAsDisabled()
    {
        // simulates the legacy situation: the snapshot predates disabled writing-system sync,
        // so it doesn't contain the disabled writing system that exists in FwData
        await _fixture.FwDataApi.CreateWritingSystem(NewVernacularWs("de", isDisabled: true));

        await _syncService.Sync(_fixture.CrdtApi, _fixture.FwDataApi, await GetSnapshot());

        var crdtWs = await _fixture.CrdtApi.GetWritingSystem("de", WritingSystemType.Vernacular);
        crdtWs.Should().NotBeNull();
        crdtWs.IsDisabled.Should().BeTrue();
        var vernacular = (await _fixture.CrdtApi.GetWritingSystems()).Vernacular;
        vernacular.Last().WsId.Should().Be((WritingSystemId)"de", "disabled writing systems are sorted last");
        vernacular.First().IsDisabled.Should().BeFalse();
    }

    [Fact]
    public async Task DisablingInFwData_SyncsToCrdt()
    {
        var pt = await _fixture.FwDataApi.CreateWritingSystem(NewVernacularWs("pt"));
        await _syncService.Sync(_fixture.CrdtApi, _fixture.FwDataApi, await GetSnapshot());
        (await _fixture.CrdtApi.GetWritingSystem("pt", WritingSystemType.Vernacular))!.IsDisabled.Should().BeFalse();
        await _snapshotService.RegenerateProjectSnapshot(_fixture.CrdtApi, _fixture.FwDataApi.Project, keepBackup: false);

        var disabled = pt.Copy();
        disabled.IsDisabled = true;
        await _fixture.FwDataApi.UpdateWritingSystem(pt, disabled);
        (await _fixture.FwDataApi.GetWritingSystem("pt", WritingSystemType.Vernacular))!.IsDisabled.Should().BeTrue();

        await _syncService.Sync(_fixture.CrdtApi, _fixture.FwDataApi, await GetSnapshot());

        (await _fixture.CrdtApi.GetWritingSystem("pt", WritingSystemType.Vernacular))!.IsDisabled.Should().BeTrue();
    }

    [Fact]
    public async Task DisablingInCrdt_SyncsToFwData()
    {
        await _fixture.FwDataApi.CreateWritingSystem(NewVernacularWs("it"));
        await _syncService.Sync(_fixture.CrdtApi, _fixture.FwDataApi, await GetSnapshot());
        await _snapshotService.RegenerateProjectSnapshot(_fixture.CrdtApi, _fixture.FwDataApi.Project, keepBackup: false);

        var crdtWs = await _fixture.CrdtApi.GetWritingSystem("it", WritingSystemType.Vernacular);
        crdtWs.Should().NotBeNull();
        var disabled = crdtWs.Copy();
        disabled.IsDisabled = true;
        await _fixture.CrdtApi.UpdateWritingSystem(crdtWs, disabled);

        await _syncService.Sync(_fixture.CrdtApi, _fixture.FwDataApi, await GetSnapshot());

        (await _fixture.FwDataApi.GetWritingSystem("it", WritingSystemType.Vernacular))!.IsDisabled.Should().BeTrue();

        // re-enable and sync back
        await _snapshotService.RegenerateProjectSnapshot(_fixture.CrdtApi, _fixture.FwDataApi.Project, keepBackup: false);
        var reenabled = disabled.Copy();
        reenabled.IsDisabled = false;
        await _fixture.CrdtApi.UpdateWritingSystem(disabled, reenabled);

        await _syncService.Sync(_fixture.CrdtApi, _fixture.FwDataApi, await GetSnapshot());

        (await _fixture.FwDataApi.GetWritingSystem("it", WritingSystemType.Vernacular))!.IsDisabled.Should().BeFalse();
    }

    [Fact]
    public async Task WritingSystemExistingOnBothSidesButMissingFromSnapshot_SyncsWithoutCrash()
    {
        // legacy conflict: the writing system is disabled in FLEx (so old FwLite never synced it)
        // while a user independently created it (enabled) in the CRDT
        await _fixture.FwDataApi.CreateWritingSystem(NewVernacularWs("nl", isDisabled: true));
        await _fixture.CrdtApi.CreateWritingSystem(NewVernacularWs("nl"));

        await _syncService.Sync(_fixture.CrdtApi, _fixture.FwDataApi, await GetSnapshot());

        // fwdata wins because the fwdata->crdt direction runs first
        (await _fixture.CrdtApi.GetWritingSystem("nl", WritingSystemType.Vernacular))!.IsDisabled.Should().BeTrue();
        (await _fixture.FwDataApi.GetWritingSystem("nl", WritingSystemType.Vernacular))!.IsDisabled.Should().BeTrue();
    }
}
