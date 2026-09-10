using MiniLcm.Exceptions;
using MiniLcm.Media;
using MiniLcm.SyncHelpers;

namespace MiniLcm.Tests;

public class SyncContextTests
{
    private static Entry NewEntry(params Sense[] senses)
    {
        return new Entry { Id = Guid.NewGuid(), LexemeForm = { { "en", "entry" } }, Senses = [.. senses] };
    }

    private static Sense NewSense(params Picture[] pictures)
    {
        return new Sense { Id = Guid.NewGuid(), Gloss = { { "en", "gloss" } }, Pictures = [.. pictures] };
    }

    private static ExampleSentence NewExample()
    {
        return new ExampleSentence { Id = Guid.NewGuid(), Sentence = { { "en", new RichString("example") } } };
    }

    private static Picture NewPicture()
    {
        return new Picture { Id = Guid.NewGuid(), MediaUri = new MediaUri(Guid.NewGuid(), "test") };
    }

    #region Move indexes

    [Fact]
    public void MovedInAndStillExists_TrackSenseReparentingAcrossEntries()
    {
        var moving = NewSense();
        var doomed = NewSense();
        var source = NewEntry(moving, doomed);
        var target = NewEntry();

        var sourceAfter = source.Copy();
        var movedSense = sourceAfter.Senses[0];
        sourceAfter.Senses.Clear();
        var targetAfter = target.Copy();
        targetAfter.Senses.Add(movedSense);

        var context = SyncContext.For([source, target], [sourceAfter, targetAfter]);

        context.ExistedBefore(movedSense).Should().NotBeNull().And.Subject.As<Sense>().Id.Should().Be(moving.Id);
        context.StillExists(moving).Should().BeTrue("it moved to the target entry rather than being deleted");
        context.StillExists(doomed).Should().BeFalse("it exists nowhere after");
        context.ExistedBefore(NewSense()).Should().BeNull("a genuinely new sense has no before-version");
    }

    [Fact]
    public void MovedInAndStillExists_TrackExampleReparentingBetweenSenses()
    {
        var example = NewExample();
        var doomed = NewExample();
        var sourceSense = NewSense();
        sourceSense.ExampleSentences = [example, doomed];
        var targetSense = NewSense();
        var entry = NewEntry(sourceSense, targetSense);

        var after = entry.Copy();
        var movedExample = after.Senses[0].ExampleSentences[0];
        after.Senses[0].ExampleSentences.Clear();
        after.Senses[1].ExampleSentences.Add(movedExample);

        var context = SyncContext.For(entry, after);

        context.ExistedBefore(movedExample).Should().NotBeNull().And.Subject.As<ExampleSentence>().Id.Should().Be(example.Id);
        context.StillExists(example).Should().BeTrue();
        context.StillExists(doomed).Should().BeFalse("it exists nowhere after");
        context.ExistedBefore(NewExample()).Should().BeNull();
    }

    #endregion

    #region Stripping moved-in descendants out of a create payload

    [Fact]
    public void WithoutMovedInDescendants_Entry_DropsMovedInSensesAndExamples_KeepsNewOnes()
    {
        var movedInSense = NewSense();
        var movedInExample = NewExample();
        var newSenseHoldingAMovedInExample = NewSense();
        newSenseHoldingAMovedInExample.ExampleSentences = [movedInExample];
        var newSenseWithNewExample = NewSense();
        newSenseWithNewExample.ExampleSentences = [NewExample()];

        // before: the sense lives under some other entry, the example under some other sense
        var otherSense = NewSense();
        otherSense.ExampleSentences = [movedInExample.Copy()];
        var otherEntry = NewEntry(movedInSense.Copy(), otherSense);
        var created = NewEntry(newSenseHoldingAMovedInExample, newSenseWithNewExample, movedInSense);

        // after: the moved items have left otherEntry, so each id lives in exactly one place
        var otherEntryAfter = otherEntry.Copy();
        otherEntryAfter.Senses.Clear();
        var context = SyncContext.For([otherEntry], [otherEntryAfter, created]);

        context.HasMovedInDescendants(created).Should().BeTrue();
        var payload = context.WithoutMovedInDescendants(created);

        // the moved-in sense is left out; the two genuinely new senses stay
        payload.Senses.Select(s => s.Id)
            .Should().Equal(newSenseHoldingAMovedInExample.Id, newSenseWithNewExample.Id);
        payload.Senses[0].ExampleSentences.Should().BeEmpty("the moved-in example is left out of the kept sense");
        payload.Senses[1].ExampleSentences.Should().ContainSingle("a genuinely new example stays in the payload");

        created.Senses.Should().HaveCount(3, "the strip returns a copy and does not mutate the original");
        created.Senses[0].ExampleSentences.Should().ContainSingle();
    }

    [Fact]
    public void WithoutMovedInDescendants_Sense_DropsMovedInExamples_KeepsNewOnes()
    {
        var movedInExample = NewExample();
        var newExample = NewExample();
        var sourceSense = NewSense();
        sourceSense.ExampleSentences = [movedInExample.Copy()];
        var before = NewEntry(sourceSense);

        var createdSense = NewSense();
        createdSense.ExampleSentences = [movedInExample, newExample];
        // after: the example has left sourceSense, so its id lives in exactly one place
        var sourceSenseAfter = sourceSense.Copy();
        sourceSenseAfter.ExampleSentences.Clear();
        var after = NewEntry(sourceSenseAfter, createdSense);

        var context = SyncContext.For([before], [after]);

        context.HasMovedInDescendants(createdSense).Should().BeTrue();
        var payload = context.WithoutMovedInDescendants(createdSense);

        payload.ExampleSentences.Select(e => e.Id).Should().Equal(newExample.Id);
        createdSense.ExampleSentences.Should().HaveCount(2, "the strip returns a copy");
    }

    [Fact]
    public void HasMovedInDescendants_IsFalse_WhenEverythingIsNew()
    {
        var newSense = NewSense();
        newSense.ExampleSentences = [NewExample()];
        var created = NewEntry(newSense);

        var context = SyncContext.For([NewEntry()], [NewEntry(), created]);

        context.HasMovedInDescendants(created).Should().BeFalse();
        context.HasMovedInDescendants(newSense).Should().BeFalse();
    }

    #endregion

    #region Deferred deletes

    [Fact]
    public async Task DeferredDeletes_DrainInOrder_IncludingADeleteThatQueuesAnother()
    {
        var order = new List<int>();
        var deferred = new DeferredDeletes();

        (await deferred.Defer(() => { order.Add(1); return Task.FromResult(1); }))
            .Should().Be(0, "deferring counts the change only when the queue is drained");
        await deferred.Defer(async () =>
        {
            order.Add(2);
            await deferred.Defer(() => { order.Add(3); return Task.FromResult(1); });
            return 1;
        });

        order.Should().BeEmpty("nothing runs until the drain");
        var changes = await deferred.DeleteAll();

        order.Should().Equal(1, 2, 3);
        changes.Should().Be(3);
    }

    [Fact]
    public async Task For_DefersDeletes_UntilDeleteAll()
    {
        var context = SyncContext.For([NewEntry()], [NewEntry()]);
        var deleted = false;

        (await context.DeferDelete(() => { deleted = true; return Task.FromResult(1); })).Should().Be(0);
        deleted.Should().BeFalse("a project sync defers deletes to the end of the walk");

        (await context.DeleteAll()).Should().Be(1);
        deleted.Should().BeTrue();
    }

    [Fact]
    public async Task Empty_RejectsDeferredDeletes_AndDetectsNoMoves()
    {
        SyncContext.Empty.ExistedBefore(NewSense()).Should().BeNull();
        SyncContext.Empty.StillExists(NewSense()).Should().BeFalse();

        // Empty tracks no moves, so nothing drains its queue; deferring here would silently drop the delete.
        var deleted = false;
        await FluentActions.Awaiting(() => SyncContext.Empty.DeferDelete(() => { deleted = true; return Task.FromResult(1); }))
            .Should().ThrowAsync<InvalidOperationException>();
        deleted.Should().BeFalse();
        (await SyncContext.Empty.DeleteAll()).Should().Be(0);
    }

    #endregion

    #region Unsupported moves are refused before anything is written

    [Fact]
    public void PictureMovedToDifferentSense_Throws()
    {
        var picture = NewPicture();
        var sourceSense = NewSense(picture);
        var targetSense = NewSense();
        var before = NewEntry(sourceSense, targetSense);

        var after = before.Copy();
        after.Senses[0].Pictures.Clear();
        after.Senses[1].Pictures.Add(picture.Copy());

        var act = () => SyncContext.For(before, after);
        act.Should().Throw<MoveNotSupportedException>()
            .WithMessage($"*{picture.Id}*{sourceSense.Id}*{targetSense.Id}*");
        act = () => SyncContext.For([before], [after]);
        act.Should().Throw<MoveNotSupportedException>();
    }

    [Fact]
    public void PictureMovedToSurvivingSenseWhileItsOwnSenseIsDeleted_Throws()
    {
        var picture = NewPicture();
        var deletedSense = NewSense(picture);
        var survivingSense = NewSense();
        var before = NewEntry(deletedSense, survivingSense);

        var after = before.Copy();
        after.Senses.RemoveAt(0);
        after.Senses[0].Pictures.Add(picture.Copy());

        var act = () => SyncContext.For(before, after);
        act.Should().Throw<MoveNotSupportedException>();
    }

    [Fact]
    public void SenseMovedToAnotherEntryCarryingItsChildren_DoesNotThrow()
    {
        // the picture and translation travel with their moved parents; their direct parents don't change
        var picture = NewPicture();
        var movingSense = NewSense(picture);
        movingSense.ExampleSentences = [NewExample()];
        var sourceEntry = NewEntry(movingSense);

        var sourceEntryAfter = sourceEntry.Copy();
        var movedSense = sourceEntryAfter.Senses[0];
        sourceEntryAfter.Senses.Clear();
        var createdEntry = NewEntry(movedSense);

        var act = () => SyncContext.For([sourceEntry], [sourceEntryAfter, createdEntry]);
        act.Should().NotThrow();
    }

    [Fact]
    public void GenuinePictureCreateAndDelete_DoesNotThrow()
    {
        var before = NewEntry(NewSense(NewPicture()), NewSense());
        var after = before.Copy();
        after.Senses[0].Pictures.Clear();
        after.Senses[1].Pictures.Add(NewPicture());

        var act = () => SyncContext.For(before, after);
        act.Should().NotThrow();
    }

    [Fact]
    public void LegacyMissingTranslationIdsAcrossExamples_DoNotThrow()
    {
        // the legacy placeholder id recurs on many examples and moves around as examples change; never a move
#pragma warning disable CS0618
        var legacyId = Translation.MissingTranslationId;
#pragma warning restore CS0618
        var sense = NewSense();
        sense.ExampleSentences =
        [
            new ExampleSentence { Id = Guid.NewGuid(), Translations = [new Translation { Id = legacyId }] },
            new ExampleSentence { Id = Guid.NewGuid(), Translations = [new Translation { Id = legacyId }] }
        ];
        var before = NewEntry(sense);

        var after = before.Copy();
        after.Senses[0].ExampleSentences[0].Translations.Clear();

        var act = () => SyncContext.For(before, after);
        act.Should().NotThrow();
    }

    #endregion
}
