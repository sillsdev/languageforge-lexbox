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

        var beforeDefaultId = GetDefaultPublicationId(beforePublications);
        var afterDefaultId = GetDefaultPublicationId(afterPublications);

        if (beforeDefaultId != afterDefaultId && beforeDefaultId is not null)
        {
            await api.UpdatePublication(beforeDefaultId.Value,
                new UpdateObjectInput<Publication>().Set(p => p.DefaultedAt, DateTimeOffset.UtcNow));
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

        if (beforePublication.DefaultedAt != afterPublication.DefaultedAt)
        {
            patchDocument.Replace(p => p.DefaultedAt, afterPublication.DefaultedAt);
        }

        if (patchDocument.Operations.Count == 0) return null;
        return new UpdateObjectInput<Publication>(patchDocument);
    }

    public static Guid? GetDefaultPublicationId(IEnumerable<Publication> publications)
    {
        return publications
            .Where(pub => pub.DefaultedAt is not null)
            .OrderByDescending(pub => pub.DefaultedAt)
            .ThenBy(pub => pub.Id)
            .Select(pub => (Guid?)pub.Id)
            .FirstOrDefault();
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
