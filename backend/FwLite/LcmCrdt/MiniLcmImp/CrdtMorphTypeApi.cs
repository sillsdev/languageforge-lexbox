using LcmCrdt.Changes;
using LcmCrdt.Changes.Entries;
using LcmCrdt.Data;
using LcmCrdt.Harmony;
using LcmCrdt.Objects;
using LinqToDB.Async;
using MiniLcm.Exceptions;
using MiniLcm.SyncHelpers;
using SIL.Harmony.Changes;

namespace LcmCrdt.MiniLcmImp;

public class CrdtMorphTypeApi(MiniLcmRepositoryFactory repoFactory, HarmonyChangeWriter harmonyChangeWriter)
{
    public async IAsyncEnumerable<MorphType> GetMorphTypes()
    {
        await using var repo = await repoFactory.CreateRepoAsync();
        await foreach (var morphType in repo.MorphTypes.AsAsyncEnumerable())
        {
            yield return morphType;
        }
    }

    public async Task<MorphType?> GetMorphType(Guid id)
    {
        await using var repo = await repoFactory.CreateRepoAsync();
        return await repo.MorphTypes.SingleOrDefaultAsync(m => m.Id == id);
    }

    public async Task<MorphType?> GetMorphType(MorphTypeKind kind)
    {
        await using var repo = await repoFactory.CreateRepoAsync();
        return await repo.MorphTypes.SingleOrDefaultAsync(m => m.Kind == kind);
    }

    public async Task<MorphType> CreateMorphType(MorphType morphType)
    {
        //I don't like returning a different object than what the user requested, it feels very unexpected, however this is pretty much what happens in the change anyway and that can't be avoided
        if (await GetMorphType(morphType.Kind) is {} actualMorphType) return actualMorphType;
        await harmonyChangeWriter.AddChange(new CreateMorphTypeChange(morphType));
        return await GetMorphType(morphType.Id) ?? throw NotFoundException.ForType<MorphType>(morphType.Id);
    }

    public async Task<MorphType> UpdateMorphType(Guid id, UpdateObjectInput<MorphType> update)
    {
        await harmonyChangeWriter.AddChange(new JsonPatchChange<MorphType>(id, update.Patch));
        return await GetMorphType(id) ?? throw NotFoundException.ForType<MorphType>(id);
    }

    public async Task<MorphType> UpdateMorphType(MorphType before, MorphType after, IMiniLcmApi api)
    {
        await MorphTypeSync.Sync(before, after, api);
        return await GetMorphType(after.Id) ?? throw NotFoundException.ForType<MorphType>(after.Id);
    }
}
