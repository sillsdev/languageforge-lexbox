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

        var act = () => new SyncContext([before], [after]);
        act.Should().Throw<MoveNotSupportedException>()
            .WithMessage($"*{picture.Id}*{sourceSense.Id}*{targetSense.Id}*");
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

        var act = () => new SyncContext([before], [after]);
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

        var act = () => new SyncContext([sourceEntry], [sourceEntryAfter, createdEntry]);
        act.Should().NotThrow();
    }

    [Fact]
    public void GenuinePictureCreateAndDelete_DoesNotThrow()
    {
        var before = NewEntry(NewSense(NewPicture()), NewSense());
        var after = before.Copy();
        after.Senses[0].Pictures.Clear();
        after.Senses[1].Pictures.Add(NewPicture());

        var act = () => new SyncContext([before], [after]);
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

        var act = () => new SyncContext([before], [after]);
        act.Should().NotThrow();
    }

    [Fact]
    public void EmptyContext_TreatsEverythingAsGenuineAndDeletesImmediately()
    {
        var context = SyncContext.Empty;
        context.Senses.IsActuallyADelete(Guid.NewGuid()).Should().BeTrue();
        context.Examples.IsActuallyAMove(Guid.NewGuid(), out _).Should().BeFalse();
        var deleted = false;
        context.Pictures.Delete(() =>
        {
            deleted = true;
            return Task.FromResult(1);
        }).Result.Should().Be(1);
        deleted.Should().BeTrue();
    }
}
