using MiniLcm.Models;
using SystemTextJsonPatch;

namespace MiniLcm.SyncHelpers;

public static class PictureSync
{
    public static async Task<int> Sync(Guid entryId,
        Guid senseId,
        IList<Picture> beforePictures,
        IList<Picture> afterPictures,
        IMiniLcmApi api,
        SyncContext context)
    {
        return await DiffCollection.DiffOrderable(
            beforePictures,
            afterPictures,
            new PicturesDiffApi(api, entryId, senseId, context));
    }

    public static async Task<int> Sync(Guid entryId,
        Guid senseId,
        Picture beforePicture,
        Picture afterPicture,
        IMiniLcmApi api)
    {
        var updateObjectInput = DiffToUpdate(beforePicture, afterPicture);
        if (updateObjectInput is not null)
            await api.SubmitUpdatePicture(entryId, senseId, beforePicture.Id, updateObjectInput);
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
        patchDocument.Operations.AddRange(SimpleStringDiff.GetStringDiff<Picture>(
            nameof(Picture.MediaUri),
            beforePicture.MediaUri.ToString(),
            afterPicture.MediaUri.ToString()));

        if (patchDocument.Operations.Count == 0) return null;
        return new UpdateObjectInput<Picture>(patchDocument);
    }

    private class PicturesDiffApi(IMiniLcmApi api, Guid entryId, Guid senseId, SyncContext context) : IOrderableCollectionDiffApi<Picture, Guid>
    {
        public Guid GetId(Picture value)
        {
            return value.Id;
        }

        public async Task<int> Add(Picture afterPicture, BetweenPosition<Picture> between)
        {
            var position = new BetweenPosition(between.Previous?.Id, between.Next?.Id);
            // a known id arriving here is a move (e.g. FLEx "Merge Sense"); see ExampleSentenceSync
            if (context.ExistedBefore(afterPicture) is { } before)
            {
                await api.SubmitMovePicture(entryId, senseId, afterPicture.Id, position, MoveKind.Reparent);
                return 1 + await Sync(entryId, senseId, before, afterPicture, api);
            }
            await api.SubmitCreatePicture(entryId, senseId, afterPicture, position);
            return 1;
        }

        public async Task<int> Move(Picture picture, BetweenPosition<Picture> between)
        {
            await api.SubmitMovePicture(entryId, senseId, picture.Id, new BetweenPosition(between.Previous?.Id, between.Next?.Id));
            return 1;
        }

        public async Task<int> Remove(Picture beforePicture)
        {
            // it was moved. Add will handle it.
            if (context.StillExists(beforePicture)) return 0;
            await api.DeletePicture(entryId, senseId, beforePicture.Id);
            return 1;
        }

        public Task<int> Replace(Picture beforePicture, Picture afterPicture)
        {
            return Sync(entryId, senseId, beforePicture, afterPicture, api);
        }
    }
}
