using FwLiteProjectSync.Tests.Fixtures;
using MiniLcm;
using MiniLcm.Models;

namespace FwLiteProjectSync.Tests;

public class VariantSyncTests : IClassFixture<SyncFixture>, IAsyncLifetime
{
    private readonly SyncFixture _fixture;
    private readonly CrdtFwdataProjectSyncService _syncService;

    private readonly Guid _mainEntryId = Guid.NewGuid();
    private readonly Guid _variantEntryId = Guid.NewGuid();
    private Entry _mainEntry = null!;
    private Entry _variantEntry = null!;

    public VariantSyncTests(SyncFixture fixture)
    {
        _fixture = fixture;
        _syncService = _fixture.SyncService;
    }

    public async Task InitializeAsync()
    {
        _fixture.DeleteSyncSnapshot();
        _mainEntry = await _fixture.FwDataApi.CreateEntry(new()
        {
            Id = _mainEntryId,
            LexemeForm = { { "en", "color" } },
            Senses =
            [
                new Sense { Gloss = { { "en", "colour sense" } } }
            ]
        });
        // sense-less on purpose — the FLEx "Insert Variant" shape
        _variantEntry = await _fixture.FwDataApi.CreateEntry(new()
        {
            Id = _variantEntryId,
            LexemeForm = { { "en", "colour" } }
        });
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
        _fixture.DeleteSyncSnapshot();
    }

    private async Task<VariantType> GetVariantType(IMiniLcmApi api, string name)
    {
        return (await api.GetVariantTypes().ToArrayAsync()).Single(t => t.Name["en"] == name);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task VariantTypesSyncFwToCrdtAtProjectImport()
    {
        await _syncService.Import(_fixture.CrdtApi, _fixture.FwDataApi);
        var fwdataVariantTypes = await _fixture.FwDataApi.GetVariantTypes().ToArrayAsync();
        var crdtVariantTypes = await _fixture.CrdtApi.GetVariantTypes().ToArrayAsync();
        fwdataVariantTypes.Should().NotBeEmpty();
        crdtVariantTypes.Should().BeEquivalentTo(fwdataVariantTypes);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task CreatingVariantInFwDataSyncsWithoutIssue()
    {
        var crdtApi = _fixture.CrdtApi;
        var fwdataApi = _fixture.FwDataApi;
        await _syncService.Import(crdtApi, fwdataApi);
        var projectSnapshot = await _fixture.RegenerateAndGetSnapshot();

        var dialectal = await GetVariantType(fwdataApi, "Dialectal Variant");
        await fwdataApi.CreateVariant(Variant.FromEntries(_variantEntry, _mainEntry) with
        {
            Types = [dialectal],
            HideMinorEntry = true,
            Comment = new() { { "en", new RichString("british spelling") } },
        });

        await _syncService.Sync(crdtApi, fwdataApi, projectSnapshot);

        SyncTests.AssertSnapshotsAreEquivalent(await fwdataApi.TakeProjectSnapshot(), await crdtApi.TakeProjectSnapshot());
        var crdtVariantEntry = await crdtApi.GetEntry(_variantEntryId);
        crdtVariantEntry.Should().NotBeNull();
        crdtVariantEntry!.Senses.Should().BeEmpty();
        var link = crdtVariantEntry.VariantOf.Should().ContainSingle().Subject;
        link.Types.Should().ContainSingle(t => t.Id == dialectal.Id);
        link.HideMinorEntry.Should().BeTrue();

        // Sync again, ensure no problems or changes
        var secondSnapshot = await _fixture.RegenerateAndGetSnapshot();
        var secondSync = await _syncService.Sync(crdtApi, fwdataApi, secondSnapshot);
        secondSync.CrdtChanges.Should().Be(0);
        secondSync.FwdataChanges.Should().Be(0);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task CreatingVariantInCrdtSyncsWithoutIssue()
    {
        var crdtApi = _fixture.CrdtApi;
        var fwdataApi = _fixture.FwDataApi;
        await _syncService.Import(crdtApi, fwdataApi);
        var projectSnapshot = await _fixture.RegenerateAndGetSnapshot();

        var spelling = await GetVariantType(crdtApi, "Spelling Variant");
        await crdtApi.CreateVariant(Variant.FromEntries(_variantEntry, _mainEntry) with { Types = [spelling] });

        await _syncService.Sync(crdtApi, fwdataApi, projectSnapshot);

        SyncTests.AssertSnapshotsAreEquivalent(await fwdataApi.TakeProjectSnapshot(), await crdtApi.TakeProjectSnapshot());
        var fwVariantEntry = await fwdataApi.GetEntry(_variantEntryId);
        fwVariantEntry.Should().NotBeNull();
        var link = fwVariantEntry!.VariantOf.Should().ContainSingle().Subject;
        link.Types.Should().ContainSingle(t => t.Id == spelling.Id);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task SenseTargetedVariantWithNewSenseSyncs()
    {
        var crdtApi = _fixture.CrdtApi;
        var fwdataApi = _fixture.FwDataApi;
        await _syncService.Import(crdtApi, fwdataApi);
        var projectSnapshot = await _fixture.RegenerateAndGetSnapshot();

        var newSense = await fwdataApi.CreateSense(_mainEntryId, new Sense { Gloss = { { "en", "new target sense" } } });
        await fwdataApi.CreateVariant(Variant.FromEntries(_variantEntry, _mainEntry, newSense.Id));

        await _syncService.Sync(crdtApi, fwdataApi, projectSnapshot);

        SyncTests.AssertSnapshotsAreEquivalent(await fwdataApi.TakeProjectSnapshot(), await crdtApi.TakeProjectSnapshot());
        var crdtMainEntry = await crdtApi.GetEntry(_mainEntryId);
        crdtMainEntry!.Variants.Should().ContainSingle(v => v.MainSenseId == newSense.Id);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task VariantTypesSyncBothWays()
    {
        var crdtApi = _fixture.CrdtApi;
        var fwdataApi = _fixture.FwDataApi;
        await _syncService.Import(crdtApi, fwdataApi);
        var projectSnapshot = await _fixture.RegenerateAndGetSnapshot();

        var fwType = new VariantType()
        {
            Id = Guid.NewGuid(),
            Name = new() { { "en", "fw custom type" } },
        };
        await fwdataApi.CreateVariantType(fwType);

        var crdtType = new VariantType()
        {
            Id = Guid.NewGuid(),
            Name = new() { { "en", "crdt custom type" } },
        };
        await crdtApi.CreateVariantType(crdtType);

        await _syncService.Sync(crdtApi, fwdataApi, projectSnapshot);

        var crdtVariantTypes = await crdtApi.GetVariantTypes().ToArrayAsync();
        var fwdataVariantTypes = await fwdataApi.GetVariantTypes().ToArrayAsync();
        crdtVariantTypes.Should().ContainEquivalentOf(fwType);
        crdtVariantTypes.Should().ContainEquivalentOf(crdtType);
        fwdataVariantTypes.Should().ContainEquivalentOf(fwType);
        fwdataVariantTypes.Should().ContainEquivalentOf(crdtType);
        crdtVariantTypes.Should().BeEquivalentTo(fwdataVariantTypes);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task VariantLinkEditsSyncBothWays()
    {
        var crdtApi = _fixture.CrdtApi;
        var fwdataApi = _fixture.FwDataApi;
        var free = await GetVariantType(fwdataApi, "Free Variant");
        var created = await fwdataApi.CreateVariant(Variant.FromEntries(_variantEntry, _mainEntry) with { Types = [free] });
        await _syncService.Import(crdtApi, fwdataApi);
        var projectSnapshot = await _fixture.RegenerateAndGetSnapshot();

        // edit the same link's own fields on each side (types in FwData, scalars in CRDT)
        var dialectal = await GetVariantType(fwdataApi, "Dialectal Variant");
        var fwLink = (await fwdataApi.GetEntry(_variantEntryId))!.VariantOf.Single();
        await fwdataApi.AddVariantType(fwLink, dialectal.Id);
        await fwdataApi.RemoveVariantType(fwLink, free.Id);

        var crdtLink = (await crdtApi.GetEntry(_variantEntryId))!.VariantOf.Single();
        var crdtAfter = crdtLink.Copy();
        crdtAfter.HideMinorEntry = true;
        crdtAfter.Comment = new() { { "en", new RichString("prefer the main entry") } };
        await crdtApi.UpdateVariant(crdtLink, crdtAfter);

        await _syncService.Sync(crdtApi, fwdataApi, projectSnapshot);

        SyncTests.AssertSnapshotsAreEquivalent(await fwdataApi.TakeProjectSnapshot(), await crdtApi.TakeProjectSnapshot());
        var merged = (await crdtApi.GetEntry(_variantEntryId))!.VariantOf.Should().ContainSingle().Subject;
        merged.Types.Should().ContainSingle(t => t.Id == dialectal.Id);
        merged.HideMinorEntry.Should().BeTrue();
        merged.Comment["en"].GetPlainText().Should().Be("prefer the main entry");
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task DeletingMainEntryInCrdtRemovesLinkOnSync()
    {
        var crdtApi = _fixture.CrdtApi;
        var fwdataApi = _fixture.FwDataApi;
        await fwdataApi.CreateVariant(Variant.FromEntries(_variantEntry, _mainEntry));
        await _syncService.Import(crdtApi, fwdataApi);
        var projectSnapshot = await _fixture.RegenerateAndGetSnapshot();

        await crdtApi.DeleteEntry(_mainEntryId);

        await _syncService.Sync(crdtApi, fwdataApi, projectSnapshot);

        (await fwdataApi.GetEntry(_mainEntryId)).Should().BeNull();
        var fwVariantEntry = await fwdataApi.GetEntry(_variantEntryId);
        fwVariantEntry.Should().NotBeNull("only the link dies, not the variant entry");
        fwVariantEntry!.VariantOf.Should().BeEmpty();
        (await crdtApi.GetEntry(_variantEntryId))!.VariantOf.Should().BeEmpty();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task DeletingVariantEntryInFwDataRemovesLinkOnSync()
    {
        var crdtApi = _fixture.CrdtApi;
        var fwdataApi = _fixture.FwDataApi;
        await fwdataApi.CreateVariant(Variant.FromEntries(_variantEntry, _mainEntry));
        await _syncService.Import(crdtApi, fwdataApi);
        var projectSnapshot = await _fixture.RegenerateAndGetSnapshot();

        await fwdataApi.DeleteEntry(_variantEntryId);

        await _syncService.Sync(crdtApi, fwdataApi, projectSnapshot);

        (await crdtApi.GetEntry(_variantEntryId)).Should().BeNull();
        var crdtMainEntry = await crdtApi.GetEntry(_mainEntryId);
        crdtMainEntry.Should().NotBeNull("only the link dies, not the main entry");
        crdtMainEntry!.Variants.Should().BeEmpty();
        (await fwdataApi.GetEntry(_mainEntryId))!.Variants.Should().BeEmpty();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task VariantAddedInFwDataWhileMainEntryDeletedInCrdt_SyncDoesNotThrow()
    {
        var crdtApi = _fixture.CrdtApi;
        var fwdataApi = _fixture.FwDataApi;
        await _syncService.Import(crdtApi, fwdataApi);
        var projectSnapshot = await _fixture.RegenerateAndGetSnapshot();

        await fwdataApi.CreateVariant(Variant.FromEntries(_variantEntry, _mainEntry));
        await crdtApi.DeleteEntry(_mainEntryId);

        await _syncService.Sync(crdtApi, fwdataApi, projectSnapshot);

        (await crdtApi.GetEntry(_mainEntryId)).Should().BeNull();
        (await fwdataApi.GetEntry(_mainEntryId)).Should().BeNull();
        var crdtVariantEntry = await crdtApi.GetEntry(_variantEntryId);
        crdtVariantEntry.Should().NotBeNull();
        crdtVariantEntry!.VariantOf.Should().BeEmpty();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task LegacySnapshotWithoutVariantTypes_FirstSyncImportsVariantsIntoCrdt()
    {
        var crdtApi = _fixture.CrdtApi;
        var fwdataApi = _fixture.FwDataApi;
        await _syncService.Import(crdtApi, fwdataApi);
        var projectSnapshot = await _fixture.RegenerateAndGetSnapshot();

        // wipe the variants a CRDT imported before variant support would have missed;
        // pre-variant snapshot json deserializes with an empty VariantTypes list
        // (ProjectSnapshot normalizes the missing property)
        foreach (var entry in await crdtApi.GetAllEntries().ToArrayAsync())
        {
            foreach (var link in entry.VariantOf)
            {
                await crdtApi.DeleteVariant(link);
            }
        }
        var legacySnapshot = projectSnapshot with
        {
            VariantTypes = [],
            Entries = [..projectSnapshot.Entries.Select(e => StripVariants(e.Copy()))]
        };

        var fwdataVariant = await fwdataApi.CreateVariant(Variant.FromEntries(_variantEntry, _mainEntry));

        await _syncService.Sync(crdtApi, fwdataApi, legacySnapshot);

        // FwData kept its link and the CRDT gained it
        (await fwdataApi.GetEntry(_variantEntryId))!.VariantOf.Should().ContainSingle();
        (await crdtApi.GetEntry(_variantEntryId))!.VariantOf.Should().ContainSingle(v => v.MainEntryId == fwdataVariant.MainEntryId);
        (await crdtApi.GetVariantTypes().ToArrayAsync()).Should().NotBeEmpty();

        static Entry StripVariants(Entry entry)
        {
            entry.VariantOf = [];
            entry.Variants = [];
            return entry;
        }
    }
}
