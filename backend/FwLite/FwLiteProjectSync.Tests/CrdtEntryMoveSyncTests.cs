using FwLiteProjectSync.Tests.Fixtures;
using MiniLcm;
using MiniLcm.Models;
using MiniLcm.SyncHelpers;

namespace FwLiteProjectSync.Tests;

public class CrdtEntryMoveSyncTests(ExtraWritingSystemsSyncFixture fixture) : EntryMoveSyncTestsBase(fixture)
{
    protected override IMiniLcmApi GetApi(SyncFixture fixture)
    {
        return fixture.CrdtApi;
    }

    // Only CRDT can diverge from the diff's "before": sync diffs the snapshot against FwData and applies to CRDT,
    // whereas the FwData pass diffs FwData's own current state. So these cases live only in the CRDT subclass.

    // CRDT as a technology has to handle these cases anyway, which is why we sync in the order we do.

    #region Move target deleted or reparented in CRDT

    // The CRDT deletion wins when the object it deleted becomes a move's target from the other side
    // (FwData intentionally still throws on a missing target).

    [Fact]
    public async Task SenseMovedToEntryDeletedInCrdt_IsDeleted()
    {
        var sense = NewSense("moving");
        var sourceEntry = await CreateEntry("source-entry", sense);
        var targetEntry = await CreateEntry("target-entry");
        await Api.DeleteEntry(targetEntry.Id);
        Entry[] before = [sourceEntry, targetEntry];
        var after = Copy(before);
        MoveSense(after, sense, targetEntry.Id);
        await Sync(before, after);

        (await Api.GetEntry(targetEntry.Id)).Should().BeNull();
        SenseIds(await GetEntry(sourceEntry.Id)).Should().BeEmpty();
        (await Api.GetSense(sense.Id)).Should().BeNull();
    }

    [Fact]
    public async Task ExampleSentenceMovedToSenseDeletedInCrdt_IsDeleted()
    {
        var example = NewExample("example");
        var sourceSense = NewSense("source", example);
        var targetSense = NewSense("target");
        var entry = await CreateEntry("entry", sourceSense, targetSense);
        await Api.DeleteSense(entry.Id, targetSense.Id);

        var after = Copy([entry]);
        MoveExample(after, example, targetSense.Id);
        await Sync([entry], after);

        var actual = await GetEntry(entry.Id);
        SenseIds(actual).Should().Equal(sourceSense.Id);
        ExampleIds(actual.Senses[0]).Should().BeEmpty();
    }

    // A target sense CRDT reparented since the snapshot is still a valid target: the example follows it to its new entry.

    [Fact]
    public async Task ExampleSentenceMovedToSenseReparentedInCrdt_FollowsSense()
    {
        var example = NewExample("example");
        var sourceSense = NewSense("source", example);
        var targetSense = NewSense("target");
        var entry = await CreateEntry("entry", sourceSense, targetSense);
        var otherEntry = await CreateEntry("other");

        // CRDT reparented the target sense to another entry after the snapshot
        await Api.MoveSense(otherEntry.Id, targetSense.Id, new BetweenPosition(null, null), MoveKind.Reparent);

        // FLEx moved the example into the target sense (still under the original entry in FLEx's view)
        Entry[] before = [entry, otherEntry];
        var after = Copy(before);
        MoveExample(after, example, targetSense.Id);
        await Sync(before, after);

        var movedTarget = (await GetEntry(otherEntry.Id)).Senses.Single(s => s.Id == targetSense.Id);
        ExampleIds(movedTarget).Should().Equal(example.Id);
    }

    #endregion

    #region Reorder of an item deleted or reparented in CRDT

    // Such a reorder is moot and must be skipped, not throw and wedge the whole sync.

    [Fact]
    public async Task ReorderingASenseDeletedInCrdt_DoesNotWedge()
    {
        var deleted = NewSense("deleted");
        var keep1 = NewSense("keep1");
        var keep2 = NewSense("keep2");
        var keep3 = NewSense("keep3");
        var entry = await CreateEntry("entry", deleted, keep1, keep2, keep3);
        await Api.DeleteSense(entry.Id, deleted.Id);

        // three senses keep their order, so the diff reorders exactly the (now-deleted) one
        var after = entry.Copy();
        var moved = after.Senses[0];
        after.Senses.RemoveAt(0);
        after.Senses.Add(moved);

        var sync = () => Sync([entry], [after]);
        await sync.Should().NotThrowAsync();

        SenseIds(await GetEntry(entry.Id)).Should().Equal(keep1.Id, keep2.Id, keep3.Id);
    }

    [Fact]
    public async Task ReorderingAnExampleSentenceDeletedInCrdt_DoesNotWedge()
    {
        var deleted = NewExample("deleted");
        var keep1 = NewExample("keep1");
        var keep2 = NewExample("keep2");
        var keep3 = NewExample("keep3");
        var sense = NewSense("sense", deleted, keep1, keep2, keep3);
        var entry = await CreateEntry("entry", sense);
        await Api.DeleteExampleSentence(entry.Id, sense.Id, deleted.Id);

        var after = entry.Copy();
        var moved = after.Senses[0].ExampleSentences[0];
        after.Senses[0].ExampleSentences.RemoveAt(0);
        after.Senses[0].ExampleSentences.Add(moved);

        var sync = () => Sync([entry], [after]);
        await sync.Should().NotThrowAsync();

        ExampleIds((await GetEntry(entry.Id)).Senses[0]).Should().Equal(keep1.Id, keep2.Id, keep3.Id);
    }

    [Fact]
    public async Task ReorderingASenseReparentedInCrdt_DoesNotWedge()
    {
        var moved = NewSense("moved");
        var keep1 = NewSense("keep1");
        var keep2 = NewSense("keep2");
        var keep3 = NewSense("keep3");
        var sourceEntry = await CreateEntry("source", moved, keep1, keep2, keep3);
        var targetEntry = await CreateEntry("target");
        await Api.MoveSense(targetEntry.Id, moved.Id, new BetweenPosition(null, null), MoveKind.Reparent);

        // the other side merely reordered the sense within its original entry
        var sourceAfter = sourceEntry.Copy();
        var reordered = sourceAfter.Senses[0];
        sourceAfter.Senses.RemoveAt(0);
        sourceAfter.Senses.Add(reordered);

        var sync = () => Sync([sourceEntry, targetEntry], [sourceAfter, targetEntry.Copy()]);
        await sync.Should().NotThrowAsync();

        SenseIds(await GetEntry(sourceEntry.Id)).Should().Equal(keep1.Id, keep2.Id, keep3.Id);
        SenseIds(await GetEntry(targetEntry.Id)).Should().Equal(moved.Id);
    }

    [Fact]
    public async Task ReorderingAnExampleSentenceReparentedInCrdt_DoesNotWedge()
    {
        var moved = NewExample("moved");
        var keep1 = NewExample("keep1");
        var keep2 = NewExample("keep2");
        var keep3 = NewExample("keep3");
        var sourceSense = NewSense("source", moved, keep1, keep2, keep3);
        var targetSense = NewSense("target");
        var entry = await CreateEntry("entry", sourceSense, targetSense);
        await Api.MoveExampleSentence(entry.Id, targetSense.Id, moved.Id, new BetweenPosition(null, null), MoveKind.Reparent);

        // the other side merely reordered the example within its original sense
        var after = entry.Copy();
        var reordered = after.Senses[0].ExampleSentences[0];
        after.Senses[0].ExampleSentences.RemoveAt(0);
        after.Senses[0].ExampleSentences.Add(reordered);

        var sync = () => Sync([entry], [after]);
        await sync.Should().NotThrowAsync();

        var actual = await GetEntry(entry.Id);
        ExampleIds(actual.Senses[0]).Should().Equal(keep1.Id, keep2.Id, keep3.Id);
        ExampleIds(actual.Senses[1]).Should().Equal(moved.Id);
    }

    [Fact]
    public async Task ReorderingWithinASenseReparentedInCrdt_StillApplies()
    {
        var example1 = NewExample("example1");
        var example2 = NewExample("example2");
        var example3 = NewExample("example3");
        var sense = NewSense("sense", example1, example2, example3);
        var sourceEntry = await CreateEntry("source", sense);
        var targetEntry = await CreateEntry("target");
        await Api.MoveSense(targetEntry.Id, sense.Id, new BetweenPosition(null, null), MoveKind.Reparent);

        // the other side reordered the examples, with the sense still under its original entry
        Entry[] before = [sourceEntry, targetEntry];
        var after = Copy(before);
        var sourceAfter = after[0];
        var reordered = sourceAfter.Senses[0].ExampleSentences[0];
        sourceAfter.Senses[0].ExampleSentences.RemoveAt(0);
        sourceAfter.Senses[0].ExampleSentences.Add(reordered);

        await Sync(before, after);

        var movedSense = (await GetEntry(targetEntry.Id)).Senses.Single(s => s.Id == sense.Id);
        ExampleIds(movedSense).Should().Equal(example2.Id, example3.Id, example1.Id);
    }

    #endregion
}
