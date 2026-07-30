using FwDataMiniLcmBridge.Api;
using FwLiteProjectSync.Tests.Fixtures;
using LcmCrdt;
using MiniLcm;
using MiniLcm.Models;

namespace FwLiteProjectSync.Tests;

/// <summary>
/// What a stale merge base does to real data, so the cost of the invariant in
/// docs/sync-atomicity/README.md is written down rather than argued about. Each scenario runs twice: once with
/// the merge base an interrupted sync leaves behind, once with the base the same sync would have recorded had it
/// finished. The difference between the two runs is the bug.
///
/// Where the stale run loses data, these tests assert that on purpose. That is not a bug in the diff engine and
/// "fixing" the diff is not the answer: the answer is that a stale base can't be produced
/// (SyncWorkerInterruptionTests, SyncStagingTests) and that an existing one is recognisable
/// (MergeBaseHealthServiceTests). Where the stale run turns out to be harmless, that is pinned too, since the
/// investigation predicted otherwise.
/// </summary>
[Trait("Category", "Integration")]
public class StaleMergeBaseDamageTests(SyncFixture fixture) : IClassFixture<SyncFixture>, IAsyncLifetime
{
    private readonly SyncFixture _fixture = fixture;
    private CrdtMiniLcmApi CrdtApi => _fixture.CrdtApi;
    private FwDataMiniLcmApi FwDataApi => _fixture.FwDataApi;
    private CrdtFwdataProjectSyncService SyncService => _fixture.SyncService;

    public async Task InitializeAsync()
    {
        _fixture.DeleteSyncSnapshot();
        await SyncService.Import(CrdtApi, FwDataApi);
        await _fixture.RegenerateAndGetSnapshot();
    }

    public async Task DisposeAsync()
    {
        await foreach (var entry in FwDataApi.GetAllEntries())
        {
            await FwDataApi.DeleteEntry(entry.Id);
        }
        foreach (var entry in await CrdtApi.GetAllEntries().ToArrayAsync())
        {
            await CrdtApi.DeleteEntry(entry.Id);
        }
        _fixture.DeleteSyncSnapshot();
    }

    private async Task<Entry> CreateFwDataEntry(string lexemeForm, bool withSense = false)
    {
        return await FwDataApi.CreateEntry(new Entry
        {
            Id = Guid.NewGuid(),
            LexemeForm = { { "en", lexemeForm } },
            Senses = withSense ? [new Sense { Id = Guid.NewGuid(), Gloss = { { "en", lexemeForm } } }] : []
        });
    }

    /// <summary>
    /// Runs a sync and returns the merge base an uninterrupted run would have recorded afterwards, without writing
    /// it. Tests choose whether to adopt it (the finished sync) or keep the old one (the interrupted sync).
    /// </summary>
    private async Task<ProjectSnapshot> SyncAndTakeNewMergeBase(ProjectSnapshot mergeBase)
    {
        await SyncService.Sync(CrdtApi, FwDataApi, mergeBase);
        return await CrdtApi.TakeProjectSnapshot();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RefiningAComponentInFlexSurvivesAStaleMergeBase(bool syncRecordedItsWork)
    {
        // This was predicted to lose the refinement, and does not. The diff keys
        // components on (complexForm, component, componentSense), so an entry-level and a sense-level link between the
        // same pair are different keys and the second sync should add the new one without removing the old. It does
        // emit that add, but the CRDT folds it into the link it already holds instead of keeping both, so the
        // refinement survives. Pinned here because it's the scenario the investigation was built around: if this
        // starts failing, the prediction has come true.
        var mergeBase = await _fixture.RegenerateAndGetSnapshot();
        var component = await CreateFwDataEntry("Apple", withSense: true);
        var complexForm = await CreateFwDataEntry("Pineapple");
        var entryLevelLink = ComplexFormComponent.FromEntries(complexForm, component);
        await FwDataApi.CreateComplexFormComponent(entryLevelLink);

        // The sync that copies the entry-level link into the CRDT, then dies before recording what it did.
        var newMergeBase = await SyncAndTakeNewMergeBase(mergeBase);
        if (syncRecordedItsWork) mergeBase = newMergeBase;

        // In FLEx the user makes the link more specific: entry-level out, sense-level in.
        await FwDataApi.DeleteComplexFormComponent(entryLevelLink);
        var senseLevelLink = ComplexFormComponent.FromEntries(complexForm, component, component.Senses[0].Id);
        await FwDataApi.CreateComplexFormComponent(senseLevelLink);

        await SyncService.Sync(CrdtApi, FwDataApi, mergeBase);

        var fwDataComponents = (await FwDataApi.GetEntry(complexForm.Id))!.Components;
        var crdtComponents = (await CrdtApi.GetEntry(complexForm.Id))!.Components;
        crdtComponents.Should().ContainSingle().Which.ComponentSenseId.Should().Be(component.Senses[0].Id);
        fwDataComponents.Should().ContainSingle().Which.ComponentSenseId.Should().Be(component.Senses[0].Id,
            "the user's refinement is what both stores end up with, stale merge base or not");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DeletingAComponentInFlexSurvivesAStaleMergeBase(bool syncRecordedItsWork)
    {
        // Deleting the component alone, unlike deleting the whole entry, is not undone. Both entries still exist in
        // fwdata and are missing from the stale base, so the first pass re-creates them in the CRDT from fwdata's
        // state, and an entry re-created with no components loses its component. fwdata wins, and the second pass has
        // nothing left to push. Pinned because it bounds the blast radius of a stale base: it is only "CRDT wins" for
        // objects the first pass has no reason to write.
        var mergeBase = await _fixture.RegenerateAndGetSnapshot();
        var component = await CreateFwDataEntry("Apple", withSense: true);
        var complexForm = await CreateFwDataEntry("Pineapple");
        var link = ComplexFormComponent.FromEntries(complexForm, component);
        await FwDataApi.CreateComplexFormComponent(link);

        // The sync that copies the link into the CRDT, then dies before recording what it did.
        var newMergeBase = await SyncAndTakeNewMergeBase(mergeBase);
        if (syncRecordedItsWork) mergeBase = newMergeBase;

        await FwDataApi.DeleteComplexFormComponent(link);
        (await FwDataApi.GetEntry(complexForm.Id))!.Components.Should().BeEmpty();

        var result = await SyncService.Sync(CrdtApi, FwDataApi, mergeBase);

        (await FwDataApi.GetEntry(complexForm.Id))!.Components.Should().BeEmpty("the user's deletion stands");
        (await CrdtApi.GetEntry(complexForm.Id))!.Components.Should().BeEmpty();
        result.FwdataChanges.Should().Be(0);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RenamingAPartOfSpeechInFlexIsRevertedWhenTheMergeBaseIsStale(bool syncRecordedItsWork)
    {
        // Observed in production: the only real CRDT-to-fwdata write on the project that prompted this touched two
        // objects, and both were FieldWorks renames reverted to names the CRDT had been holding for a year. Unlike an
        // entry, a part of speech missing from the merge base does not get overwritten from fwdata by the first pass,
        // so the CRDT's stale name survives into the second pass and wins.
        var mergeBase = await _fixture.RegenerateAndGetSnapshot();
        var partOfSpeech = await FwDataApi.CreatePartOfSpeech(new PartOfSpeech
        {
            Id = Guid.NewGuid(),
            Name = new MultiString { { "en", "Verb - reflexive" } },
            Predefined = false
        });

        // The sync that copies the category into the CRDT, then dies before recording what it did.
        var newMergeBase = await SyncAndTakeNewMergeBase(mergeBase);
        if (syncRecordedItsWork) mergeBase = newMergeBase;

        var renamed = partOfSpeech.Copy();
        renamed.Name["en"] = "Verb-reflexive";
        await FwDataApi.UpdatePartOfSpeech(partOfSpeech, renamed);

        await SyncService.Sync(CrdtApi, FwDataApi, mergeBase);

        var fwDataName = (await FwDataApi.GetPartsOfSpeech().ToArrayAsync())
            .Single(pos => pos.Id == partOfSpeech.Id).Name["en"];
        if (syncRecordedItsWork)
        {
            fwDataName.Should().Be("Verb-reflexive", "the rename propagates from fwdata to the CRDT");
        }
        else
        {
            fwDataName.Should().Be("Verb - reflexive",
                "the merge base has no record of the category at all, so the first pass leaves the CRDT's old name alone and the second pass writes it back over the rename");
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DeletingAnEntryInFlexIsUndoneWhenTheMergeBaseIsStale(bool syncRecordedItsWork)
    {
        var mergeBase = await _fixture.RegenerateAndGetSnapshot();
        var entry = await CreateFwDataEntry("Banana");

        // The sync that copies the entry into the CRDT, then dies before recording what it did.
        var newMergeBase = await SyncAndTakeNewMergeBase(mergeBase);
        if (syncRecordedItsWork) mergeBase = newMergeBase;

        await FwDataApi.DeleteEntry(entry.Id);

        await SyncService.Sync(CrdtApi, FwDataApi, mergeBase);

        if (syncRecordedItsWork)
        {
            (await CrdtApi.GetEntry(entry.Id)).Should().BeNull();
            (await FwDataApi.GetEntry(entry.Id)).Should().BeNull("the deletion propagates from fwdata to the CRDT");
        }
        else
        {
            (await CrdtApi.GetEntry(entry.Id)).Should().NotBeNull(
                "the sync only deletes what the merge base says existed, and this base never mentioned the entry");
            (await FwDataApi.GetEntry(entry.Id)).Should().NotBeNull(
                "so the entry is pushed back into fwdata and the user's deletion is undone");
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ClearingASenseFieldInFlexIsUndoneWhenTheMergeBaseIsStale(bool syncRecordedItsWork)
    {
        var mergeBase = await _fixture.RegenerateAndGetSnapshot();
        var partOfSpeech = await FwDataApi.CreatePartOfSpeech(new PartOfSpeech
        {
            Id = Guid.NewGuid(),
            Name = new MultiString { { "en", "Noun" } },
            Predefined = false
        });
        var entry = await CreateFwDataEntry("Cherry", withSense: true);
        var sense = entry.Senses[0];
        var senseWithPartOfSpeech = sense.Copy();
        senseWithPartOfSpeech.PartOfSpeechId = partOfSpeech.Id;
        await FwDataApi.UpdateSense(entry.Id, sense, senseWithPartOfSpeech);

        // The sync that copies the part of speech into the CRDT, then dies before recording what it did.
        var newMergeBase = await SyncAndTakeNewMergeBase(mergeBase);
        if (syncRecordedItsWork) mergeBase = newMergeBase;

        var withPartOfSpeech = (await FwDataApi.GetEntry(entry.Id))!.Senses[0];
        withPartOfSpeech.PartOfSpeechId.Should().Be(partOfSpeech.Id);
        var senseCleared = withPartOfSpeech.Copy();
        senseCleared.PartOfSpeechId = null;
        await FwDataApi.UpdateSense(entry.Id, withPartOfSpeech, senseCleared);

        await SyncService.Sync(CrdtApi, FwDataApi, mergeBase);

        var fwDataPartOfSpeech = (await FwDataApi.GetEntry(entry.Id))!.Senses[0].PartOfSpeechId;
        if (syncRecordedItsWork)
        {
            fwDataPartOfSpeech.Should().BeNull("clearing a field in FLEx propagates to the CRDT");
            (await CrdtApi.GetEntry(entry.Id))!.Senses[0].PartOfSpeechId.Should().BeNull();
        }
        else
        {
            fwDataPartOfSpeech.Should().Be(partOfSpeech.Id,
                "the merge base doesn't know the CRDT ever got a part of speech, so the CRDT's value refills the field the user cleared");
        }
    }
}
