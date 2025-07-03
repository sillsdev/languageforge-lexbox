using MiniLcm.Models;
using SystemTextJsonPatch;

namespace MiniLcm.SyncHelpers;

public static class PublicationSync
{
    public static async Task<int> Sync(Publication[] beforePublications,
        Publication[] afterPublications,
        IMiniLcmApi api)
    {
        return await DiffCollection.Diff(
            beforePublications,
            afterPublications,
            new PublicationsDiffApi(api));
    }

    public static async Task<int> Sync(
        Publication beforePublication,
        Publication afterPublication,
        IMiniLcmApi api)
    {
        var updateObjectInput = DiffToUpdate(beforePublication, afterPublication);
        if (updateObjectInput is null) return 0;
        await api.UpdatePublication(beforePublication.Id, updateObjectInput);
        return 1;
    }

    public static UpdateObjectInput<Publication>? DiffToUpdate(Publication beforePublication,
        Publication afterPublication)
    {
        JsonPatchDocument<Publication> patchDocument = new();
        patchDocument.Operations.AddRange(MultiStringDiff.GetMultiStringDiff<Publication>(
            nameof(Publication.Name),
            beforePublication.Name,
            afterPublication.Name));

        if (patchDocument.Operations.Count == 0) return null;
        return new UpdateObjectInput<Publication>(patchDocument);
    }

    private class PublicationsDiffApi(IMiniLcmApi api) : ObjectWithIdCollectionDiffApi<Publication>
    {
        public override async Task<int> Add(Publication currentPub)
        {
            await api.CreatePublication(currentPub);
            return 1;
        }

        public override async Task<int> Remove(Publication beforePub)
        {
            await api.DeletePublication(beforePub.Id);
            return 1;
        }

        public override Task<int> Replace(Publication beforePub, Publication afterPub)
        {
            return Sync(beforePub, afterPub, api);
        }
    }
}
