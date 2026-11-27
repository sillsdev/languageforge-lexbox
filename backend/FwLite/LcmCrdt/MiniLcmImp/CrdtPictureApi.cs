using LcmCrdt.Changes;
using LcmCrdt.Data;
using LcmCrdt.Harmony;
using MiniLcm.Exceptions;
using MiniLcm.SyncHelpers;

namespace LcmCrdt.MiniLcmImp;

public class CrdtPictureApi(MiniLcmRepositoryFactory repoFactory, HarmonyChangeWriter harmonyChangeWriter)
{
    public async Task<Picture> CreatePicture(Guid entryId,
        Guid senseId,
        Picture picture,
        BetweenPosition? between = null)
    {
        await VerifySenseBelongsToEntry(entryId, senseId);
        var change = new CreateSensePictureChange(picture, senseId, between);
        await harmonyChangeWriter.AddChange(change);
        return await GetPicture(entryId, senseId, change.PictureId) ?? throw NotFoundException.ForType<Picture>(change.PictureId);
    }

    public async Task SubmitCreatePicture(Guid entryId, Guid senseId, Picture picture, BetweenPosition? between = null)
    {
        await using var repo = await repoFactory.CreateRepoAsync();
        // the picture change edits the sense, and Harmony throws for an edit to a sense that never existed here
        if (await repo.GetSense(senseId) is null) return;
        await harmonyChangeWriter.AddChange(new CreateSensePictureChange(picture, senseId, between));
    }

    private async Task VerifySenseBelongsToEntry(Guid entryId, Guid senseId)
    {
        await using var repo = await repoFactory.CreateRepoAsync();
        var sense = await repo.GetSense(senseId);
        if (sense is not null) CrdtSenseApi.VerifySenseBelongsToEntry(entryId, sense);
    }

    public async Task<Picture?> GetPicture(Guid entryId, Guid senseId, Guid id)
    {
        await using var repo = await repoFactory.CreateRepoAsync();
        // sense first: loading it brings its pictures along, so the owner search only runs when the picture isn't there
        var sense = await repo.GetSense(senseId);
        if (sense is not null)
        {
            CrdtSenseApi.VerifySenseBelongsToEntry(entryId, sense);
            var owned = sense.Pictures.FirstOrDefault(pic => pic.Id == id);
            if (owned is not null) return owned;
        }
        var actualSenseId = await repo.GetPictureSenseId(id);
        if (actualSenseId is null) return null;
        throw ParentMismatchException.ForType<Picture>(id, senseId, actualSenseId.Value);
    }

    public async Task SubmitUpdatePicture(Guid entryId,
        Guid senseId,
        Guid pictureId,
        UpdateObjectInput<Picture> update)
    {
        var jsonPatch = update.Patch;
        var patchChange = new UpdateSensePictureChange(pictureId, senseId, jsonPatch);
        await harmonyChangeWriter.AddChange(patchChange);
    }

    public async Task<Picture> UpdatePicture(Guid entryId,
        Guid senseId,
        Guid pictureId,
        UpdateObjectInput<Picture> update)
    {
        await VerifySenseBelongsToEntry(entryId, senseId);
        await SubmitUpdatePicture(entryId, senseId, pictureId, update);
        return await GetPicture(entryId, senseId, pictureId) ?? throw NotFoundException.ForType<Picture>(pictureId);
    }

    public async Task<Picture> UpdatePicture(Guid entryId,
        Guid senseId,
        Picture before,
        Picture after,
        IMiniLcmApi api)
    {
        await VerifySenseBelongsToEntry(entryId, senseId);
        await PictureSync.Sync(entryId, senseId, before, after, api);
        return await GetPicture(entryId, senseId, after.Id) ?? throw NotFoundException.ForType<Picture>(after.Id);
    }

    public async Task MovePicture(Guid entryId, Guid senseId, Guid pictureId, BetweenPosition between, MoveKind kind = MoveKind.Reorder)
    {
        await using var repo = await repoFactory.CreateRepoAsync();
        var sense = await repo.GetSense(senseId) ?? throw NotFoundException.ForType<Sense>(senseId);
        CrdtSenseApi.VerifySenseBelongsToEntry(entryId, sense);
        if (sense.Pictures.Any(p => p.Id == pictureId))
        {
            await Reorder(sense, pictureId, between);
            return;
        }
        var ownerSenseId = await repo.GetPictureSenseId(pictureId) ?? throw NotFoundException.ForType<Picture>(pictureId);
        if (kind == MoveKind.Reorder) throw ParentMismatchException.ForType<Picture>(pictureId, senseId, ownerSenseId);
        await Reparent(repo, pictureId, ownerSenseId, sense, between);
    }

    public async Task SubmitMovePicture(Guid entryId, Guid senseId, Guid pictureId, BetweenPosition between, MoveKind kind = MoveKind.Reorder)
    {
        await using var repo = await repoFactory.CreateRepoAsync();
        var sense = await repo.GetSense(senseId);
        if (sense is not null && sense.Pictures.Any(p => p.Id == pictureId))
        {
            await Reorder(sense, pictureId, between);
            return;
        }
        // the picture was deleted or moved on this side, so a reorder is moot
        if (kind == MoveKind.Reorder) return;
        var ownerSenseId = await repo.GetPictureSenseId(pictureId);
        // deleted on this side: delete wins
        if (ownerSenseId is null) return;
        await Reparent(repo, pictureId, ownerSenseId.Value, sense, between);
    }

    private async Task Reorder(Sense sense, Guid pictureId, BetweenPosition between)
    {
        await harmonyChangeWriter.AddChange(new ReorderSensePictureChange(pictureId, sense.Id, OrderPicker.PickOrder(sense.Pictures, between)));
    }

    /// <summary>
    /// Pictures live inside their sense's row, so a move is a remove from one sense and a create (same id, data copied now)
    /// on the other. Unlike an example move, an edit or delete another client made under the old sense doesn't follow the picture,
    /// and two concurrent moves leave it in both senses. A single change that edits both senses needs harmony#48.
    /// </summary>
    private async Task Reparent(MiniLcmRepository repo, Guid pictureId, Guid ownerSenseId, Sense? targetSense, BetweenPosition between)
    {
        var ownerSense = await repo.GetSense(ownerSenseId) ?? throw NotFoundException.ForType<Sense>(ownerSenseId);
        var picture = ownerSense.Pictures.First(p => p.Id == pictureId);
        var remove = new RemoveSensePictureChange(pictureId, ownerSenseId);
        // the target sense is gone on this side: delete wins
        if (targetSense is null)
        {
            await harmonyChangeWriter.AddChange(remove);
            return;
        }
        await harmonyChangeWriter.AddChanges([remove, new CreateSensePictureChange(picture, targetSense.Id, between)]);
    }

    public async Task DeletePicture(Guid entryId, Guid senseId, Guid pictureId)
    {
        await harmonyChangeWriter.AddChange(new RemoveSensePictureChange(pictureId, senseId));
    }
}
