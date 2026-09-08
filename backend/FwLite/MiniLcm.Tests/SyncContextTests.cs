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

    private static Picture NewPicture()
    {
        return new Picture { Id = Guid.NewGuid(), MediaUri = new MediaUri(Guid.NewGuid(), "test") };
    }

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

        var act = () => SyncContext.ForEntrySync(before, after);
        act.Should().Throw<MoveNotSupportedException>()
            .WithMessage($"*{picture.Id}*{sourceSense.Id}*{targetSense.Id}*");
        act = () => SyncContext.ForProjectSync([before], [after]);
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

        var act = () => SyncContext.ForEntrySync(before, after);
        act.Should().Throw<MoveNotSupportedException>();
    }

    [Fact]
    public void SenseMovedToAnotherEntryCarryingItsChildren_DoesNotThrow()
    {
        // the picture and translation travel with their moved parents; their direct parents don't change
        var picture = NewPicture();
        var movingSense = NewSense(picture);
        movingSense.ExampleSentences =
        [
            new ExampleSentence
            {
                Id = Guid.NewGuid(),
                Sentence = { { "en", new RichString("example") } },
                Translations = [new Translation { Id = Guid.NewGuid(), Text = { { "en", new RichString("translation") } } }]
            }
        ];
        var sourceEntry = NewEntry(movingSense);

        var sourceEntryAfter = sourceEntry.Copy();
        var movedSense = sourceEntryAfter.Senses[0];
        sourceEntryAfter.Senses.Clear();
        var createdEntry = NewEntry(movedSense);

        var act = () => SyncContext.ForProjectSync([sourceEntry], [sourceEntryAfter, createdEntry]);
        act.Should().NotThrow();
    }

    [Fact]
    public void GenuinePictureCreateAndDelete_DoesNotThrow()
    {
        var before = NewEntry(NewSense(NewPicture()), NewSense());
        var after = before.Copy();
        after.Senses[0].Pictures.Clear();
        after.Senses[1].Pictures.Add(NewPicture());

        var act = () => SyncContext.ForEntrySync(before, after);
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

        var act = () => SyncContext.ForEntrySync(before, after);
        act.Should().NotThrow();
    }

    [Fact]
    public void ProjectSync_WrapsEverything()
    {
        var context = SyncContext.ForProjectSync([NewEntry(NewSense())], [NewEntry(NewSense())]);
        context.EntriesDiffApi(NullApi).Should().BeOfType<DeferringDeletesCollectionDiffApi<Entry, Guid>>();
        context.SensesDiffApi(NullApi, Guid.NewGuid()).Should().BeOfType<MoveAwareOrderableDiffApi<Sense, Guid>>();
        context.ExampleSentencesDiffApi(NullApi, Guid.NewGuid(), Guid.NewGuid()).Should().BeOfType<MoveAwareOrderableDiffApi<ExampleSentence, Guid>>();
    }

    [Fact]
    public void EntrySync_DetectsOnlyExampleMoves()
    {
        var entry = NewEntry(NewSense());
        var context = SyncContext.ForEntrySync(entry, entry.Copy());
        // senses still defer deletes and create without children, for examples moving out of a deleted or into a created sense
        context.SensesDiffApi(NullApi, Guid.NewGuid()).Should().BeOfType<DeferringDeletesOrderableDiffApi<Sense, Guid>>();
        context.ExampleSentencesDiffApi(NullApi, Guid.NewGuid(), Guid.NewGuid()).Should().BeOfType<MoveAwareOrderableDiffApi<ExampleSentence, Guid>>();
    }

    [Fact]
    public async Task EmptyContext_HandsOutUnwrappedDiffApis()
    {
        SyncContext.Empty.SensesDiffApi(NullApi, Guid.NewGuid()).Should().BeOfType<EntrySync.SensesDiffApi>();
        SyncContext.Empty.ExampleSentencesDiffApi(NullApi, Guid.NewGuid(), Guid.NewGuid()).Should().BeOfType<ExampleSentenceSync.ExampleSentencesDiffApi>();
        (await SyncContext.Empty.DeferredDeletes.DeleteAll()).Should().Be(0);
    }

    // the diff apis only store the api; nothing here runs a diff
    private static IMiniLcmApi NullApi => null!;
}
