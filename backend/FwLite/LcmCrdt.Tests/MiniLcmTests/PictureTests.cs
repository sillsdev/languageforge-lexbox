using MiniLcm.Media;
using MiniLcm.SyncHelpers;

namespace LcmCrdt.Tests.MiniLcmTests;

public class PictureTests : PictureTestsBase
{
    private readonly MiniLcmApiFixture _fixture = new();

    protected override async Task<IMiniLcmApi> NewApi()
    {
        await _fixture.InitializeAsync("picture-test");
        return _fixture.Api;
    }

    public override async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _fixture.DisposeAsync();
    }

    [Fact]
    public async Task CreateSense_NumbersPicturesFromOne()
    {
        var sense = await CreateSenseWith(PicturesContradictingListOrder());
        sense.Pictures.Select(p => p.Order).Should().Equal(1, 2, 3);
    }

    [Fact]
    public async Task CreateEntry_NumbersPicturesFromOne()
    {
        var sense = await CreateEntryWith(PicturesContradictingListOrder());
        sense.Pictures.Select(p => p.Order).Should().Equal(1, 2, 3);
    }

    [Fact]
    public async Task BulkCreateEntries_NumbersPicturesFromOne()
    {
        var sense = await BulkCreateEntryWith(PicturesContradictingListOrder());
        sense.Pictures.Select(p => p.Order).Should().Equal(1, 2, 3);
    }

    // FwData's SubmitMovePicture is just MovePicture, so these lenient cases are CRDT-only

    [Fact]
    public async Task SubmitMovePicture_ReorderUnderWrongSense_DoesNothing()
    {
        var otherSenseId = Guid.NewGuid();
        await Api.CreateSense(_entryId, new Sense { Id = otherSenseId, Gloss = { { "en", "other" } } });

        await Api.SubmitMovePicture(_entryId, otherSenseId, _pictureId, new BetweenPosition(null, null), MoveKind.Reorder);

        (await GetPictureIds()).Should().Equal(_pictureId);
        var otherSense = await Api.GetSense(_entryId, otherSenseId);
        otherSense.Should().NotBeNull();
        otherSense.Pictures.Should().BeEmpty();
    }

    [Fact]
    public async Task SubmitMovePicture_ReorderUnderMissingSense_DoesNothing()
    {
        await Api.SubmitMovePicture(_entryId, Guid.NewGuid(), _pictureId, new BetweenPosition(null, null), MoveKind.Reorder);

        (await GetPictureIds()).Should().Equal(_pictureId);
    }

    [Fact]
    public async Task SubmitMovePicture_ReparentDeletedPicture_DoesNothing()
    {
        var targetSenseId = Guid.NewGuid();
        await Api.CreateSense(_entryId, new Sense { Id = targetSenseId, Gloss = { { "en", "target" } } });
        await Api.DeletePicture(_entryId, _senseId, _pictureId);

        await Api.SubmitMovePicture(_entryId, targetSenseId, _pictureId, new BetweenPosition(null, null), MoveKind.Reparent);

        var targetSense = await Api.GetSense(_entryId, targetSenseId);
        targetSense.Should().NotBeNull();
        targetSense.Pictures.Should().BeEmpty();
        (await GetPictureIds()).Should().BeEmpty();
    }

    [Fact]
    public async Task SubmitMovePicture_ReparentToDeletedSense_MovesPictureIntoDeletedSense()
    {
        var targetSenseId = Guid.NewGuid();
        await Api.CreateSense(_entryId, new Sense { Id = targetSenseId, Gloss = { { "en", "target" } } });
        await Api.DeleteSense(_entryId, targetSenseId);

        await Api.SubmitMovePicture(_entryId, targetSenseId, _pictureId, new BetweenPosition(null, null), MoveKind.Reparent);

        (await GetPictureIds()).Should().BeEmpty();
        (await DeletedSensePictureIds(targetSenseId)).Should().Equal(_pictureId);
    }

    [Fact]
    public async Task SubmitMovePicture_ReparentToSenseThatNeverExisted_RemovesPicture()
    {
        await Api.SubmitMovePicture(_entryId, Guid.NewGuid(), _pictureId, new BetweenPosition(null, null), MoveKind.Reparent);

        (await GetPictureIds()).Should().BeEmpty();
    }

    [Fact]
    public async Task SubmitMovePicture_ReparentAlreadyApplied_Reorders()
    {
        var second = await CreatePicture("second");

        await Api.SubmitMovePicture(_entryId, _senseId, second.Id, new BetweenPosition(null, _pictureId), MoveKind.Reparent);

        (await GetPictureIds()).Should().Equal(second.Id, _pictureId);
    }

    [Fact]
    public async Task SubmitMovePicture_ReparentFromUnexpectedSense_MovesFromCurrentOwner()
    {
        var currentOwnerId = Guid.NewGuid();
        var targetSenseId = Guid.NewGuid();
        await Api.CreateSense(_entryId, new Sense { Id = currentOwnerId, Gloss = { { "en", "current" } } });
        await Api.CreateSense(_entryId, new Sense { Id = targetSenseId, Gloss = { { "en", "target" } } });
        await Api.MovePicture(_entryId, currentOwnerId, _pictureId, new BetweenPosition(null, null), MoveKind.Reparent);

        await Api.SubmitMovePicture(_entryId, targetSenseId, _pictureId, new BetweenPosition(null, null), MoveKind.Reparent);

        var entry = await Api.GetEntry(_entryId);
        entry.Should().NotBeNull();
        entry.Senses.Single(s => s.Id == targetSenseId).Pictures.Select(p => p.Id).Should().Equal(_pictureId);
        entry.Senses.Where(s => s.Id != targetSenseId).SelectMany(s => s.Pictures).Should().BeEmpty();
    }

    [Fact]
    public async Task SubmitCreatePicture_UnderDeletedSense_AddsPictureToDeletedSense()
    {
        var targetSenseId = Guid.NewGuid();
        await Api.CreateSense(_entryId, new Sense { Id = targetSenseId, Gloss = { { "en", "target" } } });
        await Api.DeleteSense(_entryId, targetSenseId);
        var picture = new Picture { Id = Guid.NewGuid(), MediaUri = new MediaUri(Guid.NewGuid(), "test") };

        await Api.SubmitCreatePicture(_entryId, targetSenseId, picture);

        (await DeletedSensePictureIds(targetSenseId)).Should().Equal(picture.Id);
    }

    [Fact]
    public async Task SubmitCreatePicture_UnderSenseThatNeverExisted_DoesNothing()
    {
        var picture = new Picture { Id = Guid.NewGuid(), MediaUri = new MediaUri(Guid.NewGuid(), "test") };

        await Api.SubmitCreatePicture(_entryId, Guid.NewGuid(), picture);

        (await Api.GetPicture(_entryId, _senseId, picture.Id)).Should().BeNull();
    }

    private async Task<IEnumerable<Guid>> DeletedSensePictureIds(Guid senseId)
    {
        var sense = await _fixture.DataModel.GetLatest<Sense>(senseId);
        sense.Should().NotBeNull();
        sense.DeletedAt.Should().NotBeNull();
        return sense.Pictures.Select(p => p.Id);
    }
}
