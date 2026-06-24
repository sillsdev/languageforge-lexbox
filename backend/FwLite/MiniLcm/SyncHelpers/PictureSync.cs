using MiniLcm.Models;
using SystemTextJsonPatch;

namespace MiniLcm.SyncHelpers;

public static class PictureSync
{
    public static async Task<int> Sync(Guid entryId,
        Guid senseId,
        IList<Picture> beforePictures,
        IList<Picture> afterPictures,
        IMiniLcmApi api)
    {
        return await DiffCollection.DiffOrderable(
            beforePictures,
            afterPictures,
            new PicturesDiffApi(api, entryId, senseId));
    }

    public static async Task<int> Sync(Guid entryId,
        Guid senseId,
        Picture beforePicture,
        Picture afterPicture,
        IMiniLcmApi api)
    {
        var updateObjectInput = DiffToUpdate(beforePicture, afterPicture);
        if (updateObjectInput is not null)
            await api.UpdatePicture(entryId, senseId, beforePicture.Id, updateObjectInput);
        return updateObjectInput is not null ? 1 : 0;
    }

    public static UpdateObjectInput<Picture>? DiffToUpdate(Picture beforePicture,
        Picture afterPicture)
    {
        JsonPatchDocument<Picture> patchDocument = new();
        patchDocument.Operations.AddRange(MultiStringDiff.GetMultiStringDiff<Picture>(
            nameof(Picture.Caption),
            beforePicture.Caption,
            afterPicture.Caption));
        // TODO: Determine if MediaUri.ToString() is correct here
        patchDocument.Operations.AddRange(SimpleStringDiff.GetStringDiff<Picture>(
            nameof(Picture.MediaUri),
            beforePicture.MediaUri.ToString(),
            afterPicture.MediaUri.ToString()));

        if (patchDocument.Operations.Count == 0) return null;
        return new UpdateObjectInput<Picture>(patchDocument);
    }

    private class PicturesDiffApi(IMiniLcmApi api, Guid entryId, Guid senseId) : IOrderableCollectionDiffApi<Picture, Guid>
    {
        public Guid GetId(Picture value)
        {
            return value.Id;
        }

        public async Task<int> Add(Picture afterPicture, BetweenPosition<Picture> between)
        {
            await api.CreatePicture(entryId, senseId, afterPicture, new BetweenPosition(between.Previous?.Id, between.Next?.Id));
            return 1;
        }

        public async Task<int> Move(Picture picture, BetweenPosition<Picture> between)
        {
            await api.MovePicture(entryId, senseId, picture.Id, new BetweenPosition(between.Previous?.Id, between.Next?.Id));
            return 1;
        }

        public async Task<int> Remove(Picture beforePicture)
        {
            await api.DeletePicture(entryId, senseId, beforePicture.Id);
            return 1;
        }

        public Task<int> Replace(Picture beforePicture, Picture afterPicture)
        {
            return Sync(entryId, senseId, beforePicture, afterPicture, api);
        }
    }
}
