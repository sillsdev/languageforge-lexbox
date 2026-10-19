using LcmCrdt.Changes;
using LcmCrdt.Changes.Entries;
using LcmCrdt.Data;
using LcmCrdt.Harmony;
using LcmCrdt.Objects;
using LinqToDB.Async;
using MiniLcm.Exceptions;
using MiniLcm.SyncHelpers;
using SIL.Harmony.Changes;
using SystemTextJsonPatch;

namespace LcmCrdt.MiniLcmImp;

public class CrdtPublicationApi(MiniLcmRepositoryFactory repoFactory, HarmonyChangeWriter harmonyChangeWriter)
{
    public async IAsyncEnumerable<Publication> GetPublications()
    {
        await using var repo = await repoFactory.CreateRepoAsync();
        await foreach (var publication in repo.Publications.AsAsyncEnumerable())
        {
            yield return publication;
        }
    }

    public async Task<Publication?> GetPublication(Guid id)
    {
        await using var repo = await repoFactory.CreateRepoAsync();
        return await repo.GetPublication(id);
    }

    public async Task<Publication> CreatePublication(Publication pub)
    {
        await harmonyChangeWriter.AddChange(new CreatePublicationChange(pub.Id, pub.Name, pub.IsMain));
        return await GetPublication(pub.Id) ?? throw NotFoundException.ForType<Publication>(pub.Id);
    }

    public async Task SubmitUpdatePublication(Guid id, UpdateObjectInput<Publication> update)
    {
        // IsMain is applied via SetMainPublicationChange (which converges across replicas), not as a plain patch op,
        // so it's stripped here. Validation rejects setting IsMain to false on every update/submit path, so isMain is always true.
        if (update.TryGetPropertyChange<Publication, bool>(nameof(Publication.IsMain), out var isMain))
        {
            var patch = new JsonPatchDocument<Publication>();
            patch.Operations.AddRange(update.Patch.Operations.Where(op =>
                !string.Equals(op.Path, $"/{nameof(Publication.IsMain)}", StringComparison.OrdinalIgnoreCase)));
            var changes = patch.ToChanges(id).ToList();
            if (isMain) changes.Add(new SetMainPublicationChange(id));
            if (changes.Count > 0) await harmonyChangeWriter.AddChanges(changes);
        }
        else if (update.Patch.Operations.Count > 0)
        {
            await harmonyChangeWriter.AddChanges(update.Patch.ToChanges(id));
        }
    }

    public async Task<Publication> UpdatePublication(Guid id, UpdateObjectInput<Publication> update)
    {
        await SubmitUpdatePublication(id, update);
        return await GetPublication(id) ?? throw NotFoundException.ForType<Publication>($"{id} (invalid patching to a new id?)");
    }

    public async Task<Publication> UpdatePublication(Publication before, Publication after, IMiniLcmApi api)
    {
        await PublicationSync.Sync(before, after, api);
        var updatedPublication = await GetPublication(after.Id) ?? throw NotFoundException.ForType<Publication>(after.Id);
        return updatedPublication;
    }

    public async Task DeletePublication(Guid id)
    {
        await harmonyChangeWriter.AddChange(new DeleteChange<Publication>(id));
    }

    public async Task AddPublication(Guid entryId, Guid publicationId)
    {
        var pub = await GetPublication(publicationId) ?? throw NotFoundException.ForType<Publication>(publicationId);
        await harmonyChangeWriter.AddChange(new AddPublicationChange(entryId, pub));
    }

    public async Task RemovePublication(Guid entryId, Guid publicationId)
    {
        await harmonyChangeWriter.AddChange(new RemovePublicationChange(entryId, publicationId));
    }
}
