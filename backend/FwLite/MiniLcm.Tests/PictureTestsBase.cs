using MiniLcm.Exceptions;
using MiniLcm.Media;
using MiniLcm.SyncHelpers;

namespace MiniLcm.Tests;

public abstract class PictureTestsBase : MiniLcmTestBase
{
    private readonly Guid _entryId = Guid.NewGuid();
    private readonly Guid _senseId = Guid.NewGuid();
    private readonly Guid _pictureId = Guid.NewGuid();
    private MediaUri _mediaUri;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        _mediaUri = await SavePictureFile();
        await Api.CreateEntry(new Entry()
        {
            Id = _entryId,
            LexemeForm = { { "en", "new-lexeme-form" } },
            Senses =
            [
                new Sense()
                {
                    Id = _senseId,
                    Gloss = { { "en", "new-sense-gloss" } },
                    Pictures =
                    [
                        new Picture
                        {
                            Id = _pictureId,
                            MediaUri = _mediaUri,
                            Caption = { { "en", new RichString("first", "en") } },
                        }
                    ]
                }
            ]
        });
    }

    private async Task<MediaUri> SavePictureFile()
    {
        // the content doesn't need to be a valid image, the APIs only care about the file
        await using var stream = new MemoryStream([1, 2, 3, 4]);
        var response = await Api.SaveFile(stream,
            new LcmFileMetadata($"picture-{Guid.NewGuid()}.png", "image/png", "Test Author", DateTimeOffset.UtcNow));
        response.Result.Should().BeOneOf(UploadFileResult.SavedLocally, UploadFileResult.SavedToLexbox);
        response.MediaUri.Should().NotBeNull().And.NotBe(MediaUri.NotFound);
        return response.MediaUri!.Value;
    }

    private Task<Picture> CreatePicture(string caption, BetweenPosition? position = null)
    {
        return Api.CreatePicture(_entryId, _senseId, new Picture
        {
            Id = Guid.NewGuid(),
            MediaUri = _mediaUri,
            Caption = { { "en", new RichString(caption, "en") } },
        }, position);
    }

    private async Task<Guid[]> GetPictureIds()
    {
        var sense = await Api.GetSense(_entryId, _senseId);
        sense.Should().NotBeNull();
        return [.. sense.Pictures.Select(p => p.Id)];
    }

    [Fact]
    public async Task Get_MissingPicture_ReturnsNull()
    {
        var picture = await Api.GetPicture(_entryId, _senseId, Guid.NewGuid());
        picture.Should().BeNull();
    }

    [Fact]
    public async Task Get_ExistingPicture_ReturnsPicture()
    {
        var picture = await Api.GetPicture(_entryId, _senseId, _pictureId);
        picture.Should().NotBeNull();
        picture.Id.Should().Be(_pictureId);
        picture.MediaUri.Should().Be(_mediaUri);
        picture.Caption["en"].Should().BeEquivalentTo(new RichString("first", "en"));
    }

    [Fact]
    public async Task Get_PictureFromWrongSense_Throws()
    {
        var otherSenseId = Guid.NewGuid();
        await Api.CreateSense(_entryId, new Sense { Id = otherSenseId, Gloss = { { "en", "other" } } });

        var act = () => Api.GetPicture(_entryId, otherSenseId, _pictureId);
        await act.Should().ThrowAsync<ParentMismatchException>();
    }

    [Fact]
    public async Task Get_PictureFromMissingSense_Throws()
    {
        var act = () => Api.GetPicture(_entryId, Guid.NewGuid(), _pictureId);
        await act.Should().ThrowAsync<ParentMismatchException>();
    }

    [Fact]
    public async Task Get_PictureFromWrongEntry_Throws()
    {
        var otherEntryId = Guid.NewGuid();
        await Api.CreateEntry(new Entry { Id = otherEntryId, LexemeForm = { { "en", "other" } } });

        var act = () => Api.GetPicture(otherEntryId, _senseId, _pictureId);
        await act.Should().ThrowAsync<ParentMismatchException>();
    }

    [Fact]
    public async Task CreatePicture_RoundTrips()
    {
        var expected = new Picture
        {
            Id = Guid.NewGuid(),
            MediaUri = _mediaUri,
            Caption = { { "en", new RichString("an apple", "en") } },
        };

        var created = await Api.CreatePicture(_entryId, _senseId, expected.Copy());
        created.Should().BeEquivalentTo(expected, options => options.Excluding(p => p.Order));

        var fetched = await Api.GetPicture(_entryId, _senseId, expected.Id);
        fetched.Should().BeEquivalentTo(created);

        var sense = await Api.GetSense(_entryId, _senseId);
        sense.Should().NotBeNull();
        sense.Pictures.Should().ContainSingle(p => p.Id == expected.Id)
            .Which.Should().BeEquivalentTo(created);
    }

    [Fact]
    public async Task CreatePicture_WithoutPosition_AppendsToEnd()
    {
        var second = await CreatePicture("second");
        var third = await CreatePicture("third");

        (await GetPictureIds()).Should().Equal([_pictureId, second.Id, third.Id]);
    }

    [Fact]
    public async Task CreatePicture_WithNextOnly_InsertsAtStart()
    {
        var created = await CreatePicture("new", new BetweenPosition(null, _pictureId));

        (await GetPictureIds()).Should().Equal([created.Id, _pictureId]);
    }

    [Fact]
    public async Task CreatePicture_WithPreviousOnly_InsertsAfterPrevious()
    {
        // previous is deliberately the last picture: with a picture after it, CRDT picks previous.Order + 1,
        // which ties with that picture, so the result is only defined when both neighbours are given
        var second = await CreatePicture("second");

        var created = await CreatePicture("new", new BetweenPosition(second.Id, null));

        (await GetPictureIds()).Should().Equal([_pictureId, second.Id, created.Id]);
    }

    [Fact]
    public async Task CreatePicture_BetweenTwoPictures_InsertsInTheMiddle()
    {
        var last = await CreatePicture("last");

        var created = await CreatePicture("new", new BetweenPosition(_pictureId, last.Id));

        (await GetPictureIds()).Should().Equal([_pictureId, created.Id, last.Id]);
    }

    [Fact]
    public async Task UpdatePicture_Patch_UpdatesCaption()
    {
        var updated = await Api.UpdatePicture(_entryId, _senseId, _pictureId,
            new UpdateObjectInput<Picture>().Set(p => p.Caption["en"], new RichString("updated", "en")));

        updated.Id.Should().Be(_pictureId);
        updated.Caption["en"].Should().BeEquivalentTo(new RichString("updated", "en"));
        updated.MediaUri.Should().Be(_mediaUri);

        var fetched = await Api.GetPicture(_entryId, _senseId, _pictureId);
        fetched.Should().BeEquivalentTo(updated);
    }

    [Fact]
    public async Task SubmitUpdatePicture_UpdatesCaption()
    {
        await Api.SubmitUpdatePicture(_entryId, _senseId, _pictureId,
            new UpdateObjectInput<Picture>().Set(p => p.Caption["en"], new RichString("submitted", "en")));

        var fetched = await Api.GetPicture(_entryId, _senseId, _pictureId);
        fetched.Should().NotBeNull();
        fetched.Caption["en"].Should().BeEquivalentTo(new RichString("submitted", "en"));
    }

    [Fact]
    public async Task UpdatePicture_BeforeAfter_UpdatesCaptionAndMediaUri()
    {
        var newMediaUri = await SavePictureFile();
        var before = await Api.GetPicture(_entryId, _senseId, _pictureId);
        before.Should().NotBeNull();
        var after = before.Copy();
        after.Caption["en"] = new RichString("changed", "en");
        after.MediaUri = newMediaUri;

        var updated = await Api.UpdatePicture(_entryId, _senseId, before, after);

        updated.Should().BeEquivalentTo(after);
        var fetched = await Api.GetPicture(_entryId, _senseId, _pictureId);
        fetched.Should().BeEquivalentTo(after);
    }

    [Fact]
    public async Task CreatePicture_WrongEntry_ThrowsWithoutCreating()
    {
        var otherEntryId = Guid.NewGuid();
        await Api.CreateEntry(new Entry { Id = otherEntryId, LexemeForm = { { "en", "other" } } });

        var act = () => Api.CreatePicture(otherEntryId, _senseId,
            new Picture { Id = Guid.NewGuid(), MediaUri = _mediaUri, Caption = { { "en", new RichString("new", "en") } } });
        await act.Should().ThrowAsync<ParentMismatchException>();

        (await GetPictureIds()).Should().Equal([_pictureId]);
    }

    [Fact]
    public async Task UpdatePicture_WrongEntry_ThrowsWithoutUpdating()
    {
        var otherEntryId = Guid.NewGuid();
        await Api.CreateEntry(new Entry { Id = otherEntryId, LexemeForm = { { "en", "other" } } });
        var before = await Api.GetPicture(_entryId, _senseId, _pictureId);
        before.Should().NotBeNull();
        var after = before.Copy();
        after.Caption["en"] = new RichString("changed", "en");

        var patch = () => Api.UpdatePicture(otherEntryId, _senseId, _pictureId,
            new UpdateObjectInput<Picture>().Set(p => p.Caption["en"], new RichString("updated", "en")));
        await patch.Should().ThrowAsync<ParentMismatchException>();
        var beforeAfter = () => Api.UpdatePicture(otherEntryId, _senseId, before, after);
        await beforeAfter.Should().ThrowAsync<ParentMismatchException>();

        var fetched = await Api.GetPicture(_entryId, _senseId, _pictureId);
        fetched.Should().BeEquivalentTo(before);
    }

    [Fact]
    public async Task MovePicture_ToStart()
    {
        var second = await CreatePicture("second");
        var third = await CreatePicture("third");

        await Api.MovePicture(_entryId, _senseId, third.Id, new BetweenPosition(null, _pictureId));

        (await GetPictureIds()).Should().Equal([third.Id, _pictureId, second.Id]);
    }

    [Fact]
    public async Task MovePicture_ToEnd()
    {
        var second = await CreatePicture("second");
        var third = await CreatePicture("third");

        await Api.MovePicture(_entryId, _senseId, _pictureId, new BetweenPosition(third.Id, null));

        (await GetPictureIds()).Should().Equal([second.Id, third.Id, _pictureId]);
    }

    [Fact]
    public async Task MovePicture_BetweenTwoPictures()
    {
        var second = await CreatePicture("second");
        var third = await CreatePicture("third");

        await Api.MovePicture(_entryId, _senseId, third.Id, new BetweenPosition(_pictureId, second.Id));

        (await GetPictureIds()).Should().Equal([_pictureId, third.Id, second.Id]);
    }

    [Fact]
    public async Task MovePicture_KeepsPictureContent()
    {
        var second = await CreatePicture("second");

        await Api.MovePicture(_entryId, _senseId, second.Id, new BetweenPosition(null, _pictureId));

        var moved = await Api.GetPicture(_entryId, _senseId, second.Id);
        moved.Should().BeEquivalentTo(second, options => options.Excluding(p => p.Order));
    }

    [Fact]
    public async Task DeletePicture_RemovesOnlyThatPicture()
    {
        var second = await CreatePicture("second");

        await Api.DeletePicture(_entryId, _senseId, _pictureId);

        var deleted = await Api.GetPicture(_entryId, _senseId, _pictureId);
        deleted.Should().BeNull();
        (await GetPictureIds()).Should().Equal([second.Id]);
    }
}
