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
        var change = new CreateSensePictureChange(picture, senseId, between);
        await harmonyChangeWriter.AddChange(change);
        return await GetPicture(entryId, senseId, change.PictureId) ?? throw NotFoundException.ForType<Picture>(change.PictureId);
    }

    public async Task<Picture?> GetPicture(Guid entryId, Guid senseId, Guid id)
    {
        await using var repo = await repoFactory.CreateRepoAsync();
        var sense = await repo.GetSense(senseId);
        return sense?.Pictures.FirstOrDefault(pic => pic.Id == id);
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
        await SubmitUpdatePicture(entryId, senseId, pictureId, update);
        return await GetPicture(entryId, senseId, pictureId) ?? throw NotFoundException.ForType<Picture>(pictureId);
    }

    public async Task<Picture> UpdatePicture(Guid entryId,
        Guid senseId,
        Picture before,
        Picture after,
        IMiniLcmApi api)
    {
        await PictureSync.Sync(entryId, senseId, before, after, api);
        return await GetPicture(entryId, senseId, after.Id) ?? throw NotFoundException.ForType<Picture>(after.Id);
    }

    public async Task MovePicture(Guid entryId, Guid senseId, Guid pictureId, BetweenPosition between)
    {
        await using var repo = await repoFactory.CreateRepoAsync();
        var sense = await repo.GetSense(senseId);
        if (sense is null) throw NotFoundException.ForType<Sense>(senseId);
        var order = OrderPicker.PickOrder(sense.Pictures, between);
        await harmonyChangeWriter.AddChange(new ReorderSensePictureChange(pictureId, senseId, order));
    }

    public async Task DeletePicture(Guid entryId, Guid senseId, Guid pictureId)
    {
        await harmonyChangeWriter.AddChange(new RemoveSensePictureChange(pictureId, senseId));
    }
}
