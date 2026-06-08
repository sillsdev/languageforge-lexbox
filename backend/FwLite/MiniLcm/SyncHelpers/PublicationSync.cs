using MiniLcm.Models;
using SystemTextJsonPatch;

namespace MiniLcm.SyncHelpers;

public static class PublicationSync
{
    public static async Task<int> Sync(Publication[] beforePublications,
        Publication[] afterPublications,
        IMiniLcmApi api)
    {
        var changes = await DiffCollection.Diff(
            beforePublications,
            afterPublications,
            new PublicationsDiffApi(api));

        // Sync the IsMain flag: if "after" has a main but "before" does not, promote it.
        // A main that's new to this collection was already created with IsMain set (PublicationsDiffApi.Add),
        // so only promote one that already existed but wasn't yet the main — otherwise we'd redundantly update
        // a publication that doesn't exist yet (e.g. it throws during a dry-run sync).
        var beforeMain = beforePublications.FirstOrDefault(p => p.IsMain);
        var afterMain = afterPublications.FirstOrDefault(p => p.IsMain);

        if (afterMain is not null && beforeMain is null && beforePublications.Any(p => p.Id == afterMain.Id))
        {
            await api.UpdatePublication(afterMain.Id,
                new UpdateObjectInput<Publication>().Set(p => p.IsMain, true));
            changes++;
        }

        return changes;
    }

    public static async Task<int> Sync(
        Publication beforePublication,
        Publication afterPublication,
        IMiniLcmApi api)
    {
        var updateObjectInput = DiffToUpdate(beforePublication, afterPublication);
        if (updateObjectInput is null) return 0;
        await api.SubmitUpdatePublication(beforePublication.Id, updateObjectInput);
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

        // IsMain is intentionally not diffed here: it transitions only 0->1, handled once in the collection-level Sync.
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
