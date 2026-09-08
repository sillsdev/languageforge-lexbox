using FwLiteProjectSync.Tests.Fixtures;
using MiniLcm;
using MiniLcm.Exceptions;
using MiniLcm.Media;
using MiniLcm.Models;
using MiniLcm.SyncHelpers;
using MiniLcm.Tests;

namespace FwLiteProjectSync.Tests;

public class CrdtEntryMoveSyncTests(ExtraWritingSystemsSyncFixture fixture) : EntryMoveSyncTestsBase(fixture)
{
    protected override IMiniLcmApi GetApi(SyncFixture fixture)
    {
        return fixture.CrdtApi;
    }

    // Another CRDT client can delete the move's target before this sync lands. The moved item must then be
    // gone too, not orphaned under a dead parent.

    [Fact]
    public async Task SenseMovedToEntryDeletedInCrdt_IsDeleted()
    {
        var sense = NewSense("moving");
        var sourceEntry = await CreateEntry("source-entry", sense);
        var targetEntry = await CreateEntry("target-entry");
        await Api.DeleteEntry(targetEntry.Id);

        var after = Copy(sourceEntry, targetEntry);
        MoveSense(after, sense.Id, targetEntry.Id);
        await Sync([sourceEntry, targetEntry], after);

        (await Api.GetEntry(targetEntry.Id)).Should().BeNull();
        SenseIds(await GetEntry(sourceEntry.Id)).Should().BeEmpty();
        (await Api.GetSense(targetEntry.Id, sense.Id)).Should().BeNull();
    }

    [Fact]
    public async Task ExampleSentenceMovedToSenseDeletedInCrdt_IsDeleted()
    {
        var example = NewExample("example");
        var sourceSense = NewSense("source", example);
        var targetSense = NewSense("target");
        var entry = await CreateEntry("entry", sourceSense, targetSense);
        await Api.DeleteSense(entry.Id, targetSense.Id);

        var after = Copy(entry);
        MoveExample(after, example.Id, targetSense.Id);
        await Sync([entry], after);

        var actual = await GetEntry(entry.Id);
        SenseIds(actual).Should().Equal(sourceSense.Id);
        ExampleIds(actual.Senses[0]).Should().BeEmpty();
    }
}

public class FwDataEntryMoveSyncTests(ExtraWritingSystemsSyncFixture fixture) : EntryMoveSyncTestsBase(fixture)
{
    protected override IMiniLcmApi GetApi(SyncFixture fixture)
    {
        return fixture.FwDataApi;
    }
}

/// <summary>
/// FLEx re-parents senses and examples guid-intact (drag-and-drop, "Merge Sense into...", merging entries),
/// so the diff between two states must apply them as moves, not as a delete and a create.
/// </summary>
public abstract class EntryMoveSyncTestsBase(ExtraWritingSystemsSyncFixture fixture) : IClassFixture<ExtraWritingSystemsSyncFixture>, IAsyncLifetime
{
    public Task InitializeAsync()
    {
        // Mirror production sync (CrdtFwdataProjectSyncService): validation only, no normalization,
        // because the data is already normalized on both sides.
        Api = TestMiniLcmWrappers.CreateValidationFactory().Create(GetApi(_fixture));
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    protected abstract IMiniLcmApi GetApi(SyncFixture fixture);

    private readonly SyncFixture _fixture = fixture;
    protected IMiniLcmApi Api = null!;

    #region Sense moves

    [Theory]
    // (sourceEntryDeleted, targetEntryCreated, targetEntryFirst); entry order only matters when both entries are updated
    [InlineData(false, false, false)]
    [InlineData(false, false, true)]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(true, true, false)]
    public async Task CanSyncSenseMovedToDifferentEntry(bool sourceEntryDeleted, bool targetEntryCreated, bool targetEntryFirst)
    {
        var sense = NewSense("original");
        var sourceEntry = await CreateEntry("source-entry", sense);
        var targetEntry = NewEntry("target-entry");
        if (!targetEntryCreated) targetEntry = await Api.CreateEntry(targetEntry);

        List<Entry> before = targetEntryCreated ? [sourceEntry] : targetEntryFirst ? [targetEntry, sourceEntry] : [sourceEntry, targetEntry];
        var after = Copy(before);
        if (targetEntryCreated) after.Add(targetEntry);
        // moved AND edited in the same sync: the move must be followed by the field diff
        MoveSense(after, sense.Id, targetEntry.Id).Gloss["en"] = "edited";
        if (sourceEntryDeleted) after.RemoveAll(e => e.Id == sourceEntry.Id);
        await Sync(before, after);

        if (sourceEntryDeleted)
            (await Api.GetEntry(sourceEntry.Id)).Should().BeNull();
        else
            SenseIds(await GetEntry(sourceEntry.Id)).Should().BeEmpty();

        SenseIds(await GetEntry(targetEntry.Id)).Should().Equal(sense.Id);
        var actualSense = await GetSense(targetEntry.Id, sense.Id);
        actualSense.EntryId.Should().Be(targetEntry.Id);
        actualSense.Gloss["en"].Should().Be("edited");
        var tryGetSenseFromSource = () => Api.GetSense(sourceEntry.Id, sense.Id);
        await tryGetSenseFromSource.Should().ThrowAsync<NotFoundException>().WithMessage("*does not belong to the expected entry*");
    }

    [Fact]
    public async Task CanSyncSensesMovedIntoPositionInTargetEntry()
    {
        var moved1 = NewSense("m1");
        var moved2 = NewSense("m2");
        var first = NewSense("first");
        var last = NewSense("last");
        var source1 = await CreateEntry("source1", moved1);
        var source2 = await CreateEntry("source2", moved2);
        var target = await CreateEntry("target", first, last);

        var after = Copy(source1, source2, target);
        MoveSense(after, moved1.Id, target.Id, index: 1);
        MoveSense(after, moved2.Id, target.Id, index: 2);
        await Sync([source1, source2, target], after);

        SenseIds(await GetEntry(target.Id)).Should().Equal(first.Id, moved1.Id, moved2.Id, last.Id);
        SenseIds(await GetEntry(source1.Id)).Should().BeEmpty();
        SenseIds(await GetEntry(source2.Id)).Should().BeEmpty();
    }

    [Fact]
    public async Task CanSyncSensesSwappedBetweenEntries()
    {
        var senseA = NewSense("a");
        var senseB = NewSense("b");
        var entryA = await CreateEntry("entry-a", senseA);
        var entryB = await CreateEntry("entry-b", senseB);

        var after = Copy(entryA, entryB);
        MoveSense(after, senseA.Id, entryB.Id);
        MoveSense(after, senseB.Id, entryA.Id);
        await Sync([entryA, entryB], after);

        SenseIds(await GetEntry(entryA.Id)).Should().Equal(senseB.Id);
        SenseIds(await GetEntry(entryB.Id)).Should().Equal(senseA.Id);
    }

    [Fact]
    public async Task CanSyncSenseWithChildrenMovedToCreatedEntry()
    {
        var translation = NewTranslation("translation");
        var example = NewExample("example", translation);
        var sense = NewSense("moving", example);
        sense.Pictures = [new Picture { Id = Guid.NewGuid(), MediaUri = new MediaUri(Guid.NewGuid(), "localhost") }];
        var sourceEntry = await CreateEntry("source-entry", sense);
        var createdEntry = NewEntry("created-entry");

        // the children ride along inside the moved sense; they must not be mistaken for moves of their own
        var after = Copy(sourceEntry);
        after.Add(createdEntry);
        MoveSense(after, sense.Id, createdEntry.Id);
        await Sync([sourceEntry], after);

        var actualCreatedEntry = await GetEntry(createdEntry.Id);
        actualCreatedEntry.LexemeForm["en"].Should().Be("created-entry");
        SenseIds(actualCreatedEntry).Should().Equal(sense.Id);
        actualCreatedEntry.Senses[0].Pictures.Select(p => p.Id).Should().Equal(sense.Pictures[0].Id);
        ExampleIds(actualCreatedEntry.Senses[0]).Should().Equal(example.Id);
        actualCreatedEntry.Senses[0].ExampleSentences[0].Translations.Select(t => t.Id).Should().Equal(translation.Id);
        SenseIds(await GetEntry(sourceEntry.Id)).Should().BeEmpty();
    }

    #endregion

    #region Example sentence moves

    [Theory]
    // (sameEntry, sourceSenseFirst); sourceSenseFirst decides whether the remove (source sense) or the add (target sense) is diffed first
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public async Task CanSyncExampleSentenceMovedToDifferentSense(bool sameEntry, bool sourceSenseFirst)
    {
        var translation = NewTranslation("translation");
        var example = NewExample("example", translation);
        var sourceSense = NewSense("source", example);
        var targetSense = NewSense("target");
        List<Entry> before = sameEntry
            ? [await CreateEntry("entry", sourceSenseFirst ? [sourceSense, targetSense] : [targetSense, sourceSense])]
            : sourceSenseFirst
                ? [await CreateEntry("source-entry", sourceSense), await CreateEntry("target-entry", targetSense)]
                : [await CreateEntry("target-entry", targetSense), await CreateEntry("source-entry", sourceSense)];
        var sourceEntryId = EntryOf(before, sourceSense.Id).Id;
        var targetEntryId = EntryOf(before, targetSense.Id).Id;

        var after = Copy(before);
        // moved AND edited in the same sync: the move must be followed by the field diff
        var movedExample = MoveExample(after, example.Id, targetSense.Id);
        movedExample.Sentence["en"] = new RichString("edited example", "en");
        movedExample.Translations[0].Text["en"] = new RichString("edited translation", "en");
        if (sameEntry)
            await Sync(before.Single(), after.Single()); // the viewer's single-entry path must be move-aware too
        else
            await Sync(before, after);

        ExampleIds(await GetSense(targetEntryId, targetSense.Id)).Should().Equal(example.Id);
        ExampleIds(await GetSense(sourceEntryId, sourceSense.Id)).Should().BeEmpty();
        var actualExample = await Api.GetExampleSentence(targetEntryId, targetSense.Id, example.Id);
        actualExample.Should().NotBeNull();
        actualExample.Sentence["en"].Should().BeEquivalentTo(new RichString("edited example", "en"));
        actualExample.Translations.Should().ContainSingle()
            .Which.Text["en"].Should().BeEquivalentTo(new RichString("edited translation", "en"));
    }

    [Fact]
    public async Task CanSyncExampleSentencesMovedIntoPositionInTargetSense()
    {
        var moved1 = NewExample("m1");
        var moved2 = NewExample("m2");
        var first = NewExample("first");
        var last = NewExample("last");
        var source1 = NewSense("source1", moved1);
        var source2 = NewSense("source2", moved2);
        var target = NewSense("target", first, last);
        var entry = await CreateEntry("entry", source1, source2, target);

        var after = Copy(entry);
        MoveExample(after, moved1.Id, target.Id, index: 1);
        MoveExample(after, moved2.Id, target.Id, index: 2);
        await Sync([entry], after);

        ExampleIds(await GetSense(entry.Id, target.Id)).Should().Equal(first.Id, moved1.Id, moved2.Id, last.Id);
        ExampleIds(await GetSense(entry.Id, source1.Id)).Should().BeEmpty();
        ExampleIds(await GetSense(entry.Id, source2.Id)).Should().BeEmpty();
    }

    [Fact]
    public async Task CanSyncExampleSentencesSwappedBetweenSenses()
    {
        var exampleA = NewExample("a");
        var exampleB = NewExample("b");
        var senseA = NewSense("a", exampleA);
        var senseB = NewSense("b", exampleB);
        var entry = await CreateEntry("entry", senseA, senseB);

        var after = Copy(entry);
        MoveExample(after, exampleA.Id, senseB.Id);
        MoveExample(after, exampleB.Id, senseA.Id);
        await Sync([entry], after);

        ExampleIds(await GetSense(entry.Id, senseA.Id)).Should().Equal(exampleB.Id);
        ExampleIds(await GetSense(entry.Id, senseB.Id)).Should().Equal(exampleA.Id);
    }

    [Theory]
    // the source is listed first, so its delete is diffed before the target's add: only deferring the delete keeps the example alive
    [InlineData(true)]
    [InlineData(false)]
    public async Task CanSyncExampleSentenceMovedOutOfDeletedParent(bool wholeEntryDeleted)
    {
        var example = NewExample("example");
        var sourceSense = NewSense("source", example);
        var targetSense = NewSense("target");
        var sourceEntry = await CreateEntry("source-entry", sourceSense);
        var targetEntry = await CreateEntry("target-entry", targetSense);

        var after = Copy(sourceEntry, targetEntry);
        MoveExample(after, example.Id, targetSense.Id);
        if (wholeEntryDeleted)
            after.RemoveAll(e => e.Id == sourceEntry.Id);
        else
            after.Single(e => e.Id == sourceEntry.Id).Senses.Clear();
        await Sync([sourceEntry, targetEntry], after);

        ExampleIds(await GetSense(targetEntry.Id, targetSense.Id)).Should().Equal(example.Id);
        if (wholeEntryDeleted)
            (await Api.GetEntry(sourceEntry.Id)).Should().BeNull();
        else
            SenseIds(await GetEntry(sourceEntry.Id)).Should().BeEmpty();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CanSyncExampleSentenceMovedToCreatedSense(bool sourceSenseDeleted)
    {
        var translation = NewTranslation("translation");
        var example = NewExample("example", translation);
        var sourceSense = NewSense("source", example);
        var otherSense = NewSense("other");
        var entry = await CreateEntry("entry", sourceSense, otherSense);
        var createdSense = NewSense("created");

        // the translation rides along inside the moved example; it must not be mistaken for a move of its own
        var after = Copy(entry);
        after[0].Senses.Add(createdSense);
        MoveExample(after, example.Id, createdSense.Id);
        if (sourceSenseDeleted) after[0].Senses.RemoveAll(s => s.Id == sourceSense.Id);
        await Sync([entry], after);

        var actualEntry = await GetEntry(entry.Id);
        SenseIds(actualEntry).Should().Equal(sourceSenseDeleted ? [otherSense.Id, createdSense.Id] : [sourceSense.Id, otherSense.Id, createdSense.Id]);
        // the sense is created without its moved-in example and filled in afterward; its own fields must survive that split
        var actualCreatedSense = actualEntry.Senses.Single(s => s.Id == createdSense.Id);
        actualCreatedSense.Gloss["en"].Should().Be("created");
        ExampleIds(actualCreatedSense).Should().Equal(example.Id);
        actualCreatedSense.ExampleSentences[0].Sentence["en"]!.Spans.Should().ContainSingle().Which.Text.Should().Be("example");
        actualCreatedSense.ExampleSentences[0].Translations.Select(t => t.Id).Should().Equal(translation.Id);
        if (!sourceSenseDeleted) ExampleIds(await GetSense(entry.Id, sourceSense.Id)).Should().BeEmpty();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CanSyncExampleSentenceMovedToCreatedEntry(bool sourceEntryDeleted)
    {
        var example = NewExample("example");
        var sourceSense = NewSense("source", example);
        var sourceEntry = await CreateEntry("source-entry", sourceSense);
        var createdSense = NewSense("created");
        var createdEntry = NewEntry("created-entry", createdSense);

        var after = Copy(sourceEntry);
        after.Add(createdEntry);
        MoveExample(after, example.Id, createdSense.Id);
        if (sourceEntryDeleted) after.RemoveAll(e => e.Id == sourceEntry.Id);
        await Sync([sourceEntry], after);

        // the entry is created without its moved-in example and filled in afterward; its own fields must survive that split
        var actualCreatedEntry = await GetEntry(createdEntry.Id);
        actualCreatedEntry.LexemeForm["en"].Should().Be("created-entry");
        SenseIds(actualCreatedEntry).Should().Equal(createdSense.Id);
        ExampleIds(actualCreatedEntry.Senses[0]).Should().Equal(example.Id);
        actualCreatedEntry.Senses[0].ExampleSentences[0].Sentence["en"]!.Spans.Should().ContainSingle().Which.Text.Should().Be("example");
        if (sourceEntryDeleted)
            (await Api.GetEntry(sourceEntry.Id)).Should().BeNull();
        else
            ExampleIds(await GetSense(sourceEntry.Id, sourceSense.Id)).Should().BeEmpty();
    }

    #endregion

    #region Sense and example sentence moves in the same sync

    [Theory]
    // (sourceEntryFirst, targetEntryCreated); sourceEntryFirst decides whether the example's old sense (a remove) or the moved sense (an add)
    // is diffed first, which only matters when both entries are updated
    [InlineData(true, false)]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public async Task CanSyncExampleSentenceMovedIntoSenseThatMovedToDifferentEntry(bool sourceEntryFirst, bool targetEntryCreated)
    {
        var example = NewExample("example");
        var movingSense = NewSense("moving");
        var stayingSense = NewSense("staying", example);
        var sourceEntry = await CreateEntry("source-entry", movingSense, stayingSense);
        var targetEntry = NewEntry("target-entry");
        if (!targetEntryCreated) targetEntry = await Api.CreateEntry(targetEntry);

        List<Entry> before = targetEntryCreated ? [sourceEntry] : sourceEntryFirst ? [sourceEntry, targetEntry] : [targetEntry, sourceEntry];
        var after = Copy(before);
        if (targetEntryCreated) after.Add(targetEntry);
        MoveSense(after, movingSense.Id, targetEntry.Id);
        MoveExample(after, example.Id, movingSense.Id);
        await Sync(before, after);

        ExampleIds(await GetSense(targetEntry.Id, movingSense.Id)).Should().Equal(example.Id);
        var actualSourceEntry = await GetEntry(sourceEntry.Id);
        SenseIds(actualSourceEntry).Should().Equal(stayingSense.Id);
        ExampleIds(actualSourceEntry.Senses[0]).Should().BeEmpty();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CanSyncExampleSentenceMovedOutOfSenseThatMovedToDifferentEntry(bool sourceEntryFirst)
    {
        var example = NewExample("example");
        var movingSense = NewSense("moving", example);
        var stayingSense = NewSense("staying");
        var sourceEntry = await CreateEntry("source-entry", movingSense, stayingSense);
        var targetEntry = await CreateEntry("target-entry");

        List<Entry> before = sourceEntryFirst ? [sourceEntry, targetEntry] : [targetEntry, sourceEntry];
        var after = Copy(before);
        MoveSense(after, movingSense.Id, targetEntry.Id);
        MoveExample(after, example.Id, stayingSense.Id);
        await Sync(before, after);

        ExampleIds(await GetSense(targetEntry.Id, movingSense.Id)).Should().BeEmpty();
        ExampleIds(await GetSense(sourceEntry.Id, stayingSense.Id)).Should().Equal(example.Id);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CanSyncExampleSentenceMovedIntoSenseMovedOutOfDeletedEntry(bool deletedEntryFirst)
    {
        // FLEx "merge entry": the survivor takes a sense, and an example of a sense that dies with the entry is rescued into it
        var example = NewExample("example");
        var rescuedSense = NewSense("rescued");
        var dyingSense = NewSense("dying", example);
        var deletedEntry = await CreateEntry("deleted-entry", rescuedSense, dyingSense);
        var survivingEntry = await CreateEntry("surviving-entry");

        List<Entry> before = deletedEntryFirst ? [deletedEntry, survivingEntry] : [survivingEntry, deletedEntry];
        var after = Copy(before);
        MoveSense(after, rescuedSense.Id, survivingEntry.Id);
        MoveExample(after, example.Id, rescuedSense.Id);
        after.RemoveAll(e => e.Id == deletedEntry.Id);
        await Sync(before, after);

        (await Api.GetEntry(deletedEntry.Id)).Should().BeNull();
        var actualSurvivingEntry = await GetEntry(survivingEntry.Id);
        SenseIds(actualSurvivingEntry).Should().Equal(rescuedSense.Id);
        ExampleIds(actualSurvivingEntry.Senses[0]).Should().Equal(example.Id);
    }

    #endregion

    #region Unsupported moves

    // FLEx can also re-parent pictures and translations, but we don't support that yet, so the sync
    // must detect and reject it before writing anything.

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SyncThrows_PictureMovedToDifferentSense(bool targetSenseCreated)
    {
        var picture = new Picture { Id = Guid.NewGuid(), MediaUri = new MediaUri(Guid.NewGuid(), "localhost") };
        var sourceSense = NewSense("source");
        sourceSense.Pictures = [picture];
        var targetSense = NewSense("target");
        var entry = NewEntry("entry", targetSenseCreated ? [sourceSense] : [sourceSense, targetSense]);

        var after = entry.Copy();
        if (targetSenseCreated) after.Senses.Add(targetSense);
        var movedPicture = after.Senses[0].Pictures.Single();
        after.Senses[0].Pictures.Clear();
        after.Senses.Single(s => s.Id == targetSense.Id).Pictures.Add(movedPicture);

        var act = () => Sync(entry, after);
        await act.Should().ThrowAsync<MoveNotSupportedException>().WithMessage($"*{picture.Id}*");
    }

    [Fact]
    public async Task SyncThrows_TranslationMovedToDifferentExampleSentence_AndWritesNothing()
    {
        var translation = NewTranslation("translation");
        var example1 = NewExample("one", translation);
        var example2 = NewExample("two");
        var sense = NewSense("gloss", example1, example2);
        var entry = await CreateEntry("entry", sense);

        var after = entry.Copy();
        var afterExample1 = after.Senses[0].ExampleSentences.Single(e => e.Id == example1.Id);
        var movedTranslation = afterExample1.Translations.Single();
        afterExample1.Translations.Clear();
        after.Senses[0].ExampleSentences.Single(e => e.Id == example2.Id).Translations.Add(movedTranslation);
        // an edit that would be applied if the sync got past the move check
        after.LexemeForm["en"] = "edited";

        var act = () => Sync(entry, after);
        await act.Should().ThrowAsync<MoveNotSupportedException>().WithMessage($"*{translation.Id}*");

        // all-or-nothing: nothing was written, the translation is still on its original example
        (await GetEntry(entry.Id)).LexemeForm["en"].Should().Be("entry");
        (await Api.GetExampleSentence(entry.Id, sense.Id, example1.Id))!.Translations.Select(t => t.Id).Should().Equal(translation.Id);
        (await Api.GetExampleSentence(entry.Id, sense.Id, example2.Id))!.Translations.Should().BeEmpty();
    }

    #endregion

    #region Complex forms referencing a moved sense

    [Theory]
    // Old component still exists in `after`, new component already exists in `before`.
    [InlineData("complex-form,old-component,new-component", false, false)]
    [InlineData("complex-form,new-component,old-component", false, false)]
    [InlineData("old-component,complex-form,new-component", false, false)]
    [InlineData("old-component,new-component,complex-form", false, false)]
    [InlineData("new-component,complex-form,old-component", false, false)]
    [InlineData("new-component,old-component,complex-form", false, false)]
    // Old component is removed entirely in `after` (e.g. the now-empty entry was deleted in FLEx).
    [InlineData("complex-form,old-component,new-component", true, false)]
    [InlineData("complex-form,new-component,old-component", true, false)]
    [InlineData("old-component,complex-form,new-component", true, false)]
    [InlineData("old-component,new-component,complex-form", true, false)]
    [InlineData("new-component,complex-form,old-component", true, false)]
    [InlineData("new-component,old-component,complex-form", true, false)]
    // New component is created in this sync, so the moved sense lands on an entry that goes through AddAndGet instead of Replace.
    [InlineData("complex-form,old-component,new-component", false, true)]
    [InlineData("complex-form,new-component,old-component", false, true)]
    [InlineData("old-component,complex-form,new-component", false, true)]
    [InlineData("old-component,new-component,complex-form", false, true)]
    [InlineData("new-component,complex-form,old-component", false, true)]
    [InlineData("new-component,old-component,complex-form", false, true)]
    // Both: old component deleted AND new component created in the same sync.
    [InlineData("complex-form,old-component,new-component", true, true)]
    [InlineData("complex-form,new-component,old-component", true, true)]
    [InlineData("old-component,complex-form,new-component", true, true)]
    [InlineData("old-component,new-component,complex-form", true, true)]
    [InlineData("new-component,complex-form,old-component", true, true)]
    [InlineData("new-component,old-component,complex-form", true, true)]
    public async Task CanSyncComponentWhenSenseMovesToDifferentEntry(string entryOrderString, bool oldComponentDeleted, bool newComponentCreated)
    {
        var entryOrder = entryOrderString.Split(",", StringSplitOptions.TrimEntries);

        var sense = NewSense("moving");
        var oldComponentEntry = await CreateEntry("old-component", sense);
        var newComponentEntry = NewEntry("new-component");
        if (!newComponentCreated) newComponentEntry = await Api.CreateEntry(newComponentEntry);

        var complexForm = NewEntry("complex form");
        complexForm.Components.Add(ComplexFormComponent.FromEntries(complexForm, oldComponentEntry, sense.Id));
        complexForm = await Api.CreateEntry(complexForm);

        var oldComponentEntryAfter = oldComponentEntry.Copy();
        var newComponentEntryAfter = newComponentEntry.Copy();
        MoveSense([oldComponentEntryAfter, newComponentEntryAfter], sense.Id, newComponentEntry.Id);
        var complexFormAfter = complexForm.Copy();
        complexFormAfter.Components = [ComplexFormComponent.FromEntries(complexForm, newComponentEntry, sense.Id)];

        Dictionary<string, (Entry Before, Entry After)> entries = new()
        {
            ["complex-form"] = (complexForm, complexFormAfter),
            ["old-component"] = (oldComponentEntry, oldComponentEntryAfter),
            ["new-component"] = (newComponentEntry, newComponentEntryAfter),
        };
        var before = entryOrder.Where(name => !(newComponentCreated && name == "new-component")).Select(name => entries[name].Before).ToList();
        var after = entryOrder.Where(name => !(oldComponentDeleted && name == "old-component")).Select(name => entries[name].After).ToList();
        await Sync(before, after);

        var actualComplexForm = await GetEntry(complexForm.Id);
        actualComplexForm.Components.Should().ContainSingle();
        actualComplexForm.Components[0].ComponentEntryId.Should().Be(newComponentEntry.Id);
        actualComplexForm.Components[0].ComponentSenseId.Should().Be(sense.Id);

        if (oldComponentDeleted)
        {
            (await Api.GetEntry(oldComponentEntry.Id)).Should().BeNull();
        }
        else
        {
            var actualOldComponentEntry = await GetEntry(oldComponentEntry.Id);
            actualOldComponentEntry.Senses.Should().BeEmpty();
            actualOldComponentEntry.ComplexForms.Should().BeEmpty();
        }

        var actualNewComponentEntry = await GetEntry(newComponentEntry.Id);
        SenseIds(actualNewComponentEntry).Should().Equal(sense.Id);
        actualNewComponentEntry.ComplexForms.Should().ContainSingle().Which.ComplexFormEntryId.Should().Be(complexForm.Id);
    }

    #endregion

    #region Helpers

    protected static Entry NewEntry(string lexemeForm, params Sense[] senses)
    {
        return new Entry { Id = Guid.NewGuid(), LexemeForm = { { "en", lexemeForm } }, Senses = [.. senses] };
    }

    protected static Sense NewSense(string gloss, params ExampleSentence[] examples)
    {
        return new Sense { Id = Guid.NewGuid(), Gloss = { { "en", gloss } }, ExampleSentences = [.. examples] };
    }

    protected static ExampleSentence NewExample(string sentence, params Translation[] translations)
    {
        return new ExampleSentence { Id = Guid.NewGuid(), Sentence = { { "en", new RichString(sentence) } }, Translations = [.. translations] };
    }

    protected static Translation NewTranslation(string text)
    {
        return new Translation { Id = Guid.NewGuid(), Text = { { "en", new RichString(text) } } };
    }

    protected Task<Entry> CreateEntry(string lexemeForm, params Sense[] senses)
    {
        return Api.CreateEntry(NewEntry(lexemeForm, senses));
    }

    /// <summary>Deep copies, as the starting point for the after state.</summary>
    protected static List<Entry> Copy(IEnumerable<Entry> entries) => [.. entries.Select(e => e.Copy())];
    protected static List<Entry> Copy(params Entry[] entries) => Copy(entries.AsEnumerable());

    /// <summary>Re-parents the sense the way FLEx does: same guid, new owner (at the end unless an index is given).</summary>
    protected static Sense MoveSense(IList<Entry> entries, Guid senseId, Guid toEntryId, int? index = null)
    {
        var fromEntry = EntryOf(entries, senseId);
        var sense = fromEntry.Senses.Single(s => s.Id == senseId);
        fromEntry.Senses.Remove(sense);
        sense.EntryId = toEntryId;
        var toSenses = entries.Single(e => e.Id == toEntryId).Senses;
        toSenses.Insert(index ?? toSenses.Count, sense);
        return sense;
    }

    /// <summary>Re-parents the example the way FLEx does: same guid, new owner (at the end unless an index is given).</summary>
    protected static ExampleSentence MoveExample(IList<Entry> entries, Guid exampleId, Guid toSenseId, int? index = null)
    {
        var fromSense = entries.SelectMany(e => e.Senses).Single(s => s.ExampleSentences.Any(x => x.Id == exampleId));
        var example = fromSense.ExampleSentences.Single(x => x.Id == exampleId);
        fromSense.ExampleSentences.Remove(example);
        example.SenseId = toSenseId;
        var toExamples = entries.SelectMany(e => e.Senses).Single(s => s.Id == toSenseId).ExampleSentences;
        toExamples.Insert(index ?? toExamples.Count, example);
        return example;
    }

    protected static Entry EntryOf(IEnumerable<Entry> entries, Guid senseId)
    {
        return entries.Single(e => e.Senses.Any(s => s.Id == senseId));
    }

    /// <summary>The whole-project sync (fw-headless).</summary>
    protected Task<int> Sync(IList<Entry> before, IList<Entry> after)
    {
        return EntrySync.SyncFull([.. before], [.. after], Api);
    }

    /// <summary>The single-entry sync (the viewer's updateEntry).</summary>
    protected Task<int> Sync(Entry before, Entry after)
    {
        return EntrySync.SyncFull(before, after, Api);
    }

    protected async Task<Entry> GetEntry(Guid entryId)
    {
        var entry = await Api.GetEntry(entryId);
        entry.Should().NotBeNull();
        return entry;
    }

    protected async Task<Sense> GetSense(Guid entryId, Guid senseId)
    {
        var sense = await Api.GetSense(entryId, senseId);
        sense.Should().NotBeNull();
        return sense;
    }

    protected static IEnumerable<Guid> SenseIds(Entry entry) => entry.Senses.Select(s => s.Id);
    protected static IEnumerable<Guid> ExampleIds(Sense sense) => sense.ExampleSentences.Select(e => e.Id);

    #endregion
}
